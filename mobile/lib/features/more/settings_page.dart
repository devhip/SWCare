import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api_client.dart';
import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../widgets/common.dart';

/// ตั้งค่า (from เมนู): language, password, the server the app talks to.
class SettingsPage extends ConsumerWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final lang = ref.watch(langProvider);

    return Scaffold(
      appBar: AppBar(flexibleSpace: const HeaderBackground(), title: Text(texts.t('settings'))),
      body: ListView(padding: const EdgeInsets.fromLTRB(12, 8, 12, 32), children: [
        SectionLabel(texts.t('language'), hint: texts.t('languageHint')),
        AppCard(
          padding: EdgeInsets.zero,
          child: RadioGroup<String>(
            groupValue: lang,
            onChanged: (v) {
              if (v != null) ref.read(sessionProvider.notifier).setLanguage(v);
            },
            // Each language in its own words, so it can be found whichever one is on.
            child: const Column(children: [
              RadioListTile(value: 'th', title: Text('ภาษาไทย'), controlAffinity: ListTileControlAffinity.trailing),
              Divider(indent: 16, endIndent: 16),
              RadioListTile(value: 'en', title: Text('English'), controlAffinity: ListTileControlAffinity.trailing),
            ]),
          ),
        ),
        SectionLabel(texts.t('account')),
        AppCard(
          padding: EdgeInsets.zero,
          child: ListTile(
            leading: const Icon(Icons.key_outlined),
            title: Text(texts.t('common:changePassword')),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => context.push('/change-password'),
          ),
        ),
        SectionLabel(texts.t('about')),
        AppCard(
          padding: EdgeInsets.zero,
          child: ListTile(
            leading: const Icon(Icons.dns_outlined),
            title: Text(texts.t('server.url')),
            subtitle: Text(ref.watch(serverConfigProvider), style: AppText.small),
          ),
        ),
      ]),
    );
  }
}

/// The small heading above a group of cards in the menu pages.
class SectionLabel extends StatelessWidget {
  const SectionLabel(this.text, {super.key, this.hint});

  final String text;
  final String? hint;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(8, 16, 8, 8),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(text, style: AppText.label),
          if (hint != null) Text(hint!, style: AppText.caption),
        ]),
      );
}
