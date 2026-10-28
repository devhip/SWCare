# SWCare — คู่มือติดตั้งบน Server (IIS)

สัปดาห์แรกเข้าผ่าน **IP ในเครือข่ายบริษัทเท่านั้น** (แผนข้อ 11) — ยังไม่มี HTTPS อย่าเปิดออกอินเทอร์เน็ต

## ภาพรวม

| ส่วน | IIS site / app pool | Port | โฟลเดอร์ |
|---|---|---|---|
| เว็บ (ไฟล์ static) | `SWCare.Web` | 80 | `C:\SWCare\web` |
| API (.NET 10) | `SWCare.Api` | 5300 | `C:\SWCare\api` |
| ไฟล์แนบ | — | — | `C:\SWCareData\attachments` |
| สำรองข้อมูล | — | — | `C:\SWCareData\backup` |
| ฐานข้อมูล | SQL Server `.\SQLEXPRESS` | — | database `SWCare` |

ผู้ใช้เปิด `http://<IP>/` เว็บเรียก API ที่ `http://<IP>:5300` (อ่านจาก `web\config.json`)

## 1. เตรียม Server (ครั้งเดียว)

1. Windows Server + IIS — PowerShell (Administrator):
   ```powershell
   Install-WindowsFeature Web-Server, Web-Static-Content, Web-Default-Doc, Web-Http-Errors, Web-Filtering, Web-Mgmt-Console
   ```
2. **ASP.NET Core Hosting Bundle 10** จาก https://dotnet.microsoft.com/download/dotnet/10.0 (ติดตั้งหลัง IIS แล้วรัน `iisreset`)
3. SQL Server (Express ได้) และ `sqlcmd` — คนที่รันสคริปต์ต้องเป็น sysadmin ของ SQL Server
4. เวลาของเครื่องตรงก็พอ (ระบบใช้เวลาไทยเองไม่ขึ้นกับ time zone ของเครื่อง)

## 2. สร้างแพ็กเกจ (เครื่องนักพัฒนา)

```powershell
cd E:\API_Dotnet\SWCare
.\scripts\publish.ps1 -Zip        # → dist\SWCare-<วันที่>.zip
```

ในแพ็กเกจ: `api\`, `web\`, `migrate.sql` (รันซ้ำได้), `tools\SheetImport\`, `deploy-iis.ps1`, `backup.ps1`, `VERSION.txt` (commit ที่ build)

## 3. ติดตั้งครั้งแรก

copy zip ไปที่ Server แล้วแตกไฟล์ จากนั้น PowerShell **Run as Administrator**:

```powershell
Get-ChildItem C:\Install\SWCare -Recurse | Unblock-File
cd C:\Install\SWCare
.\deploy-iis.ps1 -PublicHost http://192.168.1.20      # IP ที่ผู้ใช้พิมพ์ในเบราว์เซอร์
```

สคริปต์ทำให้ทั้งหมด:

- สร้างโฟลเดอร์, app pool (No Managed Code), site ทั้งสอง, เปิด firewall port 80 / 5300 (เฉพาะ Domain / Private)
- สร้าง database `SWCare` + รัน `migrate.sql` + ให้สิทธิ์ app pool `IIS APPPOOL\SWCare.Api` อ่าน/เขียน database
- ใส่ค่าตั้งค่าลง `C:\SWCare\api\web.config` (ข้อ 5) — สุ่ม `Jwt__Key` ให้เอง
- เขียน `C:\SWCare\web\config.json` ให้ชี้ API
- ตรวจ `http://localhost:5300/health`

ท้ายสคริปต์จะแสดง **รหัสผ่านครั้งเดียวของ `admin`** → เข้า `http://<IP>/` ด้วย `admin` แล้วเปลี่ยนรหัสทันที จากนั้นลบบรรทัด `Seed__AdminPassword` ออกจาก `C:\SWCare\api\web.config`

ต่อด้วยใน Admin: สร้างแผนก/ตั้งหัวหน้า → สร้างบัญชีผู้ใช้ → ตั้งผู้รับผิดชอบหลักของแต่ละระบบ → นำเข้าข้อมูลเดิม (ข้อ 7)

ตัวเลือกอื่นของ `deploy-iis.ps1`: `-InstallRoot`, `-DataRoot`, `-WebPort`, `-ApiPort`, `-SqlServer`, `-Database`, `-ConnectionString`, `-SkipMigration`

## 4. อัปเดตเวอร์ชันใหม่

สร้างแพ็กเกจใหม่ (ข้อ 2) → แตกไฟล์บน Server → รัน `.\deploy-iis.ps1 -PublicHost http://192.168.1.20` คำสั่งเดิม

- หยุด app pool → copy ไฟล์ → migrate database → เปิด app pool (ผู้ใช้หลุดไม่กี่วินาที)
- **ไม่ทับ**: `api\web.config`, `api\appsettings.Production.json`, `api\logs`, `web\config.json`, `web\web.config`, โฟลเดอร์ไฟล์แนบ
- ควร backup (ข้อ 6) ก่อนอัปเดตทุกครั้ง

## 5. ค่าตั้งค่า (environment variables ใน `C:\SWCare\api\web.config`)

