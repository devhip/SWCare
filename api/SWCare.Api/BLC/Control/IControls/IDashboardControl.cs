using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;

namespace SWCare.Api.BLC.Control.IControls;

public interface IDashboardControl
{
    Task<DashboardRes> GetAsync(DashboardReq request, CancellationToken cancellationToken);

    /// <summary>The same numbers plus the tickets reported in the range, as an .xlsx file (D12).</summary>
    Task<(byte[] Content, string FileName)> ExportAsync(DashboardReq request, CancellationToken cancellationToken);
}
