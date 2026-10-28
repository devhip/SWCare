using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using SWCare.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace SWCare.Api.Controllers;

/// <summary>Accounts. Managed by the Admin; Lookup is open to every user for pickers.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Users")]
public sealed class UsersController(IUserControl control) : ControllerBase
{
    [HttpGet]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultsViewModel<UserRes>> List([FromQuery] UserQueryReq request, CancellationToken cancellationToken) =>
        ApiResult.OkPage(await control.ListAsync(request, cancellationToken), request);

    /// <summary>Active users whose name or username contains the text, at most 50.</summary>
    [HttpGet("Lookup")]
    public async Task<ResultsViewModel<UserLookupRes>> Lookup([FromQuery] string? search, CancellationToken cancellationToken) =>
        ApiResult.OkList(await control.LookupAsync(search, cancellationToken));

    /// <summary>Active programmers, for picking an assignee.</summary>
    [HttpGet("Programmers")]
    public async Task<ResultsViewModel<UserLookupRes>> Programmers(CancellationToken cancellationToken) =>
        ApiResult.OkList(await control.ProgrammersAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<UserRes>> Get(int id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.GetAsync(id, cancellationToken));

    [HttpPost]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<UserRes>> Create(CreateUserReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.CreateAsync(request, cancellationToken));

    /// <summary>422 USER_IS_DEPARTMENT_HEAD / USER_OWNS_PRODUCTS when the change would leave a gap.</summary>
    [HttpPut("{id:int}")]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<UserRes>> Update(int id, UpdateUserReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/ResetPassword")]
    [RequireRoles(AppRole.Admin)]
    public async Task<ResultViewModel<bool>> ResetPassword(int id, ResetPasswordReq request, CancellationToken cancellationToken)
    {
        await control.ResetPasswordAsync(id, request, cancellationToken);
        return ApiResult.Ok(true);
    }
}