| ชื่อ | ค่า | หมายเหตุ |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ปิด Swagger / Scalar |
| `ConnectionStrings__SWCare` | `Server=.\SQLEXPRESS;Database=SWCare;Trusted_Connection=True;TrustServerCertificate=True` | SQL อยู่เครื่องอื่น → ใช้ SQL login ผ่าน `-ConnectionString` |
| `Jwt__Key` | สุ่ม 64 ตัวอักษร | เปลี่ยนแล้วทุกคนต้อง login ใหม่ |
| `Cors__AllowedOrigins__0` | `http://192.168.1.20` | **ต้องตรงกับที่อยู่ที่ผู้ใช้พิมพ์** ถ้าใช้ชื่อเครื่องด้วยให้เพิ่ม `Cors__AllowedOrigins__1` |
| `Storage__AttachmentsPath` | `C:\SWCareData\attachments` | app pool ต้องมีสิทธิ์ Modify |
| `Seed__AdminPassword` | สุ่ม (ครั้งแรก) | ใช้ตอนยังไม่มีบัญชีเท่านั้น — ลบได้หลัง login ครั้งแรก |

แก้ไฟล์แล้ว IIS จะ restart API เอง ไฟล์นี้มีรหัสลับ — อย่าส่งต่อหรือ commit

## 6. สำรองข้อมูล (ทุกคืน เก็บ 30 วัน — แผนข้อ 11)

copy `backup.ps1` ไปไว้ที่ `C:\SWCare\backup.ps1` แล้วลงทะเบียน (PowerShell Administrator):

```powershell
$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument '-NoProfile -ExecutionPolicy Bypass -File C:\SWCare\backup.ps1 -CopyTo \\nas\backup\SWCare'
$trigger = New-ScheduledTaskTrigger -Daily -At 23:00
Register-ScheduledTask -TaskName "SWCare backup" -Action $action -Trigger $trigger -User "SYSTEM" -RunLevel Highest
```

- ได้ `SWCare-<วันเวลา>.bak` + สำเนาโฟลเดอร์ไฟล์แนบ ลบ `.bak` ที่เก่ากว่า 30 วัน · ไม่มี NAS ให้ตัด `-CopyTo` ออก
- service account ของ SQL Server ต้องเขียน `C:\SWCareData\backup` ได้
- ทดสอบกู้คืนอย่างน้อยครั้งแรก: SSMS → Restore Database จากไฟล์ `.bak` เป็นชื่ออื่น

## 7. นำเข้าข้อมูลเดิม (ครั้งเดียว หลังสร้างบัญชีผู้ใช้)

รายละเอียดใน `tools\SheetImport\README.md` — บน Server:

```powershell
cd C:\Install\SWCare\tools\SheetImport
$env:ConnectionStrings__SWCare = "Server=.\SQLEXPRESS;Database=SWCare;Trusted_Connection=True;TrustServerCertificate=True"
.\SheetImport.exe --csv C:\Install\แจ้งงาน.csv --map C:\Install\import-map.json            # ดูรายงาน
.\SheetImport.exe --csv C:\Install\แจ้งงาน.csv --map C:\Install\import-map.json --apply    # บันทึกจริง
```

ลบไฟล์ CSV ออกจาก Server หลังนำเข้าเสร็จ

## 8. แก้ปัญหา

| อาการ | สาเหตุที่พบบ่อย |
|---|---|
| HTTP 500.19 ที่ API | ยังไม่ได้ติดตั้ง Hosting Bundle หรือติดตั้งก่อน IIS → ติดตั้งใหม่แล้ว `iisreset` |
| HTTP 500.30 / API ไม่ตอบ | ดู `C:\SWCare\api\logs\swcare-<วันที่>.log` และ Event Viewer → Application (IIS AspNetCore Module V2) ส่วนใหญ่คือ connection string / สิทธิ์ database |
| เข้าเว็บได้แต่ login ไม่ได้, Console เบราว์เซอร์ขึ้น CORS | `Cors__AllowedOrigins__0` ไม่ตรงกับที่อยู่ที่พิมพ์ (เช่นพิมพ์ชื่อเครื่องแทน IP) |
| เว็บขึ้นแต่ทุกหน้าโหลดไม่เสร็จ | `web\config.json` ชี้ API ผิด หรือ firewall ปิด port 5300 |
| แนบไฟล์ใหญ่แล้ว error 413 | `requestLimits` ใน `api\web.config` ถูกแก้ (ต้อง 210763776) |
| แนบไฟล์แล้ว error 500 | app pool ไม่มีสิทธิ์เขียน `C:\SWCareData\attachments` |
| เครื่องอื่นเปิดไม่ได้ แต่บน Server เปิดได้ | firewall / เครือข่ายคนละวง |

## 9. ก่อนเปิดให้เข้าจากนอกบริษัท (สัปดาห์ที่ 2–4)

ทำ subdomain + HTTPS ก่อน (เช่น `swcare.hipglobal.co.th`): ใส่ certificate ที่ IIS binding, เปลี่ยน `web\config.json` และ `Cors__AllowedOrigins__0` เป็น `https://…` — แอปมือถือต้องใช้ HTTPS
