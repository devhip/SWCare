import 'dart:convert';

import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Texts of the system in Thai and English (plan 8): the web's files plus app.json, loaded once at start.
/// Keys are "namespace:path", e.g. `ticket:action.Approve`; `{{name}}` is filled from [args] like i18next.
class Texts {
  Texts(this.lang, this._all);

  static const namespaces = ['common', 'auth', 'navigation', 'ticket', 'notification', 'errors', 'validation', 'dashboard', 'app'];

  final String lang;
  final Map<String, Map<String, Map<String, dynamic>>> _all;

  static Future<Map<String, Map<String, Map<String, dynamic>>>> load() async {
    final all = <String, Map<String, Map<String, dynamic>>>{};
    for (final lang in ['th', 'en']) {
      all[lang] = {
        for (final ns in namespaces)
          ns: jsonDecode(await rootBundle.loadString('assets/locales/$lang/$ns.json')) as Map<String, dynamic>,
      };
    }
    return all;
  }

  Texts withLang(String value) => Texts(value, _all);

  /// The text for [key], or the key itself when it is missing (so a gap shows on screen instead of crashing).
  String t(String key, [Map<String, Object?> args = const {}]) {
    final value = lookup(key);
    if (value == null) return key;
    return value.replaceAllMapped(RegExp(r'\{\{\s*(\w+)\s*\}\}'), (m) => '${args[m[1]] ?? ''}');
  }

  String? lookup(String key) {
    final split = key.indexOf(':');
    final ns = split < 0 ? 'app' : key.substring(0, split);
    final path = split < 0 ? key : key.substring(split + 1);
    Object? node = _all[lang]?[ns];
    for (final part in path.split('.')) {
      if (node is Map<String, dynamic>) {
        node = node[part];
      } else {
        return null;
      }
    }
    return node is String ? node : null;
  }
}

const _langKey = 'swcare.lang';

/// Set in main() after the files are loaded.
final textsSourceProvider = Provider<Map<String, Map<String, Map<String, dynamic>>>>((ref) => throw UnimplementedError());
final prefsProvider = Provider<SharedPreferences>((ref) => throw UnimplementedError());

class LangController extends Notifier<String> {
  @override
  String build() {
    final saved = ref.watch(prefsProvider).getString(_langKey);
    return saved == 'en' ? 'en' : 'th';
  }

  void set(String lang) {
    if (lang != 'th' && lang != 'en') return;
    state = lang;
    ref.read(prefsProvider).setString(_langKey, lang);
  }
}

final langProvider = NotifierProvider<LangController, String>(LangController.new);

final textsProvider = Provider<Texts>((ref) => Texts(ref.watch(langProvider), ref.watch(textsSourceProvider)));
