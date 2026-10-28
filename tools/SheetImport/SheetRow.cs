namespace SWCare.SheetImport;

/// <summary>One line of the tab "แจ้งงาน", as text.</summary>
public sealed record SheetRow(
    int Id,
    string Ref,
    string Product,
    string Description,
    string Requester,
    string Approver,
    string Department,
    string Priority,
    string Reporter,
    string ReportedAt,
    string Status,
    string StartedAt,
    string ResolvedAt,
    string Deadline,
    string Assignee,
    string Resolution)
{
    /// <summary>Id 34 is an empty line (plan 12).</summary>
    public bool IsEmpty => Product.Length == 0 && Description.Length == 0 && Reporter.Length == 0;

    /// <summary>
    /// Finds the header line ("Id", "ชื่อระบบ/ชื่อโปรแกรม", ...) and reads every line below it that has a number in Id.
    /// The QP / RQ column has no header; it is the column right after Id.
    /// </summary>
    public static List<SheetRow> Read(List<string[]> lines)
    {
        var headerIndex = lines.FindIndex(line => line.Any(cell => Key(cell) == "id"));
        if (headerIndex < 0)
        {
            throw new InvalidDataException("Header line with \"Id\" not found. Export the tab \"แจ้งงาน\" as CSV.");
        }

        var header = lines[headerIndex].Select(Key).ToList();
        int Column(string name, bool required = true)
        {
            var index = header.IndexOf(Key(name));
            if (index < 0 && required)
            {
                throw new InvalidDataException($"Column \"{name}\" not found in the header.");
            }

            return index;
        }

        var id = Column("Id");
        var refColumn = header.FindIndex(h => h.Contains("อ้างอิง"));
        if (refColumn < 0 && id + 1 < header.Count && header[id + 1].Length == 0)
        {
            refColumn = id + 1;
        }

        var columns = new
        {
            Product = Column("ชื่อระบบ/ชื่อโปรแกรม"),
            Description = Column("รายละเอียดที่แจ้ง/ปัญหา"),
            Requester = Column("ผู้ร้องขอ"),
            Approver = Column("ผู้อนุมัติร้องขอ"),
            Department = Column("ฝ่าย"),
            Priority = Column("ความเร่งด่วน"),
            Reporter = Column("ผู้แจ้งงาน"),
            ReportedAt = Column("วันที่แจ้ง"),
            Status = Column("สถานะ"),
            StartedAt = Column("เริ่มแก้ไขวันที่"),
            ResolvedAt = Column("วันที่แก้ไขเสร็จ"),
            Deadline = Column("Dead line", required: false),
            Assignee = Column("แก้ไขโดย"),
            Resolution = Column("การแก้ไข")
        };

        var rows = new List<SheetRow>();
        foreach (var line in lines.Skip(headerIndex + 1))
        {
            string Cell(int index) => index >= 0 && index < line.Length ? line[index].Trim() : "";

            if (!int.TryParse(Cell(id), out var rowId))
            {
                continue;
            }

            rows.Add(new SheetRow(
                rowId,
                Cell(refColumn),
                Cell(columns.Product),
                Cell(columns.Description),
                Cell(columns.Requester),
                Cell(columns.Approver),
                Cell(columns.Department),
                Cell(columns.Priority),
                Cell(columns.Reporter),
                Cell(columns.ReportedAt),
                Cell(columns.Status),
                Cell(columns.StartedAt),
                Cell(columns.ResolvedAt),
                Cell(columns.Deadline),
                Cell(columns.Assignee),
                Cell(columns.Resolution)));
        }

        return rows;
    }

    /// <summary>Header text without spaces, lower case: "Dead line" = "deadline".</summary>
    private static string Key(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
}
