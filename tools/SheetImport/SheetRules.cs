using System.Globalization;
using System.Text.RegularExpressions;
using SWCare.Api.Models;

namespace SWCare.SheetImport;

/// <summary>How sheet text becomes ticket fields (plan 6.2 and 12, D7). Pure functions, covered by unit tests.</summary>
public static partial class SheetRules
{
    public const string OtherProduct = "อื่นๆ (Other)";

    /// <summary>Names typed in the sheet → product (plan 6.2). First match wins, so "LPR Payment" comes before "LPR".</summary>
    private static readonly (string[] Keys, string Product)[] ProductKeys =
    [
        (["lprpayment"], "HIP LPR Payment"),
        (["ezlpr", "lpr"], "HIP EzLPR"),
        (["carpark"], "HIP CarPark Solution"),
        (["easypark"], "HIP EasyPark"),
        (["easynext"], "HIP Easynext"),
        (["eztime"], "HIPEZTIME"),
        (["oncloud"], "HIP TIME On Cloud"),
        (["premium", "series8"], "HIP Premium Time (Series 8)"),
        (["hiptime", "time4.0"], "HIP TIME 4.0"),
        (["fixsolution"], "Fix Solutions")
    ];

    /// <summary>The product the sheet text names, or "อื่นๆ (Other)" (Windows App, ALL, ทดสอบ SDK...).</summary>
    public static string ProductName(string text)
    {
        var key = Compact(text);
        foreach (var (keys, product) in ProductKeys)
        {
            if (keys.Any(key.Contains))
            {
                return product;
            }
        }

        return OtherProduct;
    }

    /// <summary>"HIP EasyPark (ซีเลสเซียล)" → ซีเลสเซียล. Notes about the version or device in brackets are not customers.</summary>
    public static string? CustomerName(string productText)
    {
        var match = BracketRegex().Match(productText);
        if (!match.Success)
        {
            return null;
        }

        var inside = match.Groups[1].Value.Trim();
        return inside.Length == 0 || NotCustomerRegex().IsMatch(inside) ? null : inside;
    }

    /// <summary>D7: the sheet has no type, so it is guessed from the words; the import report lists it for checking.</summary>
    public static TicketType GuessType(string description)
    {
        var text = description.ToLowerInvariant();
        if (TestingRegex().IsMatch(text))
        {
            return TicketType.Testing;
        }

        // "นำเข้าข้อมูลไม่ได้" is a fault, not a data request.
        if (FaultRegex().IsMatch(text))
        {
            return TicketType.Bug;
        }

        if (DataServiceRegex().IsMatch(text))
        {
            return TicketType.DataService;
        }

        return FeatureRegex().IsMatch(text) ? TicketType.FeatureRequest : TicketType.Bug;
    }

    public static TicketPriority Priority(string text) => text switch
    {
        _ when text.Contains("ทันที") => TicketPriority.Immediate,
        _ when text.Contains("ด่วน") => TicketPriority.Urgent,
        _ => TicketPriority.Normal
    };

    /// <summary>dd/MM/yyyy. Null when empty; Invalid when the text is not a date or the year is off (e.g. "226").</summary>
    public static (DateOnly? Date, bool Invalid) Date(string text, int maxYear)
    {
        if (text.Length == 0)
        {
            return (null, false);
        }

        if (DateOnly.TryParseExact(text, ["d/M/yyyy", "d/M/yy", "d/M/yyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && date.Year >= 2000 && date.Year <= maxYear)
        {
            return (date, false);
        }

        return (null, true);
    }

    /// <summary>A date of the sheet is a Thai calendar day; stored as UTC at Thai midnight.</summary>
    public static DateTime ToUtc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);

    /// <summary>First line of the details, for the list and the cards.</summary>
    public static string Title(string description, string fallback, int maxLength = 120)
    {
        var line = description.Split('\n').Select(l => WhitespaceRegex().Replace(l, " ").Trim()).FirstOrDefault(l => l.Length > 0);
        line ??= fallback.Length > 0 ? fallback : "-";
        if (line.Length <= maxLength)
        {
            return line;
        }

        var cut = line[..maxLength];
        var space = cut.LastIndexOf(' ');
        return (space > maxLength / 2 ? cut[..space] : cut).TrimEnd() + "…";
    }

    /// <summary>Device model names found in the text, longest first so "CMiF68S Plus" is not also "CMiF68S".</summary>
    public static List<string> DeviceModels(string text, IEnumerable<string> modelNames)
    {
        var compact = Compact(text).Replace("-", "");
        var found = new List<string>();
        foreach (var name in modelNames.OrderByDescending(n => n.Length))
        {
            var key = Compact(name).Replace("-", "");
            if (key.Length > 0 && compact.Contains(key))
            {
                found.Add(name);
                compact = compact.Replace(key, "|");
            }
        }

        return found;
    }

    /// <summary>Lower case without spaces.</summary>
    public static string Compact(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();

    /// <summary>Lower case with single spaces, for matching names.</summary>
    public static string NameKey(string text) => WhitespaceRegex().Replace(text.Trim(), " ").ToLowerInvariant();

    [GeneratedRegex(@"\(([^)]*)\)")]
    private static partial Regex BracketRegex();

    [GeneratedRegex(@"2\s*vl|ล่าสุด|กล้อง|cmtv|series|\d+\.\d+", RegexOptions.IgnoreCase)]
    private static partial Regex NotCustomerRegex();

    [GeneratedRegex(@"^\s*ทดสอบ|ทดสอบ\s*(กล้อง|เครื่อง|การเชื่อมต่อ|sdk|ระบบ)")]
    private static partial Regex TestingRegex();

    [GeneratedRegex(@"ไม่ได้|ไม่สามารถ|ไม่บันทึก|ไม่แสดง|ไม่ทำงาน|ผิดพลาด|ค้าง|error|bug")]
    private static partial Regex FaultRegex();

    [GeneratedRegex(@"(ลบ|นำเข้า|ย้าย|โอน|ดึง|กู้|ล้าง)\s*ข้อมูล|restore|backup|import")]
    private static partial Regex DataServiceRegex();

    [GeneratedRegex(@"เพิ่มรุ่น|เพิ่มฟังก์ชัน|เพิ่มฟีเจอร์|เพิ่มเมนู|เพิ่มรายงาน|ขอเพิ่ม|เปลี่ยน\s*logo|รองรับ")]
    private static partial Regex FeatureRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
