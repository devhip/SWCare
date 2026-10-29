import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';

/// The card of the login pages, like AuthCard.tsx: brand, title, language switch.
class AuthCard extends ConsumerWidget {
  const AuthCard({super.key, required this.title, required this.subtitle, required this.child});

  final String title;
  final String subtitle;
  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lang = ref.watch(langProvider);
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                Align(
                  alignment: Alignment.centerRight,
                  child: SegmentedButton<String>(
                    segments: const [ButtonSegment(value: 'th', label: Text('TH')), ButtonSegment(value: 'en', label: Text('EN'))],
                    selected: {lang},
                    showSelectedIcon: false,
                    onSelectionChanged: (v) => ref.read(sessionProvider.notifier).setLanguage(v.first),
                  ),
                ),
                const SizedBox(height: 16),
                Container(
                  decoration: cardDecoration,
                  padding: const EdgeInsets.all(20),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    Text('SWCare', style: AppText.sectionTitle.copyWith(color: AppColors.orange600)),
                    Text(title, style: AppText.pageTitle),
                    const SizedBox(height: 2),
                    Text(subtitle, style: AppText.small),
                    const SizedBox(height: 16),
                    child,
                  ]),
                ),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}
