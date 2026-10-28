using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace SWCare.Api.Controllers;

/// <summary>Plan 9 / D11–D12. A department head sees only their own department.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Dashboard")]
[RequireRoles(AppRole.Executive, AppRole.Admin, AppRole.SupportHead, AppRole.ProgrammerLead, AppRole.DepartmentHead)]
public sealed class DashboardController(IDashboardControl control) : ControllerBase
{
    [HttpGet]
    public async Task<ResultViewModel<DashboardRes>> Get([FromQuery] DashboardReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.GetAsync(request, cancellationToken));

    /// <summary>.xlsx with the same filters.</summary>
    [HttpGet("Export")]
    public async Task<IActionResult> Export([FromQuery] DashboardReq request, CancellationToken cancellationToken)
    {
        var (content, fileName) = await control.ExportAsync(request, cancellationToken);
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
