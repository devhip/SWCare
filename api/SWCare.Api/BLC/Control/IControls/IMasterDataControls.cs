using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;

namespace SWCare.Api.BLC.Control.IControls;

public interface IDepartmentControl
{
    /// <summary>includeInactive works for the Admin only.</summary>
    Task<List<DepartmentRes>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<DepartmentRes> CreateAsync(CreateDepartmentReq request, CancellationToken cancellationToken);
    Task<DepartmentRes> UpdateAsync(int id, UpdateDepartmentReq request, CancellationToken cancellationToken);
}

public interface IProductControl
{
    /// <summary>includeInactive works for the Admin only.</summary>
    Task<List<ProductRes>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<ProductRes> CreateAsync(SaveProductReq request, CancellationToken cancellationToken);
    Task<ProductRes> UpdateAsync(int id, SaveProductReq request, CancellationToken cancellationToken);
}

public interface IDeviceModelControl
{
    /// <summary>includeInactive works for the Admin only.</summary>
    Task<List<DeviceModelRes>> ListAsync(bool includeInactive, CancellationToken cancellationToken);

    Task<DeviceModelRes> CreateAsync(SaveDeviceModelReq request, CancellationToken cancellationToken);
    Task<DeviceModelRes> UpdateAsync(int id, SaveDeviceModelReq request, CancellationToken cancellationToken);
}

public interface ISettingControl
{
    Task<SettingsRes> GetAsync(CancellationToken cancellationToken);
    Task<SettingsRes> UpdateAsync(UpdateSettingsReq request, CancellationToken cancellationToken);
}
