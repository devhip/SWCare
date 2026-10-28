using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SWCare.Api.Controllers;

/// <summary>Login with an account of the system (week 1). Google sign-in comes in week 2 (plan 8).</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
public sealed class AuthController(IAuthControl control) : ControllerBase
{
    /// <summary>401 INVALID_CREDENTIALS or ACCOUNT_DISABLED. Check User.MustChangePassword in the answer.</summary>
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<ResultViewModel<LoginRes>> Login(LoginReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.LoginAsync(request, cancellationToken));

    /// <summary>The refresh token works once; keep the new pair from the answer.</summary>
    [HttpPost("RefreshToken")]
    [AllowAnonymous]
    public async Task<ResultViewModel<LoginRes>> RefreshToken(RefreshTokenReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.RefreshAsync(request, cancellationToken));

    /// <summary>Ends this session even when the access token has already expired.</summary>
    [HttpPost("Logout")]
    [AllowAnonymous]
    public async Task<ResultViewModel<bool>> Logout(RefreshTokenReq request, CancellationToken cancellationToken)
    {
        await control.LogoutAsync(request, cancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>Logs out the user's other sessions and answers new tokens for this one.</summary>
    [HttpPost("ChangePassword")]
    [AllowWhilePasswordChangeRequired]
    public async Task<ResultViewModel<LoginRes>> ChangePassword(ChangePasswordReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.ChangePasswordAsync(request, cancellationToken));

    [HttpGet("Me")]
    [AllowWhilePasswordChangeRequired]
    public async Task<ResultViewModel<MeRes>> Me(CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.GetMeAsync(cancellationToken));

    [HttpPut("Me/Language")]
    [AllowWhilePasswordChangeRequired]
    public async Task<ResultViewModel<MeRes>> SetLanguage(SetLanguageReq request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await control.SetLanguageAsync(request, cancellationToken));
}
