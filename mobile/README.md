# SWCare Mobile

แอป Flutter (Android / iOS) ของ SWCare ใช้ API ชุดเดียวกับเว็บ — ตามแผน `docs/SWCare-Plan.md` ข้อ 9, 10, 13.4 (Q44)

- ทุกบทบาทยกเว้นหน้า Admin · Kanban เป็นรายการแยกสถานะ ไม่มีลาก
- สแตก: Riverpod, go_router, dio, image_picker, file_picker (Push: Firebase `hip-notification-6d592` — ขั้นถัดไป)
- package id `com.hip.swcare`

## เครื่องมือ (เครื่อง dev)

| | ที่อยู่ |
|---|---|
| Flutter SDK 3.47 | `C:\flutter` (ไม่ได้ใส่ PATH — เรียก `C:\flutter\bin\flutter.bat`) |
| Android SDK | `%LOCALAPPDATA%\Android\Sdk` |
| JDK 17 | `%LOCALAPPDATA%\Android\jdk17` (ตั้งไว้ใน `flutter config --jdk-dir`) |

iOS ต้อง build บน Mac

## รัน

```bash
cd mobile
C:\flutter\bin\flutter.bat pub get
C:\flutter\bin\flutter.bat run -d chrome                                  # ทดสอบหน้าจอบน Chrome (API ที่ http://localhost:5300)
C:\flutter\bin\flutter.bat run --dart-define=API_URL=http://192.168.8.85:5300   # มือถือจริงในวง LAN
C:\flutter\bin\flutter.bat build apk --dart-define=API_URL=https://swcare.hipglobal.co.th
C:\flutter\bin\flutter.bat analyze && C:\flutter\bin\flutter.bat test
```

- ที่อยู่ API: ค่าจาก `--dart-define=API_URL` (ไม่ใส่ = `http://10.0.2.2:5300` บน Android emulator, `http://localhost:5300` ที่อื่น) เปลี่ยนได้ที่หน้า Login (ปุ่มที่อยู่เซิร์ฟเวอร์ด้านล่าง)
- Android / iOS บล็อก HTTP ธรรมดาเมื่อไม่ใช่ debug — ใช้งานจริงต้องเป็น HTTPS (แผนข้อ 11)

## ข้อความ (ภาษาไทย / อังกฤษ)

- ข้อความของระบบใช้ไฟล์ชุดเดียวกับเว็บ: `dart run tool/sync_locales.dart` copy `web/locales/{th,en}/*.json` มาไว้ที่ `assets/locales/` — แก้คำที่เว็บแล้ว sync
- ข้อความเฉพาะแอปอยู่ที่ `assets/locales/{th,en}/app.json` (ไม่ถูกทับ)
- ในโค้ดเรียก `texts.t('ticket:action.Approve')` (`namespace:path`, ไม่ใส่ namespace = `app`) — test `every key the app uses exists in both languages` ตรวจว่ามีครบทั้งสองภาษา
