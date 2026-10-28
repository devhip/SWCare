using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SWCare.Api.Data;
using SWCare.Api.Models;

namespace SWCare.SheetImport;

/// <summary>import-map.json: what the rules cannot guess. Keys of Types / Products are sheet Ids.</summary>
public sealed class ImportMap
{
    /// <summary>Name in the sheet → username, e.g. "KUNG HIP": "kung".</summary>
    public Dictionary<string, string> People { get; set; } = [];

    /// <summary>"12": "DataService" — overrides the guessed type (D7).</summary>
    public Dictionary<string, TicketType> Types { get; set; } = [];

    /// <summary>"12": "HIP TIME 4.0" — overrides the product read from the sheet.</summary>
    public Dictionary<string, string> Products { get; set; } = [];

    public static ImportMap Load(string? path)
    {
        if (path is null)
        {
            return new ImportMap();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        return JsonSerializer.Deserialize<ImportMap>(File.ReadAllText(path), options) ?? new ImportMap();
    }
}

/// <summary>A sheet row turned into a ticket that is not saved yet.</summary>
public sealed class PlannedTicket(SheetRow row, Ticket ticket)
{
    public SheetRow Row { get; } = row;
    public Ticket Ticket { get; } = ticket;
    public string ProductName { get; set; } = "";
    public List<string> DeviceModelNames { get; } = [];

    /// <summary>Why the ticket gets the flag "ข้อมูลเดิมต้องตรวจ".</summary>
    public List<string> Review { get; } = [];

    /// <summary>Names that match no account, by column.</summary>
    public List<(string Column, string Name)> Unmatched { get; } = [];
}

/// <summary>Plan 12: builds tickets from the sheet rows, then saves them in one transaction.</summary>
public sealed class Importer(SWCareDbContext db, ImportMap map, string fallbackUsername)
{
    private readonly int maxYear = DateTime.UtcNow.Year + 1;

