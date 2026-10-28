using System.Text.Json;
using System.Text.RegularExpressions;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed partial class TicketControl(
    SWCareDbContext db,
    CurrentUser currentUser,
    AttachmentStorage storage,
    ISettingControl settings,
    TimeProvider clock) : ITicketControl
{
    public async Task<TicketDetailRes> CreateAsync(SaveTicketReq request, IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken)
    {
        var me = currentUser.User;

        // Nobody could approve it (Q2). A head's own ticket skips approval, so it needs no head.
        if (!me.IsDepartmentHead
            && await db.Departments.AnyAsync(d => d.Id == me.DepartmentId && d.HeadUserId == null, cancellationToken))
        {
            throw new BusinessRuleException("Your department has no head to approve tickets. Ask the Admin.", ErrorCodes.NoDepartmentHead);
        }

        await CheckDetailsAsync(request, me.Id, me.DepartmentKind, cancellationToken);
        AttachmentRules.Validate(files);
        await DropUntouchedTemplateAsync(request, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;
        var ticket = new Ticket
        {
            ReporterId = me.Id,
            DepartmentId = me.DepartmentId,
            Status = TicketStatus.PendingApproval,
            ReportedAt = now
        };
        Apply(ticket, request, me.Id);
        ticket.DeviceModels = request.DeviceModelIds.Distinct()
            .Select(modelId => new TicketDeviceModel { DeviceModelId = modelId })
            .ToList();
        ticket.Activities.Add(new TicketActivity { Type = TicketActivityType.Created, ToStatus = TicketStatus.PendingApproval, ActorId = me.Id });

        if (me.IsDepartmentHead)
        {
            // The head would approve their own ticket anyway (Q14).
            ticket.Status = TicketStatus.PendingTriage;
            ticket.ApproverId = me.Id;
            ticket.ApprovedAt = now;
            ticket.Activities.Add(new TicketActivity
            {
                Type = TicketActivityType.AutoApproved,
                FromStatus = TicketStatus.PendingApproval,
                ToStatus = TicketStatus.PendingTriage
            });
        }

        var saved = new List<StoredFile>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            ticket.TicketNo = await NextTicketNoAsync(cancellationToken);
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync(cancellationToken);

            await StoreFilesAsync(ticket.Id, commentId: null, files, me.Id, saved, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            AttachmentStorage.DeleteQuietly(saved);
            throw;
        }

        return await GetAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDetailRes> UpdateAsync(int id, SaveTicketReq request, CancellationToken cancellationToken)
    {
        var me = currentUser.User;
        var ticket = await db.Tickets.VisibleTo(me)
                         .Include(t => t.Department)
                         .Include(t => t.Product)
                         .Include(t => t.Requester)
                         .Include(t => t.DeviceModels).ThenInclude(m => m.DeviceModel)
                         .SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Ticket not found.");

        TicketWorkflow.Ensure(TicketWorkflow.StateOf(ticket), me, TicketAction.Edit);
        await CheckDetailsAsync(request, ticket.ReporterId, ticket.Department.Kind, cancellationToken);
        await DropUntouchedTemplateAsync(request, cancellationToken);

        var changes = await DescribeChangesAsync(ticket, request, cancellationToken);
        if (changes.Count == 0)
        {
            return await GetAsync(id, cancellationToken);
        }

        Apply(ticket, request, ticket.ReporterId);

        var newModelIds = request.DeviceModelIds.Distinct().ToHashSet();
        foreach (var removed in ticket.DeviceModels.Where(m => !newModelIds.Contains(m.DeviceModelId)).ToList())
        {
            ticket.DeviceModels.Remove(removed);
        }

        foreach (var added in newModelIds.Except(ticket.DeviceModels.Select(m => m.DeviceModelId)))
        {
            ticket.DeviceModels.Add(new TicketDeviceModel { DeviceModelId = added });
        }

        ticket.Activities.Add(new TicketActivity
        {
            Type = TicketActivityType.Edited,
            ActorId = me.Id,
            Data = JsonSerializer.Serialize(new { Changes = changes })
        });

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<TicketDetailRes> GetAsync(int id, CancellationToken cancellationToken)
    {
        var me = currentUser.User;
        var ticket = await db.Tickets.AsNoTracking().AsSplitQuery()
                         .VisibleTo(me)
                         .Include(t => t.Product)
                         .Include(t => t.Reporter)
                         .Include(t => t.Requester)
                         .Include(t => t.Department)
                         .Include(t => t.Approver)
                         .Include(t => t.Assignee)
                         .Include(t => t.DeviceModels).ThenInclude(m => m.DeviceModel)
                         .Include(t => t.Watchers).ThenInclude(w => w.User)
                         .Include(t => t.Attachments).ThenInclude(a => a.UploadedBy)
                         .Include(t => t.Comments).ThenInclude(c => c.Author)
                         .Include(t => t.Activities).ThenInclude(a => a.Actor)
                         .SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
                     ?? throw new NotFoundException("Ticket not found.");

        var attachmentsByComment = ticket.Attachments
            .Where(a => a.CommentId is not null)
            .GroupBy(a => a.CommentId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Id).Select(ToRes).ToList());

        return new TicketDetailRes(
            ticket.Id,
            ticket.TicketNo,
            ticket.LegacyRef,
            ticket.Title,
            ticket.Type,
            new IdNameRes(ticket.Product.Id, ticket.Product.Name),
            ticket.Version,
            ticket.CustomerName,
            ticket.Description,
            ticket.TestedDetails,
            ticket.RequestedPriority,
            ticket.ActualPriority,
            ticket.Status,
            Brief(ticket.Reporter),
            BriefOrNull(ticket.Requester),
            new IdNameRes(ticket.Department.Id, ticket.Department.Name),
            BriefOrNull(ticket.Approver),
            ticket.ApprovedAt,
            BriefOrNull(ticket.Assignee),
            ticket.PlannedStartDate,
            ticket.PlannedDueDate,
            ticket.ReportedAt,
            ticket.StartedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            ticket.Resolution,
            ticket.ReturnCount,
            ticket.RescheduleCount,
            ticket.NeedsLegacyReview,
            ticket.LegacyReporterName,
            ticket.LegacyRequesterName,
            ticket.LegacyApproverName,
            ticket.LegacyAssigneeName,
            ticket.DeviceModels
                .OrderBy(m => m.DeviceModel.SortOrder).ThenBy(m => m.DeviceModel.Name)
                .Select(m => new IdNameRes(m.DeviceModel.Id, m.DeviceModel.Name))
                .ToList(),
            ticket.Watchers.OrderBy(w => w.User.DisplayName).Select(w => Brief(w.User)).ToList(),
            ticket.Attachments.Where(a => a.CommentId is null).OrderBy(a => a.Id).Select(ToRes).ToList(),
            ticket.Comments
                .OrderBy(c => c.Id)
                .Select(c => new CommentRes(c.Id, Brief(c.Author), c.Body, c.CreatedAt, attachmentsByComment.GetValueOrDefault(c.Id) ?? []))
                .ToList(),
            ticket.Activities
                .OrderBy(a => a.Id)
                .Select(a => new ActivityRes(a.Id, a.Type, a.FromStatus, a.ToStatus, BriefOrNull(a.Actor), a.Note, a.Data, a.CreatedAt))
                .ToList(),
            TicketWorkflow.AllowedActions(TicketWorkflow.StateOf(ticket), me).Order().ToList());
    }

    public async Task<TicketDetailRes> AddAttachmentsAsync(int id, IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken)
    {
        var me = currentUser.User;
        var ticket = await FindVisibleAsync(id, cancellationToken);
        TicketWorkflow.Ensure(TicketWorkflow.StateOf(ticket), me, TicketAction.AddAttachment);
        AttachmentRules.Validate(files);

        var saved = new List<StoredFile>();
        try
        {
            await StoreFilesAsync(id, commentId: null, files, me.Id, saved, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            AttachmentStorage.DeleteQuietly(saved);
            throw;
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<TicketDetailRes> AddCommentAsync(int id, AddCommentForm form, CancellationToken cancellationToken)
    {
        var me = currentUser.User;
        var ticket = await FindVisibleAsync(id, cancellationToken);
        TicketWorkflow.Ensure(TicketWorkflow.StateOf(ticket), me, TicketAction.Comment);

        var body = form.Body?.Trim() ?? "";
        if (body.Length == 0 && form.Files.Count == 0)
        {
            throw new BusinessRuleException("Write something or attach a file.", ErrorCodes.EmptyComment);
        }

        AttachmentRules.Validate(form.Files);

        var comment = new TicketComment { TicketId = id, AuthorId = me.Id, Body = body };
        db.TicketComments.Add(comment);

        // The reporter answered: the ticket goes back to where it was waiting (Q32).
        if (ticket is { Status: TicketStatus.WaitingInfo, StatusBeforeWaiting: { } previous } && ticket.ReporterId == me.Id)
        {
            ticket.Status = previous;
            ticket.StatusBeforeWaiting = null;
            ticket.Activities.Add(new TicketActivity
            {
                Type = TicketActivityType.InfoProvided,
                FromStatus = TicketStatus.WaitingInfo,
                ToStatus = previous,
                ActorId = me.Id
            });
        }

        var saved = new List<StoredFile>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await StoreFilesAsync(id, comment.Id, form.Files, me.Id, saved, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            AttachmentStorage.DeleteQuietly(saved);
            throw;
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<AttachmentFile> OpenAttachmentAsync(int id, long attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await db.TicketAttachments.AsNoTracking()
                             .Where(a => a.Id == attachmentId && a.TicketId == id)
                             .Where(a => db.Tickets.VisibleTo(currentUser.User).Any(t => t.Id == a.TicketId))
                             .SingleOrDefaultAsync(cancellationToken)
                         ?? throw new NotFoundException("File not found.");

        var path = storage.PathOf(id, attachment.StoredName);
        if (!File.Exists(path))
        {
            throw new NotFoundException("The file is missing on the server.");
        }

        return new AttachmentFile(path, attachment.FileName, attachment.ContentType);
    }

    public async Task<PagedRes<TicketListItemRes>> ListAsync(TicketQueryReq request, CancellationToken cancellationToken)
    {
        var me = currentUser.User;
        var query = db.Tickets.AsNoTracking().VisibleTo(me).InScope(request.Scope, me);

        if (request.Statuses.Count > 0)
        {
            var statuses = request.Statuses;
            query = query.Where(t => statuses.Contains(t.Status));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var text = request.Search.Trim();
            query = query.Where(t => t.TicketNo.Contains(text)
                                     || (t.LegacyRef != null && t.LegacyRef.Contains(text))
                                     || t.Title.Contains(text)
                                     || t.Description.Contains(text)
                                     || (t.Resolution != null && t.Resolution.Contains(text))
                                     || (t.CustomerName != null && t.CustomerName.Contains(text)));
        }

        if (request.ProductId is { } productId)
        {
            query = query.Where(t => t.ProductId == productId);
        }

        if (request.AssigneeId is { } assigneeId)
        {
            query = query.Where(t => t.AssigneeId == assigneeId);
        }

        if (request.DepartmentId is { } departmentId)
        {
            query = query.Where(t => t.DepartmentId == departmentId);
        }

        var ordered = request.Scope switch
        {
            // "ทำทันที" first (Q22), then the oldest.
            TicketScope.ToTriage => query
                .OrderBy(t => t.RequestedPriority == TicketPriority.Immediate ? 0 : t.RequestedPriority == TicketPriority.Urgent ? 1 : 2)
                .ThenBy(t => t.ReportedAt),

            // What I am on, then my queue in the lead's order.
            TicketScope.ToDo => query
                .OrderBy(t => t.Status == TicketStatus.InProgress ? 0 : 1)
                .ThenBy(t => t.QueueOrder)
                .ThenBy(t => t.PlannedDueDate),

            TicketScope.ToApprove or TicketScope.ToClose => query.OrderBy(t => t.ReportedAt),
            _ => query.OrderByDescending(t => t.ReportedAt).ThenByDescending(t => t.Id)
        };

        var total = await query.CountAsync(cancellationToken);
        var today = DateOnly.FromDateTime(ThaiTime.Now(clock));
        var items = await ordered
            .Skip(request.Skip)
            .Take(request.PageLimit)
            .Select(t => new TicketListItemRes(
                t.Id,
                t.TicketNo,
                t.Title,
                t.Type,
                t.Product.Name,
                t.Status,
                t.RequestedPriority,
                t.ActualPriority,
                new UserBriefRes(t.Reporter.Id, t.Reporter.DisplayName),
                t.Requester == null ? null : new UserBriefRes(t.Requester.Id, t.Requester.DisplayName),
                t.Assignee == null ? null : new UserBriefRes(t.Assignee.Id, t.Assignee.DisplayName),
                t.Department.Name,
                t.CustomerName,
                t.ReportedAt,
                t.PlannedDueDate,
                t.PlannedDueDate != null && t.PlannedDueDate < today
                && (t.Status == TicketStatus.Queued || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingInfo)))
            .ToListAsync(cancellationToken);

        return new PagedRes<TicketListItemRes>(items, total);
    }

    public async Task<TicketCountsRes> CountsAsync(CancellationToken cancellationToken)
    {
        var me = currentUser.User;
        var visible = db.Tickets.AsNoTracking().VisibleTo(me);

        // Open work only: "ที่ฉันแจ้ง" counts what is not over yet.
        Task<int> Count(TicketScope scope) => visible.InScope(scope, me)
            .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled && t.Status != TicketStatus.Declined)
            .CountAsync(cancellationToken);

        return new TicketCountsRes(
            await Count(TicketScope.Reported),
            await Count(TicketScope.Following),
            await Count(TicketScope.ToDo),
            await Count(TicketScope.ToApprove),
            await Count(TicketScope.ToClose),
            await Count(TicketScope.ToTriage));
    }

    public async Task<List<string>> CustomerNamesAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.Tickets.AsNoTracking().VisibleTo(currentUser.User).Where(t => t.CustomerName != null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(t => t.CustomerName!.Contains(text));
        }

        return await query.Select(t => t.CustomerName!).Distinct().OrderBy(name => name).Take(20).ToListAsync(cancellationToken);
    }

    /// <summary>SW-2026-0001, numbered again from 1 each (Thai) year (Q36). Runs inside the caller's transaction.</summary>
    private async Task<string> NextTicketNoAsync(CancellationToken cancellationToken)
    {
        var year = ThaiTime.Now(clock).Year;
        var numbers = await db.Database.SqlQuery<int>($"""
            MERGE TicketSequences WITH (HOLDLOCK) AS target
            USING (SELECT {year} AS [Year]) AS source ON target.[Year] = source.[Year]
            WHEN MATCHED THEN UPDATE SET LastNumber = target.LastNumber + 1
            WHEN NOT MATCHED THEN INSERT ([Year], LastNumber) VALUES (source.[Year], 1)
            OUTPUT inserted.LastNumber AS [Value];
            """).ToListAsync(cancellationToken);
        return $"SW-{year}-{numbers.Single():D4}";
    }

    private async Task CheckDetailsAsync(SaveTicketReq request, int reporterId, DepartmentKind reporterKind, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
                      ?? throw new NotFoundException("System / program not found.");
        if (!product.IsActive)
        {
            throw new BusinessRuleException("This system / program is switched off.", ErrorCodes.InactiveProduct);
        }

        var modelIds = request.DeviceModelIds.Distinct().ToList();
        if (modelIds.Count > 0
            && await db.DeviceModels.CountAsync(m => modelIds.Contains(m.Id) && m.IsActive, cancellationToken) != modelIds.Count)
        {
            throw new BusinessRuleException("A device model is unknown or switched off.", ErrorCodes.InactiveDeviceModel);
        }

        if (request.RequesterId is { } requesterId && requesterId != reporterId
            && !await db.Users.AnyAsync(u => u.Id == requesterId && u.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("The requester is unknown or disabled.", ErrorCodes.InactiveUser);
        }

        // Customer Support says what it tried before a Bug or FeatureRequest reaches the programmers (Q55, Q56).
        if (reporterKind == DepartmentKind.Support && request.Type is TicketType.Bug or TicketType.FeatureRequest)
        {
            var template = (await settings.GetAsync(cancellationToken)).TestedDetailsTemplate;
            if (!HasOwnText(request.TestedDetails, template))
            {
                throw new BusinessRuleException("Fill in what has been tested.", ErrorCodes.TestedDetailsRequired);
            }
        }
    }

    /// <summary>Someone outside Support left the headings as they were: keep nothing rather than empty headings.</summary>
    private async Task DropUntouchedTemplateAsync(SaveTicketReq request, CancellationToken cancellationToken)
    {
        if (request.TestedDetails is null)
        {
            return;
        }

        var template = (await settings.GetAsync(cancellationToken)).TestedDetailsTemplate;
        if (!HasOwnText(request.TestedDetails, template))
        {
            request.TestedDetails = null;
        }
    }

    /// <summary>The text is not empty and is not just the untouched headings.</summary>
    private static bool HasOwnText(string? text, string template) =>
        !string.IsNullOrWhiteSpace(text) && Normalize(text) != Normalize(template);

    private static string Normalize(string text) => Whitespace().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    private static void Apply(Ticket ticket, SaveTicketReq request, int reporterId)
    {
        ticket.Title = request.Title.Trim();
        ticket.Type = request.Type;
        ticket.ProductId = request.ProductId;
        ticket.Version = NullIfBlank(request.Version);
        ticket.CustomerName = NullIfBlank(request.CustomerName);
        ticket.Description = request.Description.Trim();
        ticket.TestedDetails = NullIfBlank(request.TestedDetails);
        ticket.RequestedPriority = request.RequestedPriority;

        // ผู้ร้องขอ left empty = the reporter (Q57).
        ticket.RequesterId = request.RequesterId == reporterId ? null : request.RequesterId;
    }

    /// <summary>Old and new values for the timeline. Long texts only say that they changed.</summary>
    private async Task<List<object>> DescribeChangesAsync(Ticket ticket, SaveTicketReq request, CancellationToken cancellationToken)
    {
        var changes = new List<object>();

        void Compare(string field, string? oldValue, string? newValue)
        {
            if (!string.Equals(oldValue ?? "", newValue ?? "", StringComparison.Ordinal))
            {
                changes.Add(new { Field = field, Old = oldValue, New = newValue });
            }
        }

        void CompareLong(string field, string? oldValue, string? newValue)
        {
            if (!string.Equals(oldValue ?? "", newValue ?? "", StringComparison.Ordinal))
            {
                changes.Add(new { Field = field });
            }
        }

        Compare("Title", ticket.Title, request.Title.Trim());
        Compare("Type", ticket.Type.ToString(), request.Type.ToString());
        if (ticket.ProductId != request.ProductId)
        {
            var newName = await db.Products.Where(p => p.Id == request.ProductId).Select(p => p.Name).SingleAsync(cancellationToken);
            Compare("Product", ticket.Product.Name, newName);
        }

        Compare("Version", ticket.Version, NullIfBlank(request.Version));
        Compare("CustomerName", ticket.CustomerName, NullIfBlank(request.CustomerName));
        CompareLong("Description", ticket.Description, request.Description.Trim());
        CompareLong("TestedDetails", ticket.TestedDetails, NullIfBlank(request.TestedDetails));
        Compare("RequestedPriority", ticket.RequestedPriority.ToString(), request.RequestedPriority.ToString());

        var newRequesterId = request.RequesterId == ticket.ReporterId ? null : request.RequesterId;
        if (ticket.RequesterId != newRequesterId)
        {
            var newName = newRequesterId is null
                ? null
                : await db.Users.Where(u => u.Id == newRequesterId).Select(u => u.DisplayName).SingleAsync(cancellationToken);
            Compare("Requester", ticket.Requester?.DisplayName, newName);
        }

        var oldModels = ticket.DeviceModels.Select(m => m.DeviceModel.Name).Order().ToList();
        var newIds = request.DeviceModelIds.Distinct().ToList();
        var newModels = await db.DeviceModels.Where(m => newIds.Contains(m.Id)).Select(m => m.Name).OrderBy(n => n).ToListAsync(cancellationToken);
        Compare("DeviceModels", string.Join(", ", oldModels), string.Join(", ", newModels));

        return changes;
    }

    private async Task<Ticket> FindVisibleAsync(int id, CancellationToken cancellationToken) =>
        await db.Tickets.VisibleTo(currentUser.User).SingleOrDefaultAsync(t => t.Id == id, cancellationToken)
        ?? throw new NotFoundException("Ticket not found.");

    private async Task StoreFilesAsync(
        int ticketId,
        long? commentId,
        IReadOnlyCollection<IFormFile> files,
        int uploadedById,
        List<StoredFile> saved,
        CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            var stored = await storage.SaveAsync(ticketId, file, cancellationToken);
            saved.Add(stored);
            db.TicketAttachments.Add(new TicketAttachment
            {
                TicketId = ticketId,
                CommentId = commentId,
                FileName = stored.FileName,
                ContentType = stored.ContentType,
                SizeBytes = stored.SizeBytes,
                StoredName = stored.StoredName,
                UploadedById = uploadedById
            });
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static UserBriefRes Brief(User user) => new(user.Id, user.DisplayName);

    private static UserBriefRes? BriefOrNull(User? user) => user is null ? null : Brief(user);

    private static AttachmentRes ToRes(TicketAttachment attachment) => new(
        attachment.Id,
        attachment.FileName,
        attachment.ContentType,
        attachment.SizeBytes,
        Brief(attachment.UploadedBy),
        attachment.CreatedAt);
}
