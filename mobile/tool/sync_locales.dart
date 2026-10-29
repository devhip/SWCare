// Copies the web's text files (web/locales/<lang>/*.json) into assets/locales/<lang>/, so the app and the web
// use the same words. The app's own texts live in assets/locales/<lang>/app.json and are not touched.
//
//   cd mobile && dart run tool/sync_locales.dart
import 'dart:io';

void main() {
  final web = Directory('../web/locales');
  if (!web.existsSync()) {
    stderr.writeln('web/locales not found - run from the mobile folder');
    exitCode = 1;
    return;
  }

  for (final lang in ['th', 'en']) {
    final target = Directory('assets/locales/$lang')..createSync(recursive: true);
    for (final file in Directory('${web.path}/$lang').listSync().whereType<File>()) {
      final name = file.uri.pathSegments.last;
      if (!name.endsWith('.json') || name == 'app.json') continue;
      file.copySync('${target.path}/$name');
      stdout.writeln('$lang/$name');
    }
  }
}