    public async Task<List<PlannedTicket>> PlanAsync(IEnumerable<SheetRow> rows, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking().ToListAsync(cancellationToken);
        var people = new PeopleLookup(users, map.People);
        var fallback = users.SingleOrDefault(u => u.Username.Equals(fallbackUsername, StringComparison.OrdinalIgnoreCase))
                       ?? throw new InvalidOperationException($"User \"{fallbackUsername}\" (--fallback-user) not found.");

        var departments = await db.Departments.AsNoTracking().ToListAsync(cancellationToken);
        var supportDepartment = departments.Single(d => d.Kind == DepartmentKind.Support);
        var products = await db.Products.AsNoTracking().ToListAsync(cancellationToken);
        var other = products.SingleOrDefault(p => p.Name == SheetRules.OtherProduct)
                    ?? throw new InvalidOperationException($"Product \"{SheetRules.OtherProduct}\" not found.");
        var models = await db.DeviceModels.AsNoTracking().Where(m => m.IsActive).ToListAsync(cancellationToken);

        var planned = new List<PlannedTicket>();
        foreach (var row in rows.Where(r => !r.IsEmpty))
        {
            var ticket = new Ticket { LegacyRef = Empty(row.Ref) };
            var plan = new PlannedTicket(row, ticket);

            // Product and customer
            var productName = map.Products.GetValueOrDefault(row.Id.ToString()) ?? SheetRules.ProductName(row.Product);
            var product = products.SingleOrDefault(p => p.Name == productName);
            if (product is null)
            {
                plan.Review.Add($"ไม่พบระบบ \"{productName}\" ในข้อมูลหลัก");
                product = other;
            }

            if (row.Product.Length == 0)
            {
                plan.Review.Add("ไม่มีชื่อระบบ");
            }

            ticket.ProductId = product.Id;
            plan.ProductName = product.Name;
            ticket.CustomerName = SheetRules.CustomerName(row.Product);

            // Text
            ticket.Description = row.Description.Length > 0 ? row.Description : row.Product.Length > 0 ? row.Product : "-";
            ticket.Title = SheetRules.Title(row.Description, row.Product);
            ticket.Type = map.Types.TryGetValue(row.Id.ToString(), out var type) ? type : SheetRules.GuessType(row.Description);
            ticket.RequestedPriority = SheetRules.Priority(row.Priority);
            ticket.Resolution = Empty(row.Resolution);
            foreach (var model in SheetRules.DeviceModels($"{row.Product} {row.Description}", models.Select(m => m.Name)))
            {
                plan.DeviceModelNames.Add(model);
                ticket.DeviceModels.Add(new TicketDeviceModel { DeviceModelId = models.First(m => m.Name == model).Id });
            }

            // People: names that match no account are kept as text (plan 12)
            var reporter = Person(plan, "ผู้แจ้งงาน", row.Reporter, people, out var reporterText);
            ticket.ReporterId = (reporter ?? fallback).Id;
            ticket.LegacyReporterName = reporter is null ? reporterText : null;

            var requester = Person(plan, "ผู้ร้องขอ", row.Requester, people, out var requesterText);
            ticket.RequesterId = requester is not null && requester.Id != ticket.ReporterId ? requester.Id : null;
            ticket.LegacyRequesterName = requester is null ? requesterText : null;

            var approver = Person(plan, "ผู้อนุมัติร้องขอ", row.Approver, people, out var approverText);
            ticket.ApproverId = approver?.Id;
            ticket.LegacyApproverName = approver is null ? approverText : null;

            var assignee = Person(plan, "แก้ไขโดย", row.Assignee, people, out var assigneeText);
            ticket.AssigneeId = assignee?.Id;
            ticket.LegacyAssigneeName = assignee is null ? assigneeText : null;

            var department = departments.FirstOrDefault(d => SheetRules.NameKey(d.Name) == SheetRules.NameKey(row.Department));
            ticket.DepartmentId = department?.Id ?? reporter?.DepartmentId ?? supportDepartment.Id;

            // Dates
            var reported = DateOf(plan, "วันที่แจ้ง", row.ReportedAt);
            var started = DateOf(plan, "เริ่มแก้ไขวันที่", row.StartedAt);
            var resolved = DateOf(plan, "วันที่แก้ไขเสร็จ", row.ResolvedAt);
            var deadline = DateOf(plan, "Dead line", row.Deadline);
            if (reported is null)
            {
                plan.Review.Add("ไม่มีวันที่แจ้ง");
            }

            if (reported is not null && started < reported)
            {
                plan.Review.Add("วันเริ่มแก้ไขก่อนวันแจ้ง");
            }

            if (started is not null && resolved < started)
            {
                plan.Review.Add("วันแก้ไขเสร็จก่อนวันเริ่มแก้ไข");
            }

            if (reported is not null && deadline < reported)
            {
                plan.Review.Add("Dead line ก่อนวันแจ้ง");
            }

            if (row.Resolution.Contains("ยังไม่ได้แก้ไข"))
            {
                plan.Review.Add("การแก้ไขเขียนว่า \"ยังไม่ได้แก้ไข\"");
            }

            var reportedDate = reported ?? started ?? resolved ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            ticket.ReportedAt = SheetRules.ToUtc(reportedDate);
            ticket.ApprovedAt = ticket.ReportedAt; // approved in the sheet already (plan 12)
            ticket.StartedAt = started is null ? null : SheetRules.ToUtc(started.Value);
            ticket.ResolvedAt = resolved is null ? null : SheetRules.ToUtc(resolved.Value);
            ticket.PlannedStartDate = started;
            ticket.PlannedDueDate = deadline;

            // Status (plan 12)
            if (row.Status.Contains("ปิด"))
            {
                ticket.Status = TicketStatus.Closed;
                ticket.ClosedAt = SheetRules.ToUtc(resolved ?? started ?? reportedDate);
            }
            else if (started is not null)
            {
                // "แจ้งงาน" with a start date = being worked on, by the product's main programmer
                ticket.AssigneeId ??= product.OwnerUserId;
                if (ticket.AssigneeId is null)
                {
                    plan.Review.Add("กำลังแก้ไขแต่ไม่มีคนทำ (ระบบนี้ยังไม่มีผู้รับผิดชอบหลัก) — ไปรอจัดคิว");
                    ticket.Status = TicketStatus.PendingTriage;
                }
                else
                {
                    ticket.Status = TicketStatus.InProgress;
                    ticket.ActualPriority = ticket.RequestedPriority;
                    ticket.TriagedAt = ticket.ReportedAt;
                    ticket.LegacyAssigneeName = null;
                }
            }
            else
            {
                ticket.Status = TicketStatus.PendingTriage;
                ticket.AssigneeId = null;
            }

            ticket.NeedsLegacyReview = plan.Review.Count > 0;
            var note = $"ชีตแจ้งงาน Id {row.Id}";
            if (row.Product.Length > 0 && row.Product != product.Name)
            {
                note += $" · ระบบในชีต: {row.Product}";
            }

            if (plan.Review.Count > 0)
            {
                note += $" · ต้องตรวจ: {string.Join(", ", plan.Review)}";
            }

            ticket.Activities.Add(new TicketActivity
            {
                Type = TicketActivityType.Imported,
                ToStatus = ticket.Status,
                ActorId = fallback.Id,
                Note = note
            });

            planned.Add(plan);
        }

        return planned;
    }

