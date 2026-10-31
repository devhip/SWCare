import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../widgets/common.dart';

/// The login pages: the header of the web as a hero (brand, language) and the card over its lower edge.
class AuthCard extends ConsumerWidget {
  const AuthCard({super.key, required this.title, required this.subtitle, required this.child});

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final top = MediaQuery.paddingOf(context).top;
    const overlap = 40.0;

    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.light,
      child: Scaffold(
        body: SingleChildScrollView(
          child: Column(children: [
            SizedBox(
              height: top + 230,
              child: Stack(fit: StackFit.expand, children: [
                const HeaderBackground(circle: 340),
                Padding(
                  padding: EdgeInsets.fromLTRB(24, top + 12, 16, overlap + 20),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    const Align(alignment: Alignment.centerRight, child: _LanguageToggle()),
                    const Spacer(),
                    Row(children: [
                      const BrandMark(size: 52, onHeader: true),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text(texts.t('common:appName'), style: AppText.pageTitle.copyWith(color: Colors.white)),
                          Text(texts.t('common:appTagline'), style: AppText.small.copyWith(color: const Color(0xE6FFFFFF))),
                        ]),
                      ),
                    ]),
                  ]),
                ),
              ]),
            ),
            Transform.translate(
              offset: const Offset(0, -overlap),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 440),
                  child: Container(
                    decoration: cardDecoration,
                    padding: const EdgeInsets.fromLTRB(20, 24, 20, 16),
                    child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                      Text(title, style: AppText.pageTitle),
                      const SizedBox(height: 2),
                      Text(subtitle, style: AppText.small),
                      const SizedBox(height: 20),
                      child,
                    ]),
                  ),
                ),
              ),
            ),
          ]),
        ),
      ),
    );
  }
}

/// TH | EN on the header, before login (after login it is in ตั้งค่า).
class _LanguageToggle extends ConsumerWidget {
  const _LanguageToggle();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lang = ref.watch(langProvider);
    return Container(
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(color: const Color(0x2EFFFFFF), borderRadius: BorderRadius.circular(99)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        for (final (value, label) in const [('th', 'TH'), ('en', 'EN')])
          Semantics(
            selected: lang == value,
            button: true,
            child: Material(
              color: lang == value ? Colors.white : Colors.transparent,
              shape: const StadiumBorder(),
              clipBehavior: Clip.antiAlias,
              child: InkWell(
                onTap: lang == value ? null : () => ref.read(sessionProvider.notifier).setLanguage(value),
                child: Container(
                  constraints: const BoxConstraints(minWidth: 52, minHeight: 36),
                  alignment: Alignment.center,
                  child: Text(label, style: AppText.label.copyWith(color: lang == value ? AppColors.orange600 : Colors.white)),
                ),
              ),
            ),
          ),
      ]),
    );
  }
}
