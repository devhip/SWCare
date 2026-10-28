using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;

namespace SWCare.Api.BLC.Control.IControls;

public interface IUserControl
{
    Task<PagedRes<UserRes>> ListAsync(UserQueryReq request, CancellationToken cancellationToken);
    Task<UserRes> GetAsync(int id, CancellationToken cancellationToken);
    Task<UserRes> CreateAsync(CreateUserReq request, CancellationToken cancellationToken);

    /// <summary>Refused when it would leave a department without its head or a product owner outside Software.</summary>
    Task<UserRes> UpdateAsync(int id, UpdateUserReq request, CancellationToken cancellationToken);

    /// <summary>The user must change it at the next login; other sessions are logged out.</summary>
    Task ResetPasswordAsync(int id, ResetPasswordReq request, CancellationToken cancellationToken);

    /// <summary>Active users for pickers, at most 50.</summary>
    Task<List<UserLookupRes>> LookupAsync(string? search, CancellationToken cancellationToken);

    /// <summary>Active members of the Software department.</summary>
    Task<List<UserLookupRes>> ProgrammersAsync(CancellationToken cancellationToken);
}
