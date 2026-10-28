# SWCare

ระบบแจ้งงานโปรแกรมเมอร์ (เว็บ + แอปมือถือ) แทน Google Sheet Tab "แจ้งงาน" — แผนและการตัดสินใจทั้งหมดอยู่ที่ [docs/SWCare-Plan.md](docs/SWCare-Plan.md)

| โฟลเดอร์ | คืออะไร |
|---|---|
| [api/](api) | ASP.NET Core .NET 10 Web API + SQL Server |
| [web/](web) | Next.js (static export) + MUI |
| [mobile/](mobile) | แอป Flutter (สัปดาห์ที่ 2–4) |
| [docs/](docs) | แผนงาน |

## เริ่มรันบนเครื่อง dev

ต้องมี .NET SDK 10, Node.js 20+ และ SQL Server Express (`.\SQLEXPRESS`)

```bash
# API — http://localhost:5300 (Scalar: /scalar)
cd api/SWCare.Api
dotnet user-secrets set Seed:AdminPassword "<รหัสผ่านเริ่มต้นของ admin>"   # ครั้งแรกเท่านั้น
dotnet run

# Web — http://localhost:3020
cd web
npm install
npm run dev
```

ครั้งแรก API จะสร้างฐานข้อมูล `SWCare` และบัญชี `admin` ให้ login ด้วยรหัสที่ตั้งไว้ แล้วระบบจะให้เปลี่ยนรหัสผ่านก่อนใช้งาน
