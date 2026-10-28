using ClosedXML.Excel;
using SWCare.Api.BLC;
using SWCare.Api.DTO.Res;
using SWCare.Api.Models;

namespace SWCare.Api.Services;

/// <summary>
/// Excel of the Dashboard (D12): sheet 1 = the numbers on screen, sheet 2 = the tickets reported in the range.
/// Labels in the user's language (th / en), dates in Thai time.
/// </summary>
public static class DashboardExcel
{
    private const string Orange = "#F06400";
    private const string Cream = "#FFF6EE";

    public static byte[] Build(
        DashboardRes data,
        IReadOnlyList<DashboardTicket> tickets,
        string? departmentName,
        string? productName,
        string language,
        DateTime createdThai)
    {
        var th = language != "en";
        string L(string thai, string english) => th ? thai : english;

        using var book = new XLWorkbook();
        var summary = book.Worksheets.Add(L("สรุป", "Summary"));
        var row = 1;

        summary.Cell(row, 1).Value = "SWCare Dashboard";
        summary.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml(Orange));
        row += 1;
        summary.Cell(row++, 1).Value = $"{L("ช่วงวันที่", "Range")}: {data.From:dd/MM/yyyy} – {data.To:dd/MM/yyyy}";
        summary.Cell(row++, 1).Value = $"{L("ฝ่าย", "Department")}: {departmentName ?? L("ทุกฝ่าย", "All departments")}";
        summary.Cell(row++, 1).Value = $"{L("ระบบ/โปรแกรม", "System / program")}: {productName ?? L("ทุกระบบ", "All systems")}";
        summary.Cell(row++, 1).Value = $"{L("ออกรายงาน", "Exported")}: {createdThai:dd/MM/yyyy HH:mm}";
        row++;

        var kpi = data.Kpi;
        row = Table(summary, row, L("ตัวเลขสรุป", "Key numbers"), [L("รายการ", "Item"), L("จำนวน", "Value")],
        [
            [L("งานค้างตอนนี้", "Open now"), kpi.OpenNow],
            [L("เลยกำหนดตอนนี้", "Overdue now"), kpi.OverdueNow],
            [L("รออนุมัติตอนนี้", "Waiting approval now"), kpi.WaitingApprovalNow],
            [L("รอจัดคิวตอนนี้", "Waiting triage now"), kpi.WaitingTriageNow],
            [L("งานเข้าใหม่ในช่วง", "New in range"), kpi.NewInRange],
            [L("ปิดงานในช่วง", "Closed in range"), kpi.ClosedInRange],
            [L("ไม่ดำเนินการในช่วง", "Declined in range"), kpi.DeclinedInRange],
            [L("ยกเลิกในช่วง", "Cancelled in range"), kpi.CancelledInRange],
            [L("เสร็จทันกำหนด (%)", "On time (%)"), kpi.OnTimePercent is { } percent ? percent : "-"],
            [L("เสร็จทันกำหนด (งาน)", "On time (tickets)"), $"{kpi.OnTimeCount} / {kpi.OnTimeBase}"],
            [L("เฉลี่ยแจ้ง → ปิด (วัน)", "Average report → close (days)"), kpi.AvgDaysToClose is { } average ? average : "-"],
        ]);

