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
| NDK 28.2 + CMake | ใน Android SDK — sdkmanager ถูกแทนด้วย Android CLI: `cmdline-tools\latest\bin\android.exe sdk install ndk/28.2.13676358` |

- iOS ต้อง build บน Mac
- `android/gradle.properties` ปิด `kotlin.incremental` เพราะ pub cache (C:) กับโปรเจกต์ (E:) อยู่คนละไดรฟ์

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
- HTTP ธรรมดาใช้ได้เฉพาะ debug build (`android/app/src/debug/res/xml/network_security_config.xml`) สำหรับลองกับ API ในวง LAN — release build ต้องเป็น HTTPS (แผนข้อ 11)
- โฟลเดอร์ `web/` มีไว้ทดสอบหน้าจอบน Chrome เท่านั้น (`flutter build web --dart-define=API_URL=...`) ไม่ได้ใช้งานจริง — กล้อง/วิดีโอ และการเปิดไฟล์แนบใช้ได้บนมือถือเท่านั้น

## ทดสอบทุกบทบาท (บน emulator / มือถือ)

`integration_test/roles_test.dart` เปิดแอปจริง login ผ่านหน้า Login ทีละคน (14 บทบาท) ตรวจเมนู แท็บงานของฉัน หน้าที่เปิดได้ ปุ่มของงานที่ค้างทุกขั้น (13 งาน) แล้วกดปุ่มทุก flow (แจ้งงาน → ตีกลับ → แก้ไข → ส่งใหม่ → อนุมัติ → ขอข้อมูล → ตอบ → จัดคิว → เริ่ม → พัก → เลื่อนวัน → เปลี่ยนคนทำ → จบงาน → ส่งกลับ → ปิดงาน, ไม่ดำเนินการ, ยกเลิก, ผู้ติดตาม, ติดตาม/เลิกติดตาม, ภาษา) แล้วเทียบผลกับ API

- ต้องใช้ API ที่ต่อ**ฐานข้อมูลว่างใหม่** (test สร้างผู้ใช้และงานเอง) — ห้ามชี้ไปที่ฐานข้อมูลจริง
- รหัสผ่านส่งทาง `--dart-define` ตอนรัน ไม่เก็บในไฟล์

```bash
C:\flutter\bin\flutter.bat test integration_test/roles_test.dart -d emulator-5554 --dart-define=API_URL=http://10.0.2.2:5399 --dart-define=ADMIN_PW=<รหัส admin ตอนเริ่ม> --dart-define=TEST_PW=<รหัสของผู้ใช้ทดสอบ>
```

## ข้อความ (ภาษาไทย / อังกฤษ)

- ข้อความของระบบใช้ไฟล์ชุดเดียวกับเว็บ: `dart run tool/sync_locales.dart` copy `web/locales/{th,en}/*.json` มาไว้ที่ `assets/locales/` — แก้คำที่เว็บแล้ว sync
- ข้อความเฉพาะแอปอยู่ที่ `assets/locales/{th,en}/app.json` (ไม่ถูกทับ)
- ในโค้ดเรียก `texts.t('ticket:action.Approve')` (`namespace:path`, ไม่ใส่ namespace = `app`) — test `every key the app uses exists in both languages` ตรวจว่ามีครบทั้งสองภาษา
