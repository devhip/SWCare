using SWCare.Api.Models;

namespace SWCare.Api.DTO.Res;

public sealed record DepartmentRes(
    int Id,
    string Name,
    DepartmentKind Kind,
    int? HeadUserId,
    string? HeadUserName,
    int MemberCount,
    bool IsActive);

public sealed record ProductRes(
    int Id,
    string Name,
    int? OwnerUserId,
    string? OwnerName,
    int SortOrder,
    bool IsActive);

public sealed record DeviceModelRes(
    int Id,
    string Name,
    int SortOrder,
    bool IsActive);

public sealed record SettingsRes(
    int AutoCloseWorkingDays,
    string MorningReminderTime,
    string TestedDetailsTemplate);