        row = Table(summary, row, L("แนวโน้มรายเดือน", "By month"), [L("เดือน", "Month"), L("งานเข้า", "New"), L("ปิดได้", "Closed")],
            data.Months.Select(m => new XLCellValue[] { m.Month, m.New, m.Closed }).ToList());
        row = Table(summary, row, L("งานค้างแยกสถานะ", "Open by status"), [L("สถานะ", "Status"), L("จำนวน", "Count")],
            data.OpenByStatus.Select(s => new XLCellValue[] { StatusName(s.Status, th), s.Count }).ToList());
        row = Breakdown(summary, row, L("แยกตามระบบ/โปรแกรม", "By system / program"), data.ByProduct, th, null);
        row = Breakdown(summary, row, L("แยกตามประเภทงาน", "By type"), data.ByType, th, key => TypeName(Enum.Parse<TicketType>(key), th));
        row = Breakdown(summary, row, L("แยกตามฝ่ายที่แจ้ง", "By reporting department"), data.ByDepartment, th, null);
        row = Breakdown(summary, row, L("ลูกค้าที่แจ้งมากที่สุด", "Top customers"), data.ByCustomer, th, null);
        row = Table(summary, row, L("ทีมโปรแกรมเมอร์", "Programmers"),
            [L("ชื่อ", "Name"), L("งานในมือ", "Open"), L("กำลังแก้ไข", "In progress"), L("เลยกำหนด", "Overdue"), L("จบงานในช่วง", "Finished in range"), L("เฉลี่ยวันแก้ไข", "Avg. working days"), L("เลื่อนวัน (ครั้ง)", "Reschedules")],
            data.Team.Select(m => new XLCellValue[] { m.Name, m.OpenNow, m.InProgressNow, m.OverdueNow, m.ResolvedInRange, m.AvgWorkDays is { } d ? d : "-", m.Reschedules }).ToList());
        row = Table(summary, row, L("เวลาเฉลี่ยแต่ละขั้น (งานที่ปิดในช่วง)", "Average days per step (tickets closed in range)"),
            [L("ขั้น", "Step"), L("เฉลี่ย (วัน)", "Average days"), L("จำนวนงาน", "Tickets")],
            data.Stages.Select(s => new XLCellValue[] { StageName(s.Stage, th), s.AvgDays is { } d ? d : "-", s.Count }).ToList());
        Table(summary, row, L("ไม่ดำเนินการ แยกตามฝ่าย", "Declined by department"), [L("ฝ่าย", "Department"), L("จำนวน", "Count")],
            data.Declines.Select(d => new XLCellValue[] { d.Department, d.Count }).ToList());
        summary.Column(1).Width = 42;
        summary.Columns(2, 7).Width = 16;

        var list = book.Worksheets.Add(L("รายการงาน", "Tickets"));
        string[] headers =
        [
            L("เลขที่", "No."), L("เลขอ้างอิงเดิม", "Old ref."), L("หัวข้อ", "Title"), L("ประเภท", "Type"), L("ระบบ/โปรแกรม", "System"),
            L("ลูกค้า/หน้างาน", "Customer"), L("ฝ่าย", "Department"), L("คนแจ้ง", "Reporter"), L("คนทำ", "Assignee"), L("สถานะ", "Status"),
            L("ความเร่งด่วน", "Priority"), L("วันที่แจ้ง", "Reported"), L("วันที่เริ่มแก้ไข", "Started"), L("วันที่แก้ไขเสร็จ", "Finished"),
            L("วันที่ปิดงาน", "Closed"), L("วันคาดว่าจะเสร็จ", "Due"), L("ทันกำหนด", "On time"), L("แจ้ง → ปิด (วัน)", "Report → close (days)"),
            L("เลื่อนวัน (ครั้ง)", "Reschedules"), L("ส่งกลับ (ครั้ง)", "Sent back"), L("ข้อมูลเดิมต้องตรวจ", "Old data to check")
        ];
        for (var c = 0; c < headers.Length; c++)
        {
            list.Cell(1, c + 1).Value = headers[c];
        }

        var r = 2;
        foreach (var t in tickets)
        {
            XLCellValue[] values =
            [
                t.TicketNo, t.LegacyRef ?? "", t.Title, TypeName(t.Type, th), t.ProductName, t.CustomerName ?? "", t.DepartmentName,
                t.ReporterName, t.AssigneeName ?? "", StatusName(t.Status, th), PriorityName(t.Priority, th),
                Date(t.ReportedAt), Date(t.StartedAt), Date(t.ResolvedAt), Date(t.ClosedAt),
                t.PlannedDueDate is { } due ? due.ToDateTime(TimeOnly.MinValue) : "",
                t.ResolvedAt is null || t.PlannedDueDate is null ? "" : DashboardStats.IsOnTime(t) ? L("ทัน", "Yes") : L("ไม่ทัน", "No"),
                t.Status == TicketStatus.Closed && DashboardStats.Days(t.ReportedAt, t.ClosedAt) is { } days ? Math.Round(days, 1) : "",
                t.RescheduleCount, t.ReturnCount, t.NeedsLegacyReview ? L("ใช่", "Yes") : ""
            ];
            for (var c = 0; c < values.Length; c++)
            {
                list.Cell(r, c + 1).Value = values[c];
            }

            r++;
        }

