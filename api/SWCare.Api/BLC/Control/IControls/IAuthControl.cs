using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;

namespace SWCare.Api.BLC.Control.IControls;

public interface IAuthControl
{
    Task<LoginRes> LoginAsync(LoginReq request, CancellationToken cancellationToken);

    /// <summary>Each refresh token works once; the answer carries a new pair.</summary>
    Task<LoginRes> RefreshAsync(RefreshTokenReq request, CancellationToken cancellationToken);

    Task LogoutAsync(RefreshTokenReq request, CancellationToken cancellationToken);

    /// <summary>Logs out every other session of the user and answers new tokens for this one.</summary>
    Task<LoginRes> ChangePasswordAsync(ChangePasswordReq request, CancellationToken cancellationToken);

    Task<MeRes> GetMeAsync(CancellationToken cancellationToken);
    Task<MeRes> SetLanguageAsync(SetLanguageReq request, CancellationToken cancellationToken);
}
