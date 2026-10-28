namespace SWCare.Api.Models;

/// <summary>A value the Admin can change without a release (plan 6.2 การตั้งค่า). Keys are in AppSettingKeys.</summary>
public sealed class AppSetting : IAuditable
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public static class AppSettingKeys
{
    /// <summary>Working days (Mon–Fri) after the programmer finishes before the ticket closes itself (Q16, Q27).</summary>
    public const string AutoCloseWorkingDays = "Ticket.AutoCloseWorkingDays";

    /// <summary>"HH:mm", Thai time (Q47).</summary>
    public const string MorningReminderTime = "Notification.MorningReminderTime";

    /// <summary>Headings put into "สิ่งที่ทดสอบแล้ว" for the reporter to fill in (Q56).</summary>
    public const string TestedDetailsTemplate = "Ticket.TestedDetailsTemplate";

    /// <summary>"yyyy-MM-dd" (Thai date) of the last morning job, so it runs once a day. Not shown to the Admin.</summary>
    public const string MorningJobLastRun = "Job.MorningLastRun";
}

/// <summary>Values written by DbSeeder on the first start (plan 6.2).</summary>
public static class AppSettingDefaults
{
    public const int AutoCloseWorkingDays = 3;
    public const string MorningReminderTime = "08:30";

    public const string TestedDetailsTemplate =
        "- เวอร์ชันโปรแกรม / Firmware ของเครื่อง:\n" +
        "- ทดสอบที่ไหน (หน้างาน / office):\n" +
        "- ทำอะไรไปแล้วบ้าง (เช่น ping, restart เครื่อง, Download ซ้ำ, ลองเวอร์ชันล่าสุด, ลองกับเครื่องอื่น):\n" +
        "- ผลที่ได้ / ข้อความ Error:\n" +
        "- ทำซ้ำที่ office ได้ไหม (ได้ / ไม่ได้ / ยังไม่ได้ลอง):";
}
