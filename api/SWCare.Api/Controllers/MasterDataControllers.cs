using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace SWCare.Api.Controllers;

/// <summary>Departments and their heads (the approvers). Everyone reads; the Admin changes.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Master data")]
public sealed class DepartmentsController(IDepartmentControl control) : ControllerBase
{
    [HttpGet]
    public async Task<ResultsViewModel<DepartmentRes>> List([FromQuery] bool includeInactive, CancellationToken cancellationToken) =>
        ApiResult.OkList(await control.ListAsync(includeInactive, cancellationToken));

    [HttpPost]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<DepartmentRes>> Create(CreateDepartmentReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<DepartmentRes>> Update(int id, UpdateDepartmentReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.UpdateAsync(id, request, cancellationToken));
}

/// <summary>Systems / programs (ระบบ/โปรแกรม) and their main programmer.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Master data")]
public sealed class ProductsController(IProductControl control) : ControllerBase
{
    [HttpGet]
    public async Task<ResultsViewModel<ProductRes>> List([FromQuery] bool includeInactive, CancellationToken cancellationToken) =>
        ApiResult.OkList(await control.ListAsync(includeInactive, cancellationToken));

    [HttpPost]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<ProductRes>> Create(SaveProductReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<ProductRes>> Update(int id, SaveProductReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.UpdateAsync(id, request, cancellationToken));
}

[ApiController]
[Route("api/[controller]")]
[Tags("Master data")]
public sealed class DeviceModelsController(IDeviceModelControl control) : ControllerBase
{
    [HttpGet]
    public async Task<ResultsViewModel<DeviceModelRes>> List([FromQuery] bool includeInactive, CancellationToken cancellationToken) =>
        ApiResult.OkList(await control.ListAsync(includeInactive, cancellationToken));

    [HttpPost]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<DeviceModelRes>> Create(SaveDeviceModelReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.CreateAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<DeviceModelRes>> Update(int id, SaveDeviceModelReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.UpdateAsync(id, request, cancellationToken));
}

/// <summary>Auto-close days, reminder time and the "สิ่งที่ทดสอบแล้ว" headings.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Master data")]
public sealed class SettingsController(ISettingControl control) : ControllerBase
{
    [HttpGet]
    public async Task<ResultViewModel<SettingsRes>> Get(CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.GetAsync(cancellationToken));

    [HttpPut]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<SettingsRes>> Update(UpdateSettingsReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.UpdateAsync(request, cancellationToken));
}
