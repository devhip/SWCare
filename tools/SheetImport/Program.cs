using System.Text;
using Microsoft.EntityFrameworkCore;
using SWCare.Api.Data;
using SWCare.Api.Data.Interceptors;
using SWCare.SheetImport;

// Plan 12: dotnet run --project tools/SheetImport -- --csv แจ้งงาน.csv --connection "..." [--map import-map.json] [--apply]
// Without --apply nothing is written: it prints what would be imported, so names and types can be checked first.

Console.OutputEncoding = Encoding.UTF8;

var options = ParseArgs(args);
if (options is null)
{
    Console.WriteLine("""
        SWCare SheetImport — นำเข้าข้อมูลจาก Tab "แจ้งงาน" (แผนข้อ 12)

          --csv <file>            ไฟล์ CSV ที่ Export จาก Google Sheet (จำเป็น)
          --connection <text>     connection string ของฐานข้อมูล SWCare (หรือตั้ง env ConnectionStrings__SWCare)
          --map <file>            import-map.json: จับคู่ชื่อคน / แก้ประเภทงาน / แก้ระบบ (ดู import-map.example.json)
          --fallback-user <name>  บัญชีที่เป็นคนแจ้งแทนเมื่อชื่อในชีตจับคู่ไม่ได้ (ค่าเริ่มต้น admin)
          --apply                 บันทึกจริง (ถ้าไม่ใส่ = แสดงรายงานอย่างเดียว)
          --force                 นำเข้าซ้ำแม้ฐานข้อมูลมีงานที่นำเข้าแล้ว
        """);
    return 1;
}

var connection = options.Connection ?? Environment.GetEnvironmentVariable("ConnectionStrings__SWCare");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("ไม่มี connection string: ใส่ --connection หรือตั้ง env ConnectionStrings__SWCare");
    return 1;
}

try
{
    return await RunAsync(options, connection);
}
catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or IOException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine($"นำเข้าไม่ได้: {ex.Message}");
    return 1;
}

static async Task<int> RunAsync(Options options, string connection)
{
    var rows = SheetRow.Read(Csv.Parse(File.ReadAllText(options.CsvPath, Encoding.UTF8)));
    var map = ImportMap.Load(options.MapPath);

    var dbOptions = new DbContextOptionsBuilder<SWCareDbContext>()
        .UseSqlServer(connection)
        .AddInterceptors(new AuditSaveChangesInterceptor(new AuditContext { Actor = "sheet-import" }, TimeProvider.System))
        .Options;
    await using var db = new SWCareDbContext(dbOptions);

    var importer = new Importer(db, map, options.FallbackUser);
    var planned = await importer.PlanAsync(rows, CancellationToken.None);
    Report.Print(rows, planned);

    if (!options.Apply)
    {
        Console.WriteLine();
        Console.WriteLine("ยังไม่ได้บันทึก — ตรวจรายงานแล้วรันซ้ำพร้อม --apply");
        return 0;
    }

    if (planned.Count == 0)
    {
        Console.WriteLine("ไม่มีงานให้นำเข้า");
        return 0;
    }

    await importer.ApplyAsync(planned, options.Force, CancellationToken.None);
    Console.WriteLine();
    Console.WriteLine($"บันทึกแล้ว {planned.Count} งาน: {planned.First().Ticket.TicketNo} ... {planned.Last().Ticket.TicketNo}");
    return 0;
}

static Options? ParseArgs(string[] args)
{
    string? csv = null, connection = null, map = null, fallback = "admin";
    bool apply = false, force = false;
    for (var i = 0; i < args.Length; i++)
    {
        string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
        switch (args[i])
        {
            case "--csv": csv = Next(); break;
            case "--connection": connection = Next(); break;
            case "--map": map = Next(); break;
            case "--fallback-user": fallback = Next(); break;
            case "--apply": apply = true; break;
            case "--force": force = true; break;
            default: return null;
        }
    }

    return csv is null ? null : new Options(csv, connection, map, fallback, apply, force);
}

internal sealed record Options(string CsvPath, string? Connection, string? MapPath, string FallbackUser, bool Apply, bool Force);
