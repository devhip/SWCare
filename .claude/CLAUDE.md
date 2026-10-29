# SWCare — คู่มือสำหรับ Agent

ระบบแจ้งงานโปรแกรมเมอร์ (เว็บ + แอปมือถือ) แทน Google Sheet Tab "แจ้งงาน"

**ความคืบหน้าอยู่ที่ `docs/SWCare-Progress.md`** — ดูว่าทำถึงไหนก่อนเริ่ม และติ๊ก / เพิ่มรายการทุกครั้งที่ทำเสร็จ

**อ่าน `docs/SWCare-Plan.md` ก่อนทำงานทุกครั้ง** — requirement ทั้งหมดตกลงกับผู้ใช้แล้ว (Q1–Q57) ห้ามเปลี่ยนการตัดสินใจหรือถามซ้ำเรื่องที่ตกลงแล้ว ถ้าต้องตัดสินใจเรื่องใหม่ให้ถามผู้ใช้ แล้วเพิ่มลงข้อ 18 ของแผน

## โครงสร้าง

| โฟลเดอร์ | สแตก |
|---|---|
| `api/` | ASP.NET Core .NET 10, Controllers, EF Core + SQL Server, FluentValidation, Serilog |
| `web/` | Next.js 16 (static export) + MUI 9 + TanStack Query + zustand + i18next, npm |
| `mobile/` | Flutter — ยังไม่เริ่ม ใช้โครงจาก `E:\API_Dotnet\hip-easy-next\mobile\easy-next-app` |
| `tools/SheetImport` | นำเข้าชีตเดิมครั้งเดียว (ใช้ DbContext ของ API) — `tools/SheetImport/README.md` |
| `scripts/` | PowerShell: `publish.ps1`, `deploy-iis.ps1`, `backup.ps1` — `docs/Deploy.md` |

## API (`api/SWCare.Api`)

- โฟลเดอร์: `Controllers/` → `BLC/Control/IControls` + `Controls/` → `Data/` (DbContext, Configurations, Migrations) · `DTO/Req` (class + Validator ในไฟล์เดียวกัน) · `DTO/Res` (record) · `Models/` · `Services/`
- **ทุก response เป็น envelope** `{ Success, Message, Result, Code }` JSON แบบ PascalCase — คืน `ApiResult.Ok(...)` / `ApiResult.OkPage(...)`
- error: `throw` คลาสใน `BLC/ApiExceptions.cs` พร้อม code จาก `ErrorCodes` → `ApiExceptionHandler` แปลงเป็น envelope **เพิ่ม code ใหม่แล้วต้องเพิ่มข้อความใน `web/locales/{th,en}/errors.json` ด้วย**
- ทุก endpoint ต้อง login (FallbackPolicy) ยกเว้น `[AllowAnonymous]` · จำกัดบทบาทด้วย `[RequireRoles(AppRole.X)]`
- บทบาทไม่ได้เก็บใน token: `CurrentUserFilter` โหลดผู้ใช้จาก DB ทุก request → `CurrentUser.User` (`UserContext`) · โปรแกรมเมอร์ = แผนก Kind Software, หัวหน้าโปรแกรมเมอร์ = หัวหน้าแผนก Software, หัวหน้า Support = หัวหน้าแผนก Kind Support, Admin = `IsAdmin`, ผู้บริหาร = `IsExecutive`, ดูงานทั้งหมด = `CanViewAllTickets` · สองอย่างหลังเห็นทุกงาน (`SeesAllTickets`) แต่งานที่ไม่เกี่ยวกับตัวเองกดได้แค่ "ติดตาม" (`SeesAllReadOnly`)
- **กฎสถานะของงานทั้งหมดอยู่ที่ `BLC/TicketWorkflow.cs`** — endpoint ที่เปลี่ยนสถานะต้องเรียก `TicketWorkflow.Ensure(...)` และเว็บแสดงปุ่มจาก `AllowedActions` เท่านั้น แก้กฎแล้วต้องเพิ่ม unit test · ดึงงานผ่าน `.VisibleTo(user)` เสมอ (งานที่มองไม่เห็นตอบ 404)
- ไฟล์แนบเก็บที่ `Storage:AttachmentsPath` (ค่าเริ่มต้น `App_Data/attachments`) ดาวน์โหลดผ่าน API เท่านั้น
- ไม่มีการลบข้อมูล (Q54): ข้อมูลหลักใช้ `IsActive`, งานจบด้วยสถานะสุดท้าย · FK ทั้งหมดเป็น Restrict
- เวลาเก็บเป็น UTC · enum เก็บเป็น string

