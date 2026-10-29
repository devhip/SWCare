import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'core/api_client.dart';
import 'core/i18n.dart';
import 'core/router.dart';
import 'core/theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializeDateFormatting('th');
  await initializeDateFormatting('en');
  final prefs = await SharedPreferences.getInstance();
  final tokens = TokenStore(const FlutterSecureStorage());
  await tokens.load();
  final texts = await Texts.load();

  runApp(ProviderScope(
    overrides: [
      prefsProvider.overrideWithValue(prefs),
      tokenStoreProvider.overrideWithValue(tokens),
      textsSourceProvider.overrideWithValue(texts),
    ],
    // A failed call shows its error with a retry button instead of retrying on its own.
    retry: (_, _) => null,
    child: const SwcareApp(),
  ));
}

class SwcareApp extends ConsumerWidget {
  const SwcareApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lang = ref.watch(langProvider);
    return MaterialApp.router(
      title: 'SWCare',
      debugShowCheckedModeBanner: false,
      theme: buildTheme(),
      routerConfig: ref.watch(routerProvider),
      locale: Locale(lang),
      supportedLocales: const [Locale('th'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
    );
  }
}
