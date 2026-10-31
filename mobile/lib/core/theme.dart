import 'package:flutter/material.dart';

/// The web's tokens (web/theme/tokens.ts, HIPHR look D9) for the app.
abstract final class AppColors {
  static const orange600 = Color(0xFFF06400);
  static const orange500 = Color(0xFFFF7A1A);
  static const orange400 = Color(0xFFFF9A3C);
  static const orange100 = Color(0xFFFFEBDA);
  static const orange50 = Color(0xFFFFF6EE);
  static const cream = Color(0xFFFFF8F1);
  static const ink = Color(0xFF2B1A0E);
  static const muted = Color(0xFF8A6A55);
  static const mutedLight = Color(0xFFB39A88);
  static const line = Color(0xFFF3DCC8);
  static const lineSoft = Color(0xFFF6E4D4);

  static const successBg = Color(0xFFE9F7EF);
  static const successText = Color(0xFF157A3C);
  static const warningBg = Color(0xFFFFF7E6);
  static const warningText = Color(0xFF8A5A00);
  static const errorBg = Color(0xFFFDECEC);
  static const errorText = Color(0xFFC0272D);
  static const error = Color(0xFFE5484D);
  static const infoBg = Color(0xFFE8F2FC);
  static const infoText = Color(0xFF145EA8);
  static const blueBg = Color(0xFFE8F2FC);
  static const blue = Color(0xFF2E7CD6);
  static const violetBg = Color(0xFFEEEAFB);
  static const violet = Color(0xFF6D53D6);
  static const tealBg = Color(0xFFE6F6F4);
  static const teal = Color(0xFF0E9384);
}

/// typeScale of the web: use the style for its job, not ad-hoc sizes.
abstract final class AppText {
  static const pageTitle = TextStyle(fontSize: 22, height: 32 / 22, fontWeight: FontWeight.w700, color: AppColors.ink);
  static const sectionTitle = TextStyle(fontSize: 17, height: 26 / 17, fontWeight: FontWeight.w700, color: AppColors.ink);
  static const body = TextStyle(fontSize: 14.5, height: 23 / 14.5, fontWeight: FontWeight.w400, color: AppColors.ink);
  static const bodyStrong = TextStyle(fontSize: 14.5, height: 23 / 14.5, fontWeight: FontWeight.w600, color: AppColors.ink);
  static const small = TextStyle(fontSize: 13, height: 20 / 13, fontWeight: FontWeight.w400, color: AppColors.muted);
  static const label = TextStyle(fontSize: 12.5, height: 18 / 12.5, fontWeight: FontWeight.w600, color: AppColors.muted);
  static const caption = TextStyle(fontSize: 12, height: 18 / 12, fontWeight: FontWeight.w400, color: AppColors.muted);
  static const kpi = TextStyle(fontSize: 30, height: 38 / 30, fontWeight: FontWeight.w700, color: AppColors.ink);
}

/// The header bars and the top of the login page: hiphr.headerGradient of the web kept darker,
/// so the white title and status bar icons stay readable across a narrow phone.
const headerGradient = LinearGradient(
  begin: Alignment.topLeft,
  end: Alignment.bottomRight,
  colors: [AppColors.orange600, AppColors.orange500],
);

/// White card with rounded corners and the faint orange shadow of HIPHR.
const cardDecoration = BoxDecoration(
  color: Colors.white,
  borderRadius: BorderRadius.all(Radius.circular(18)),
  boxShadow: [
    BoxShadow(color: Color(0x14F06400), blurRadius: 28, offset: Offset(0, 8)),
    BoxShadow(color: Color(0x0D2B1A0E), blurRadius: 6, offset: Offset(0, 2)),
  ],
);

ThemeData buildTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: AppColors.orange600,
    primary: AppColors.orange600,
    onPrimary: Colors.white,
    surface: Colors.white,
    onSurface: AppColors.ink,
    error: AppColors.error,
  );
  const radius = BorderRadius.all(Radius.circular(12));
  const inputBorder = OutlineInputBorder(borderRadius: radius, borderSide: BorderSide(color: AppColors.line));

  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    fontFamily: 'Prompt',
    scaffoldBackgroundColor: AppColors.cream,
    textTheme: const TextTheme(
      titleLarge: AppText.pageTitle,
      titleMedium: AppText.sectionTitle,
      bodyLarge: AppText.body,
      bodyMedium: AppText.body,
      bodySmall: AppText.small,
      labelLarge: TextStyle(fontSize: 14, fontWeight: FontWeight.w600),
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: AppColors.orange600,
      foregroundColor: Colors.white,
      elevation: 0,
      centerTitle: false,
      titleTextStyle: TextStyle(fontFamily: 'Prompt', fontSize: 18, fontWeight: FontWeight.w700, color: Colors.white),
    ),
    inputDecorationTheme: const InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: inputBorder,
      enabledBorder: inputBorder,
      focusedBorder: OutlineInputBorder(borderRadius: radius, borderSide: BorderSide(color: AppColors.orange400, width: 1.6)),
      labelStyle: AppText.small,
      floatingLabelStyle: TextStyle(color: AppColors.orange600, fontWeight: FontWeight.w600),
      contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 12),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: AppColors.orange600,
        minimumSize: const Size(64, 44),
        shape: const RoundedRectangleBorder(borderRadius: radius),
        textStyle: const TextStyle(fontFamily: 'Prompt', fontSize: 14, fontWeight: FontWeight.w600),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: AppColors.orange600,
        minimumSize: const Size(64, 44),
        side: const BorderSide(color: AppColors.orange400),
        shape: const RoundedRectangleBorder(borderRadius: radius),
        textStyle: const TextStyle(fontFamily: 'Prompt', fontSize: 14, fontWeight: FontWeight.w600),
      ),
    ),
    textButtonTheme: TextButtonThemeData(style: TextButton.styleFrom(foregroundColor: AppColors.orange600)),
    chipTheme: const ChipThemeData(side: BorderSide.none, shape: StadiumBorder()),
    dividerTheme: const DividerThemeData(color: AppColors.lineSoft, space: 1),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: Colors.white,
      indicatorColor: AppColors.orange100,
      labelTextStyle: WidgetStateProperty.all(const TextStyle(fontFamily: 'Prompt', fontSize: 12, fontWeight: FontWeight.w500)),
    ),
    snackBarTheme: const SnackBarThemeData(behavior: SnackBarBehavior.floating),
  );
}