```bash
cd api && dotnet build SWCare.slnx
cd api && dotnet test SWCare.slnx                   # รวม tools/SheetImport ด้วย
cd api/SWCare.Api && dotnet run                     # http://localhost:5300 (ฟังทุก IP) — Scalar ที่ /scalar
cd api/SWCare.Api && dotnet ef migrations add <Name> -o Data/Migrations   # ครั้งแรกรัน dotnet tool restore
```

- Development: migrate ฐานข้อมูลเองตอนเริ่ม และ `DbSeeder` ใส่ข้อมูลตั้งต้นกับบัญชี `admin` (รหัสจาก user-secrets `Seed:AdminPassword` ถ้าไม่ตั้งจะสุ่มแล้วพิมพ์ทาง console ครั้งเดียว)
- ห้าม commit รหัสผ่าน / key ลง appsettings หรือ `.http`

## Web (`web/`)

- ลำดับชั้น: `types/` → `lib/*.api.ts` (เรียกผ่าน `lib/http.ts`) → `queries/` (TanStack Query) → หน้าใน `app/<Route>/page.tsx` ครอบด้วย `<AppShell roles={...}>`
- route เป็น PascalCase ลงท้าย `/` (static export, `trailingSlash`) · เพิ่มเมนูที่ `lib/menu.ts` เมื่อหน้าพร้อมแล้ว
- ข้อความบนจอทุกจุดผ่าน i18n (`locales/th`, `locales/en`) — แปลเฉพาะข้อความของระบบ ไม่แปลข้อมูลที่ผู้ใช้พิมพ์ · ภาษาไทยใช้คำว่า "แผนก" ไม่ใช้ "ฝ่าย"
- หน้าตาตาม HIPHR (D9): ฟอนต์ Prompt, สีจาก `hiphr`/semantic ใน `theme/tokens.ts`, การ์ดใช้ `sx={{ ...card }}` · **ขนาดตัวอักษรใช้ `typeScale` ตามหน้าที่เท่านั้น**: pageTitle · sectionTitle (หัวการ์ด) · body · small · label (ชื่อช่อง/หัวตาราง) · caption · kpi
- กราฟใน Dashboard ใช้สีจาก `VIZ` ใน `app/Dashboard/_components/Charts.tsx` — เพิ่มสีใหม่ต้องรัน dataviz validator ก่อน
- **theme ตั้ง breakpoint `sm` = 0 และ `spacing` = 4px** — ใช้ `md` เป็นจุดแบ่งมือถือ/จอใหญ่ และอย่าใช้ `Dialog maxWidth="sm"` (จะกว้าง 0)
- หลังเปลี่ยนสถานะงาน ให้ `invalidateQueries({ queryKey: TICKET_LISTS_KEY })` เพื่อให้รายการและตัวนับอัปเดต
- ปลายทาง API อ่านจาก `public/config.json` ตอนรัน — `{host}` = host ที่เปิดหน้าเว็บ จึงเปิดได้ทั้ง localhost และ IP ในวง LAN

```bash
cd web && npm run dev        # http://localhost:3020
cd web && npx tsc --noEmit && npx eslint . && npm run build   # ผลอยู่ที่ web/out
```

## Git

- ทำงานเสร็จและทดสอบแล้วทุกครั้ง: อัปเดต `docs/SWCare-Progress.md` → `git commit` → `git push origin main` ได้เลยไม่ต้องถาม · commit กับ push แยกคำสั่งกัน

## ภาษา

- เอกสารและข้อความถึงผู้ใช้เป็นภาษาไทย · ชื่อในโค้ดและคอมเมนต์เป็นภาษาอังกฤษ

## ติดตั้ง

- ขั้นตอนและค่าตั้งค่าบน Server ทั้งหมดอยู่ที่ `docs/Deploy.md`
- สคริปต์ `.ps1` ต้องเป็น ASCII เท่านั้น — Windows PowerShell 5.1 อ่านไฟล์ UTF-8 ที่ไม่มี BOM เป็น ANSI แล้ว `—` กลายเป็นเครื่องหมายคำพูด