        var range = list.Range(1, 1, Math.Max(r - 1, 1), headers.Length);
        var table = range.CreateTable();
        table.Theme = XLTableTheme.TableStyleLight11;
        foreach (var column in new[] { 12, 13, 14, 15, 16 })
        {
            list.Column(column).Style.DateFormat.Format = "dd/MM/yyyy";
        }

        list.Columns().AdjustToContents(1, Math.Min(r, 300), 8, 60);
        list.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }

    private static XLCellValue Date(DateTime? utc) => utc is { } value ? ThaiTime.FromUtc(value) : "";

    /// <summary>A titled table; returns the row after it plus one empty row.</summary>
    private static int Table(IXLWorksheet sheet, int row, string title, string[] headers, IReadOnlyList<XLCellValue[]> rows)
    {
        sheet.Cell(row, 1).Value = title;
        sheet.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml(Orange));
        row++;
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = sheet.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml(Cream));
        }

        row++;
        foreach (var values in rows)
        {
            for (var c = 0; c < values.Length; c++)
            {
                sheet.Cell(row, c + 1).Value = values[c];
            }

            row++;
        }

        return row + 1;
    }

    private static int Breakdown(IXLWorksheet sheet, int row, string title, IReadOnlyList<BreakdownRes> items, bool th, Func<string, string>? label) =>
        Table(sheet, row, title, [th ? "ชื่อ" : "Name", th ? "งานเข้าในช่วง" : "New in range", th ? "ยังไม่จบ" : "Still open"],
            items.Select(i => new XLCellValue[] { label?.Invoke(i.Key) ?? i.Label, i.Count, i.Open }).ToList());

    private static string StatusName(TicketStatus status, bool th) => status switch
    {
        TicketStatus.PendingApproval => th ? "รออนุมัติ" : "Pending approval",
        TicketStatus.Rejected => th ? "ตีกลับ" : "Returned",
        TicketStatus.PendingTriage => th ? "รอจัดคิว" : "Pending triage",
        TicketStatus.WaitingInfo => th ? "รอข้อมูลเพิ่ม" : "Waiting for info",
        TicketStatus.Queued => th ? "อยู่ในคิว" : "Queued",
        TicketStatus.InProgress => th ? "กำลังแก้ไข" : "In progress",
        TicketStatus.Resolved => th ? "จบงาน (รอปิด)" : "Finished (to close)",
        TicketStatus.Closed => th ? "ปิดงาน" : "Closed",
        TicketStatus.Cancelled => th ? "ยกเลิก" : "Cancelled",
        TicketStatus.Declined => th ? "ไม่ดำเนินการ" : "Declined",
        _ => status.ToString()
    };

    private static string TypeName(TicketType type, bool th) => type switch
    {
        TicketType.Bug => "Bug",
        TicketType.FeatureRequest => th ? "ขอเพิ่มฟีเจอร์/รุ่นเครื่อง" : "Feature / device model",
        TicketType.Testing => th ? "ทดสอบ/R&D" : "Testing / R&D",
        TicketType.DataService => th ? "งานบริการข้อมูล" : "Data service",
        _ => type.ToString()
    };

    private static string PriorityName(TicketPriority priority, bool th) => priority switch
    {
        TicketPriority.Immediate => th ? "ทำทันที" : "Immediate",
        TicketPriority.Urgent => th ? "ด่วน (1–2 วัน)" : "Urgent (1–2 days)",
        _ => th ? "ปกติ (ตามคิว)" : "Normal (queue)"
    };

    private static string StageName(string stage, bool th) => stage switch
    {
        "Approval" => th ? "รออนุมัติ" : "Waiting approval",
        "Triage" => th ? "รอจัดคิว" : "Waiting triage",
        "Queue" => th ? "อยู่ในคิว" : "In queue",
        "Work" => th ? "กำลังแก้ไข" : "Being fixed",
        "Close" => th ? "รอปิด" : "Waiting to close",
        _ => stage
    };
}
