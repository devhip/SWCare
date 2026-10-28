using SWCare.Api.Models;
using SWCare.SheetImport;

namespace SWCare.Api.Tests.Unit;

/// <summary>Plan 12: how the rows of the old sheet become tickets.</summary>
public sealed class SheetImportTests
{
    [Fact]
    public void Csv_keeps_commas_quotes_and_line_breaks_inside_quotes()
    {
        var rows = Csv.Parse("﻿a,\"b, c\",\"line 1\nline 2\",\"say \"\"hi\"\"\"\r\n1,,3,\n");

        Assert.Equal(2, rows.Count);
        Assert.Equal(["a", "b, c", "line 1\nline 2", "say \"hi\""], rows[0]);
        Assert.Equal(["1", "", "3", ""], rows[1]);
    }

    [Fact]
    public void Rows_are_read_by_header_and_the_ref_column_has_no_header()
    {
        const string csv = """
            ,,,
            Id,,ชื่อระบบ/ชื่อโปรแกรม,รายละเอียดที่แจ้ง/ปัญหา,ผู้ร้องขอ,ผู้อนุมัติร้องขอ,ฝ่าย,ความเร่งด่วน,ผู้แจ้งงาน,วันที่แจ้ง,สถานะ,เริ่มแก้ไขวันที่,วันที่แก้ไขเสร็จ,Dead line,แก้ไขโดย,การแก้ไข
            1,RQ130526,HIP TIME 4.0,ทดสอบกล้อง,สุรีย์,,Customer Support,ทำทันที,KUNG HIP,05/01/2025,ปิดงาน,08/01/2025,08/01/2025,,พี่ป้อม,ขอเอกสาร
            ,,,,,,,,,,แจ้งงาน,,,,,
            34,,,,,,,,,,ปิดงาน,,,,,
            """;

        var rows = SheetRow.Read(Csv.Parse(csv));

        Assert.Equal(2, rows.Count);
        Assert.Equal("RQ130526", rows[0].Ref);
        Assert.Equal("KUNG HIP", rows[0].Reporter);
        Assert.Equal("พี่ป้อม", rows[0].Assignee);
        Assert.True(rows[1].IsEmpty);
    }

    [Theory]
    [InlineData("HIP TIME 4.0 ( 2VL)", "HIP TIME 4.0")]
    [InlineData("HIPTime 4.0", "HIP TIME 4.0")]
    [InlineData("Premium Time Series 8 2.0.6", "HIP Premium Time (Series 8)")]
    [InlineData("HIP On Cloud", "HIP TIME On Cloud")]
    [InlineData("Eztime", "HIPEZTIME")]
    [InlineData("Carpark Solutions(One Tech)", "HIP CarPark Solution")]
    [InlineData("LprPayment (งานโรงพยาบาลลำปาง)", "HIP LPR Payment")]
    [InlineData("EzLpr", "HIP EzLPR")]
    [InlineData("LPR งาน PABX", "HIP EzLPR")]
    [InlineData("Hipeasynext(TheRiverBrooke)", "HIP Easynext")]
    [InlineData("Fix Solutions", "Fix Solutions")]
    [InlineData("Windows App", SheetRules.OtherProduct)]
    [InlineData("ALL", SheetRules.OtherProduct)]
    public void Product_names_typed_in_the_sheet_map_to_master_data(string text, string expected) =>
        Assert.Equal(expected, SheetRules.ProductName(text));

    [Theory]
    [InlineData("HIP EasyPark (ซีเลสเซียล)", "ซีเลสเซียล")]
    [InlineData("HIP TIME 4.0 ( 2VL)", null)]
    [InlineData("HIPTIME 4.0 (กล้อง CMTV924)", null)]
    [InlineData("HIP PremiumTime (ล่าสุด )", null)]
    [InlineData("HIP TIME 4.0", null)]
    public void Customer_comes_from_the_brackets(string text, string? expected) =>
        Assert.Equal(expected, SheetRules.CustomerName(text));

    [Theory]
    [InlineData("ทดสอบกล้อง CMTV924 เชื่อมต่อกับโปรแกรม", TicketType.Testing)]
    [InlineData("เพิ่มรุ่นเครื่องสแกน CMi511C/CMiF54C/CiF55C", TicketType.FeatureRequest)]
    [InlineData("เปลี่ยน LOGO ระบบเป็น LOGO แบบใหม่", TicketType.FeatureRequest)]
    [InlineData("ลบข้อมูลใน VM Attendance", TicketType.DataService)]
    [InlineData("นำเข้ารายชื่อ Excel ไม่ได้", TicketType.Bug)]
    [InlineData("นำเข้าข้อมูลไม่ได้", TicketType.Bug)]
    [InlineData("สมาชิกเข้าออกแล้วไม่บันทึก Log", TicketType.Bug)]
    public void Type_is_guessed_from_the_words(string description, TicketType expected) =>
        Assert.Equal(expected, SheetRules.GuessType(description));

    [Theory]
    [InlineData("ทำทันที", TicketPriority.Immediate)]
    [InlineData("ด่วน(1-2 วัน)", TicketPriority.Urgent)]
    [InlineData("ปกติ (ตามคิว)", TicketPriority.Normal)]
    [InlineData("", TicketPriority.Normal)]
    public void Priority_follows_the_sheet(string text, TicketPriority expected) =>
        Assert.Equal(expected, SheetRules.Priority(text));

    [Theory]
    [InlineData("05/01/2025", "2025-01-05", false)]
    [InlineData("5/1/2025", "2025-01-05", false)]
    [InlineData("", null, false)]
    [InlineData("13/03/226", null, true)] // the year typo in the sheet (plan 12)
    [InlineData("31/02/2025", null, true)]
    [InlineData("เมื่อวาน", null, true)]
    public void Dates_are_day_month_year(string text, string? expected, bool invalid)
    {
        var (date, isInvalid) = SheetRules.Date(text, 2027);

        Assert.Equal(expected is null ? null : DateOnly.Parse(expected), date);
        Assert.Equal(invalid, isInvalid);
    }

    [Fact]
    public void A_sheet_date_is_thai_midnight_in_utc() =>
        Assert.Equal(new DateTime(2025, 1, 4, 17, 0, 0, DateTimeKind.Utc), SheetRules.ToUtc(new DateOnly(2025, 1, 5)));

    [Fact]
    public void Title_is_the_first_line_cut_at_a_word()
    {
        Assert.Equal("เพิ่มรุ่นเครื่องสแกน CMi511C", SheetRules.Title("\n  เพิ่มรุ่นเครื่องสแกน   CMi511C\nฟังก์ชัน ezTIME", ""));
        Assert.Equal("HIP TIME 4.0", SheetRules.Title("", "HIP TIME 4.0"));

        var title = SheetRules.Title(string.Join(' ', Enumerable.Repeat("word", 40)), "", 30);
        Assert.Equal("word word word word word word…", title);
    }

    [Fact]
    public void Device_models_are_found_in_the_text_longest_first()
    {
        string[] models = ["CMiF68S", "CMiF68S Plus", "CiF93S-VL", "CiF55C"];

        var found = SheetRules.DeviceModels("เชื่อมต่อกับรุ่น CIF93S-VL และ CMiF68S Plus", models);

        Assert.Equal(["CMiF68S Plus", "CiF93S-VL"], found);
    }
}
