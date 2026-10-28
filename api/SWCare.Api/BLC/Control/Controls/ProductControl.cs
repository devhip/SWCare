using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class ProductControl(SWCareDbContext db, CurrentUser currentUser) : IProductControl
{
    public async Task<List<ProductRes>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();
        if (!(includeInactive && currentUser.User.IsAdmin))
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new ProductRes(
                p.Id,
                p.Name,
                p.OwnerUserId,
                p.OwnerUser == null ? null : p.OwnerUser.DisplayName,
                p.SortOrder,
                p.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductRes> CreateAsync(SaveProductReq request, CancellationToken cancellationToken)
    {
        var product = new Product();
        await ApplyAsync(product, request, cancellationToken);
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(product.Id, cancellationToken);
    }

    public async Task<ProductRes> UpdateAsync(int id, SaveProductReq request, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Product not found.");
        await ApplyAsync(product, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task ApplyAsync(Product product, SaveProductReq request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Products.AnyAsync(p => p.Name == name && p.Id != product.Id, cancellationToken))
        {
            throw new ConflictException("Another product has this name.", ErrorCodes.NameTaken);
        }

        // The owner is suggested as the assignee at triage (Q19).
        if (request.OwnerUserId is { } ownerId
            && !await db.Users.AnyAsync(
                u => u.Id == ownerId && u.IsActive && u.Department.Kind == DepartmentKind.Software,
                cancellationToken))
        {
            throw new BusinessRuleException("The owner must be an active programmer.", ErrorCodes.OwnerNotProgrammer);
        }

        product.Name = name;
        product.OwnerUserId = request.OwnerUserId;
        product.SortOrder = request.SortOrder;
        product.IsActive = request.IsActive;
    }

    private async Task<ProductRes> GetAsync(int id, CancellationToken cancellationToken) =>
        (await ListAsync(includeInactive: true, cancellationToken)).Single(p => p.Id == id);
}
