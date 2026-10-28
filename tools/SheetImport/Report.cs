namespace SWCare.SheetImport;

/// <summary>What the import would do: one line per row, then the counts and the names to map in import-map.json.</summary>
public static class Report
{
    public static void Print(List<SheetRow> rows, List<PlannedTicket> planned)
    {
        var skipped = rows.Where(r => r.IsEmpty).Select(r => r.Id).ToList();
        Console.WriteLine($"แถวในชีต {rows.Count} · นำเข้า {planned.Count} · ข้าม (แถวว่าง) {skipped.Count}{(skipped.Count > 0 ? $": Id {string.Join(", ", skipped)}" : "")}");
        Console.WriteLine();
        Console.WriteLine("Id   | สถานะ          | ประเภท          | ระบบ                          | รุ่นเครื่อง / ต้องตรวจ");
        foreach (var plan in planned)
        {
            var ticket = plan.Ticket;
            var extra = new List<string>();
            if (plan.DeviceModelNames.Count > 0)
            {
                extra.Add(string.Join(", ", plan.DeviceModelNames));
            }

            if (plan.Review.Count > 0)
            {
                extra.Add("⚠ " + string.Join("; ", plan.Review));
            }

            Console.WriteLine($"{plan.Row.Id,-4} | {ticket.Status,-14} | {ticket.Type,-15} | {Cut(plan.ProductName, 29),-29} | {string.Join(" · ", extra)}");
        }

        Console.WriteLine();
        Console.WriteLine("สถานะ:   " + Counts(planned.Select(p => p.Ticket.Status.ToString())));
        Console.WriteLine("ประเภท:  " + Counts(planned.Select(p => p.Ticket.Type.ToString())));
        Console.WriteLine("ระบบ:    " + Counts(planned.Select(p => p.ProductName)));
        Console.WriteLine($"ต้องตรวจ: {planned.Count(p => p.Ticket.NeedsLegacyReview)} งาน");

        var unmatched = planned.SelectMany(p => p.Unmatched).GroupBy(u => u.Name).OrderByDescending(g => g.Count()).ToList();
        Console.WriteLine();
        if (unmatched.Count == 0)
        {
            Console.WriteLine("ชื่อคนจับคู่กับบัญชีได้ทั้งหมด");
            return;
        }

        Console.WriteLine("ชื่อที่ยังจับคู่กับบัญชีไม่ได้ (จะเก็บเป็นข้อความ — ถ้าเป็นคนที่มีบัญชี ให้เพิ่มใน import-map.json \"People\"):");
        foreach (var group in unmatched)
        {
            Console.WriteLine($"  {group.Key}  ×{group.Count()}  ({string.Join(", ", group.Select(u => u.Column).Distinct())})");
        }
    }

    private static string Counts(IEnumerable<string> values) =>
        string.Join(" · ", values.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}"));

    private static string Cut(string text, int length) => text.Length <= length ? text : text[..(length - 1)] + "…";
}
