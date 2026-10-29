import 'package:intl/intl.dart';

/// Users see Thai time (Q47) whatever the phone's zone is; the API sends UTC. Gregorian years in Thai too,
/// like web/lib/format.ts ("29 ต.ค. 2026 11:19").
DateTime _thai(DateTime utc) => utc.toUtc().add(const Duration(hours: 7));

String formatDateTime(DateTime? value, String lang) {
  if (value == null) return '-';
  return DateFormat('dd MMM yyyy HH:mm', lang).format(_thai(value));
}

/// A DateOnly from the API ("2026-11-03"): no time zone shift.
String formatDate(String? value, String lang) {
  if (value == null || value.isEmpty) return '-';
  return DateFormat('dd MMM yyyy', lang).format(DateTime.parse(value));
}

/// Today in Thai time as "yyyy-MM-dd" (the API's DateOnly).
String thaiToday() => DateFormat('yyyy-MM-dd').format(_thai(DateTime.now()));

String addDays(String date, int days) => DateFormat('yyyy-MM-dd').format(DateTime.parse(date).add(Duration(days: days)));

String formatSize(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(0)} KB';
  return '${(bytes / 1024 / 1024).toStringAsFixed(1)} MB';
}
