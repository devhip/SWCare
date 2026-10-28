using SWCare.Api.BLC;

namespace SWCare.Api.Services;

/// <summary>Limits for uploaded files (Q20). Large logs go as a Drive link instead.</summary>
public static class AttachmentRules
{
    public const long MaxFileBytes = 20L * 1024 * 1024;
    public const int MaxFilesPerRequest = 10;

    /// <summary>All files plus the form fields of one request.</summary>
    public const long MaxRequestBytes = MaxFileBytes * MaxFilesPerRequest + 1024 * 1024;

    /// <summary>Pictures, PDF, Excel, Word, zip, short videos, text logs.</summary>
    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".heic",
        ".pdf",
        ".xls", ".xlsx", ".csv",
        ".doc", ".docx",
        ".txt", ".log",
        ".zip", ".rar", ".7z",
        ".mp4", ".mov", ".webm"
    };

    public static void Validate(IReadOnlyCollection<IFormFile> files)
    {
        if (files.Count > MaxFilesPerRequest)
        {
            throw new BusinessRuleException($"At most {MaxFilesPerRequest} files at a time.", ErrorCodes.TooManyFiles);
        }

        foreach (var file in files)
        {
            if (file.Length > MaxFileBytes)
            {
                throw new BusinessRuleException($"{file.FileName} is larger than 20 MB.", ErrorCodes.FileTooLarge);
            }

            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
            {
                throw new BusinessRuleException($"{file.FileName}: this file type is not allowed.", ErrorCodes.FileTypeNotAllowed);
            }
        }
    }
}

/// <summary>A file written to disk, not yet in the database.</summary>
public sealed record StoredFile(string FileName, string ContentType, long SizeBytes, string StoredName, string FullPath);

/// <summary>
/// Files live on the server disk under Storage:AttachmentsPath/&lt;ticket id&gt;/ with a GUID name, and are only
/// served through the API after a permission check (plan 10).
/// </summary>
public sealed class AttachmentStorage
{
    private readonly string _root;

    public AttachmentStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Storage:AttachmentsPath"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "attachments")
            : Path.GetFullPath(configured, environment.ContentRootPath);
    }

    public async Task<StoredFile> SaveAsync(int ticketId, IFormFile file, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(_root, ticketId.ToString());
        Directory.CreateDirectory(folder);

        var storedName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        var fullPath = Path.Combine(folder, storedName);
        await using (var target = File.Create(fullPath))
        {
            await file.CopyToAsync(target, cancellationToken);
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        return new StoredFile(Path.GetFileName(file.FileName), contentType, file.Length, storedName, fullPath);
    }

    public string PathOf(int ticketId, string storedName) => Path.Combine(_root, ticketId.ToString(), storedName);

    /// <summary>Clean-up when the database save after an upload fails.</summary>
    public static void DeleteQuietly(IEnumerable<StoredFile> files)
    {
        foreach (var file in files)
        {
            try
            {
                File.Delete(file.FullPath);
            }
            catch (IOException)
            {
                // A leftover file is harmless; it is never served without a database row.
            }
        }
    }
}
