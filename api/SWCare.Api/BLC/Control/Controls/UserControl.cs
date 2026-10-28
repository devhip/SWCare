using System.Linq.Expressions;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class UserControl(SWCareDbContext db, CurrentUser currentUser, TimeProvider clock) : IUserControl
{
    private static readonly Expression<Func<User, UserRes>> ToRes = u => new UserRes(
        u.Id,
        u.Username,
        u.DisplayName,
        u.Email,
        u.DepartmentId,
        u.Department.Name,
        u.Department.HeadUserId == u.Id,
        u.IsAdmin,
        u.IsActive,
        u.MustChangePassword,
        u.Language,
        u.LastLoginAt,
        u.CreatedAt,
        u.UpdatedAt);

    public async Task<PagedRes<UserRes>> ListAsync(UserQueryReq request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(u => u.Username.Contains(search)
                                     || u.DisplayName.Contains(search)
                                     || (u.Email != null && u.Email.Contains(search)));
        }

        if (request.DepartmentId is { } departmentId)
        {
            query = query.Where(u => u.DepartmentId == departmentId);
        }

        if (request.IsActive is { } isActive)
        {
            query = query.Where(u => u.IsActive == isActive);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.DisplayName)
            .Skip(request.Skip)
            .Take(request.PageLimit)
            .Select(ToRes)
            .ToListAsync(cancellationToken);
        return new PagedRes<UserRes>(items, total);
    }

    public async Task<UserRes> GetAsync(int id, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Id == id).Select(ToRes).SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("User not found.");

    public async Task<UserRes> CreateAsync(CreateUserReq request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new ConflictException("This username is taken.", ErrorCodes.UsernameTaken);
        }

        await EnsureEmailFreeAsync(email, exceptUserId: null, cancellationToken);
        await GetActiveDepartmentAsync(request.DepartmentId, cancellationToken);
        AuthControl.EnsureNotUsername(request.Password, username);

        var user = new User
        {
            Username = username,
            DisplayName = request.DisplayName.Trim(),
            Email = email,
            DepartmentId = request.DepartmentId,
            IsAdmin = request.IsAdmin,
            PasswordHash = PasswordHasher.Hash(request.Password),
            MustChangePassword = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<UserRes> UpdateAsync(int id, UpdateUserReq request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        if (user.Id == currentUser.User.Id && (!request.IsAdmin || !request.IsActive))
        {
            throw new BusinessRuleException("You cannot remove your own Admin access or disable yourself.", ErrorCodes.CannotChangeOwnAccess);
        }

        var email = NormalizeEmail(request.Email);
        await EnsureEmailFreeAsync(email, exceptUserId: user.Id, cancellationToken);

        var movesDepartment = user.DepartmentId != request.DepartmentId;
        var disables = user.IsActive && !request.IsActive;
        var newDepartment = movesDepartment
            ? await GetActiveDepartmentAsync(request.DepartmentId, cancellationToken)
            : null;

        if (movesDepartment || disables)
        {
            // Approvals stop if a department loses its head without the Admin noticing.
            var headOf = await db.Departments.Where(d => d.HeadUserId == user.Id).Select(d => d.Name).ToListAsync(cancellationToken);
            if (headOf.Count > 0)
            {
                throw new BusinessRuleException(
                    $"This user is the head of {string.Join(", ", headOf)}. Pick another head first.",
                    ErrorCodes.UserIsDepartmentHead);
            }

            // Product owners are suggested as assignees, so they must stay active programmers.
            var targetKind = newDepartment?.Kind
                             ?? await db.Departments.Where(d => d.Id == user.DepartmentId).Select(d => d.Kind).SingleAsync(cancellationToken);
            if (!request.IsActive || targetKind != DepartmentKind.Software)
            {
                var owns = await db.Products.Where(p => p.OwnerUserId == user.Id).Select(p => p.Name).ToListAsync(cancellationToken);
                if (owns.Count > 0)
                {
                    throw new BusinessRuleException(
                        $"This user owns {string.Join(", ", owns)}. Pick another owner first.",
                        ErrorCodes.UserOwnsProducts);
                }
            }
        }

        user.DisplayName = request.DisplayName.Trim();
        user.Email = email;
        user.DepartmentId = request.DepartmentId;
        user.IsAdmin = request.IsAdmin;
        user.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        if (disables)
        {
            await AuthControl.RevokeAllRefreshTokensAsync(db, user.Id, clock, cancellationToken);
        }

        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordReq request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("User not found.");

        AuthControl.EnsureNotUsername(request.NewPassword, user.Username);

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.MustChangePassword = true;
        await db.SaveChangesAsync(cancellationToken);
        await AuthControl.RevokeAllRefreshTokensAsync(db, user.Id, clock, cancellationToken);
    }

    public async Task<List<UserLookupRes>> LookupAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(u => u.DisplayName.Contains(text) || u.Username.Contains(text));
        }

        return await query
            .OrderBy(u => u.DisplayName)
            .Take(50)
            .Select(u => new UserLookupRes(u.Id, u.DisplayName, u.DepartmentId, u.Department.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<List<UserLookupRes>> ProgrammersAsync(CancellationToken cancellationToken) =>
        db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Department.Kind == DepartmentKind.Software)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserLookupRes(u.Id, u.DisplayName, u.DepartmentId, u.Department.Name))
            .ToListAsync(cancellationToken);

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private async Task EnsureEmailFreeAsync(string? email, int? exceptUserId, CancellationToken cancellationToken)
    {
        if (email is null)
        {
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email && u.Id != exceptUserId, cancellationToken))
        {
            throw new ConflictException("This e-mail belongs to another user.", ErrorCodes.EmailTaken);
        }
    }

    private async Task<Department> GetActiveDepartmentAsync(int departmentId, CancellationToken cancellationToken)
    {
        var department = await db.Departments.AsNoTracking().SingleOrDefaultAsync(d => d.Id == departmentId, cancellationToken)
                         ?? throw new NotFoundException("Department not found.");
        if (!department.IsActive)
        {
            throw new BusinessRuleException("This department is switched off.", ErrorCodes.InactiveDepartment);
        }

        return department;
    }
}
