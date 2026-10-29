import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:swcare/core/api_client.dart';
import 'package:swcare/core/format.dart';
import 'package:swcare/core/i18n.dart';
import 'package:swcare/features/notifications/notifications_page.dart';
import 'package:swcare/models/models.dart';

/// The real text files of the app (synced from the web), read from disk.
Map<String, Map<String, Map<String, dynamic>>> _files() => {
      for (final lang in ['th', 'en'])
        lang: {
          for (final ns in Texts.namespaces)
            ns: jsonDecode(File('assets/locales/$lang/$ns.json').readAsStringSync()) as Map<String, dynamic>,
        },
    };

void main() {
  late Texts th;
  late Texts en;

  setUpAll(() async {
    await initializeDateFormatting('th');
    await initializeDateFormatting('en');
    final all = _files();
    th = Texts('th', all);
    en = Texts('en', all);
  });

  test('texts come from the web files, by namespace and path', () {
    expect(th.t('ticket:action.Approve'), 'อนุมัติ');
    expect(en.t('ticket:action.Approve'), 'Approve');
    expect(th.t('ticket:status.WaitingInfo'), 'รอข้อมูลเพิ่ม');
  });

  test('{{name}} is filled like i18next; a missing key shows itself', () {
    expect(th.t('ticket:list.total', {'count': 5}), 'ทั้งหมด 5 งาน');
    expect(th.t('ticket:no.such.key'), 'ticket:no.such.key');
  });

  test('every key the app uses exists in both languages', () {
    final keys = RegExp(r"""\.t\('([a-zA-Z]+:[\w.]+|[\w.]+)'""");
    final missing = <String>[];
    for (final file in Directory('lib').listSync(recursive: true).whereType<File>().where((f) => f.path.endsWith('.dart'))) {
      for (final m in keys.allMatches(file.readAsStringSync())) {
        final key = m[1]!;
        if (key.contains(r'$')) continue;
        for (final texts in [th, en]) {
          if (texts.lookup(key) == null) missing.add('${texts.lang} $key (${file.path})');
        }
      }
    }
    expect(missing, isEmpty);
  });

  test('API errors show the text of their code', () {
    expect(ApiException('TESTED_DETAILS_REQUIRED', 'x').text(th), 'กรุณากรอกสิ่งที่ทดสอบแล้ว');
    expect(ApiException('NETWORK', null).text(th), th.t('errors.network'));
    expect(ApiException('HTTP_500', 'Boom').text(th), 'Boom');
    expect(ApiException('HTTP_500', null).text(th), th.t('errors:requestFailed'));
  });

  test('notification text uses Data in the reader language', () {
    final item = AppNotification.fromJson({
      'Id': 1,
      'Type': 'TicketTriaged',
      'TicketId': 7,
      'ActorName': 'หัวหน้า',
      'Data': jsonEncode({'TicketNo': 'SW-2026-0007', 'Assignee': 'พี่ป้อม', 'PlannedDueDate': '2026-11-03'}),
      'CreatedAt': '2026-10-29T04:00:00Z',
      'IsRead': false,
    });
    expect(notificationText(item, th), 'หัวหน้า จัดคิว SW-2026-0007 ให้ พี่ป้อม คาดว่าเสร็จ 03 พ.ย. 2026');
  });

  test('dates are Thai time with Gregorian years', () {
    expect(formatDateTime(DateTime.utc(2026, 10, 29, 4, 19), 'th'), '29 ต.ค. 2026 11:19');
    expect(formatDate('2026-11-03', 'en'), '03 Nov 2026');
    expect(addDays('2026-12-30', 3), '2027-01-02');
  });
}
