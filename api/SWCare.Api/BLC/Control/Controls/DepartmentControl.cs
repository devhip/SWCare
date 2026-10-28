using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class DepartmentControl(SWCareDbContext db, CurrentUser currentUser) : IDepartmentControl
{
    public async Task<List<DepartmentRes>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.Departments.AsNoTracking();
        if (!(includeInactive && currentUser.User.IsAdmin))
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentRes(
                d.Id,
                d.Name,
                d.Kind,
                d.HeadUserId,
                d.HeadUser == null ? null : d.HeadUser.DisplayName,
                d.Members.Count(m => m.IsActive),
                d.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentRes> CreateAsync(CreateDepartmentReq request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, exceptId: null, cancellationToken);

        var department = new Department { Name = name, Kind = DepartmentKind.General };
        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(department.Id, cancellationToken);
    }

    public async Task<DepartmentRes> UpdateAsync(int id, UpdateDepartmentReq request, CancellationToken cancellationToken)
    {
        var department = await db.Departments.SingleOrDefaultAsync(d => d.Id == id, cancellationToken)
                         ?? throw new NotFoundException("Department not found.");

        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, exceptId: id, cancellationToken);

        if (request.HeadUserId is { } headUserId
            && !await db.Users.AnyAsync(u => u.Id == headUserId && u.DepartmentId == id && u.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("The head must be an active member of this department.", ErrorCodes.HeadNotMember);
        }

        if (department.IsActive && !request.IsActive)
        {
            if (department.Kind != DepartmentKind.General)
            {
                throw new BusinessRuleException("Customer Support and Software cannot be switched off.", ErrorCodes.DepartmentKindLocked);
            }

            if (await db.Users.AnyAsync(u => u.DepartmentId == id && u.IsActive, cancellationToken))
            {
                throw new BusinessRuleException("Move or disable its members first.", ErrorCodes.DepartmentHasMembers);
            }
        }

        department.Name = name;
        department.HeadUserId = request.HeadUserId;
        department.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task<DepartmentRes> GetAsync(int id, CancellationToken cancellationToken) =>
        (await ListAsync(includeInactive: true, cancellationToken)).Single(d => d.Id == id);

    private async Task EnsureNameFreeAsync(string name, int? exceptId, CancellationToken cancellationToken)
    {
        if (await db.Departments.AnyAsync(d => d.Name == name && d.Id != exceptId, cancellationToken))
        {
            throw new ConflictException("Another department has this name.", ErrorCodes.NameTaken);
        }
    }
}