    /// <summary>Saves everything or nothing. Numbers follow the year of the report date: SW-2025-0001...</summary>
    public async Task ApplyAsync(List<PlannedTicket> planned, bool force, CancellationToken cancellationToken)
    {
        if (!force && await db.TicketActivities.AnyAsync(a => a.Type == TicketActivityType.Imported, cancellationToken))
        {
            throw new InvalidOperationException("This database already has imported tickets. Use --force to import again.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var plan in planned)
        {
            var ticket = plan.Ticket;
            ticket.TicketNo = await NextNumberAsync(ThaiYear(ticket.ReportedAt), cancellationToken);
            if (ticket.Status == TicketStatus.InProgress && ticket.AssigneeId is { } assigneeId)
            {
                ticket.QueueOrder = (await db.Tickets
                    .Where(t => t.AssigneeId == assigneeId && (t.Status == TicketStatus.Queued || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingInfo))
                    .MaxAsync(t => (int?)t.QueueOrder, cancellationToken) ?? 0) + 1;
            }

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static int ThaiYear(DateTime utc) => utc.AddHours(7).Year;

    /// <summary>Same numbering as TicketControl: one counter per year in TicketSequences.</summary>
    private async Task<string> NextNumberAsync(int year, CancellationToken cancellationToken)
    {
        var numbers = await db.Database.SqlQuery<int>($"""
            MERGE TicketSequences WITH (HOLDLOCK) AS target
            USING (SELECT {year} AS [Year]) AS source ON target.[Year] = source.[Year]
            WHEN MATCHED THEN UPDATE SET LastNumber = target.LastNumber + 1
            WHEN NOT MATCHED THEN INSERT ([Year], LastNumber) VALUES (source.[Year], 1)
            OUTPUT inserted.LastNumber AS [Value];
            """).ToListAsync(cancellationToken);
        return $"SW-{year}-{numbers.Single():D4}";
    }

    private DateOnly? DateOf(PlannedTicket plan, string column, string text)
    {
        var (date, invalid) = SheetRules.Date(text, maxYear);
        if (invalid)
        {
            plan.Review.Add($"{column} ผิดรูปแบบ \"{text}\"");
        }

        return date;
    }

    private static User? Person(PlannedTicket plan, string column, string name, PeopleLookup people, out string? text)
    {
        text = Empty(name);
        if (text is null)
        {
            return null;
        }

        var user = people.Find(text);
        if (user is null)
        {
            plan.Unmatched.Add((column, text));
        }

        return user;
    }

    private static string? Empty(string text) => text.Length == 0 ? null : text;
}

/// <summary>
/// Sheet name → account: import-map.json first, then username, e-mail, the part of the e-mail before @, or display name.
/// A key that fits two accounts matches neither.
/// </summary>
public sealed class PeopleLookup
{
    private readonly Dictionary<string, User?> byKey = [];
    private readonly Dictionary<string, User?> aliases = [];

    public PeopleLookup(IEnumerable<User> users, Dictionary<string, string> aliasMap)
    {
        var list = users.ToList();
        foreach (var user in list)
        {
            Add(user.Username, user);
            Add(user.DisplayName, user);
            if (user.Email is { Length: > 0 } email)
            {
                Add(email, user);
                Add(email.Split('@')[0], user);
            }
        }

        foreach (var (name, username) in aliasMap)
        {
            aliases[SheetRules.NameKey(name)] = list.SingleOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                                               ?? throw new InvalidOperationException($"import-map.json: \"{name}\" points to user \"{username}\", which does not exist.");
        }
    }

    public User? Find(string name)
    {
        var key = SheetRules.NameKey(name);
        return aliases.TryGetValue(key, out var alias) ? alias : byKey.GetValueOrDefault(key);
    }

    private void Add(string text, User user)
    {
        var key = SheetRules.NameKey(text);
        if (key.Length == 0)
        {
            return;
        }

        byKey[key] = byKey.TryGetValue(key, out var existing) && existing?.Id != user.Id ? null : user;
    }
}
