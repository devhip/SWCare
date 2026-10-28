using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Data;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;
using SWCare.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace SWCare.Api.BLC.Control.Controls;

public sealed class DeviceModelControl(SWCareDbContext db, CurrentUser currentUser) : IDeviceModelControl
{
    public async Task<List<DeviceModelRes>> ListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.DeviceModels.AsNoTracking();
        if (!(includeInactive && currentUser.User.IsAdmin))
        {
            query = query.Where(m => m.IsActive);
        }

        return await query
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
            .Select(m => new DeviceModelRes(m.Id, m.Name, m.SortOrder, m.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<DeviceModelRes> CreateAsync(SaveDeviceModelReq request, CancellationToken cancellationToken)
    {
        var model = new DeviceModel();
        await ApplyAsync(model, request, cancellationToken);
        db.DeviceModels.Add(model);
        await db.SaveChangesAsync(cancellationToken);
        return ToRes(model);
    }

    public async Task<DeviceModelRes> UpdateAsync(int id, SaveDeviceModelReq request, CancellationToken cancellationToken)
    {
        var model = await db.DeviceModels.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
                    ?? throw new NotFoundException("Device model not found.");
        await ApplyAsync(model, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToRes(model);
    }

    private async Task ApplyAsync(DeviceModel model, SaveDeviceModelReq request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.DeviceModels.AnyAsync(m => m.Name == name && m.Id != model.Id, cancellationToken))
        {
            throw new ConflictException("Another device model has this name.", ErrorCodes.NameTaken);
        }

        model.Name = name;
        model.SortOrder = request.SortOrder;
        model.IsActive = request.IsActive;
    }

    private static DeviceModelRes ToRes(DeviceModel model) => new(model.Id, model.Name, model.SortOrder, model.IsActive);
}
