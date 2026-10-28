using SWCare.Api.DTO.Req;
using SWCare.Api.DTO.Res;

namespace SWCare.Api.BLC.Control.IControls;

public interface ITicketControl
{
    /// <summary>A department head's own ticket skips approval (Q14).</summary>
    Task<TicketDetailRes> CreateAsync(SaveTicketReq request, IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken);

    /// <summary>Reporter only, until the department head approves (Q33).</summary>
    Task<TicketDetailRes> UpdateAsync(int id, SaveTicketReq request, CancellationToken cancellationToken);

    /// <summary>404 when the caller may not see the ticket.</summary>
    Task<TicketDetailRes> GetAsync(int id, CancellationToken cancellationToken);

    Task<TicketDetailRes> AddAttachmentsAsync(int id, IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken);

    /// <summary>The reporter's answer sends a WaitingInfo ticket back to where it was (Q32).</summary>
    Task<TicketDetailRes> AddCommentAsync(int id, AddCommentForm form, CancellationToken cancellationToken);

    Task<AttachmentFile> OpenAttachmentAsync(int id, long attachmentId, CancellationToken cancellationToken);

    /// <summary>Customer names used before, for suggestions (Q42).</summary>
    Task<List<string>> CustomerNamesAsync(string? search, CancellationToken cancellationToken);
}
