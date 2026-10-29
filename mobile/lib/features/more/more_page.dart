import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api_client.dart';
import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../widgets/common.dart';
import '../tickets/ticket_list.dart';

/// เมนู: the pages of each role (no Admin pages in the app, Q44), language, password, logout.
class MorePage extends ConsumerWidget {
  const MorePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final me = ref.watch(meProvider);
    final counts = ref.watch(countsProvider).value;
    final lang = ref.watch(langProvider);

    Widget badge(String scope) {
      final n = counts?[scope] ?? 0;
      return n > 0 ? Pill('$n', bg: AppColors.orange600, fg: Colors.white, strong: true) : const Icon(Icons.chevron_right);
    }

    final roles = [
      if (me.isAdmin) texts.t('roles.admin'),
      if (me.isExecutive) texts.t('roles.executive'),
      if (me.isProgrammerLead) texts.t('roles.programmerLead') else if (me.isProgrammer) texts.t('roles.programmer'),
      if (me.isSupportHead) texts.t('roles.supportHead') else if (me.isDepartmentHead) texts.t('roles.departmentHead'),
      if (me.canViewAllTickets) texts.t('roles.viewAll'),
    ];

    return Scaffold(
      appBar: AppBar(title: Text(texts.t('menu'))),
      body: ListView(padding: const EdgeInsets.all(12), children: [
        AppCard(
          child: Row(children: [
            CircleAvatar(
              radius: 24,
              backgroundColor: AppColors.orange100,
              child: Text(me.displayName.characters.first, style: AppText.sectionTitle.copyWith(color: AppColors.orange600)),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(me.displayName, style: AppText.bodyStrong),
                Text('${me.username} · ${me.departmentName}', style: AppText.small),
                if (roles.isNotEmpty) Text(roles.join(' · '), style: AppText.caption),
              ]),
            ),
          ]),
        ),
        const SizedBox(height: 12),
        AppCard(
          padding: EdgeInsets.zero,
          child: Column(children: [
            if (me.seesDashboard)
              ListTile(leading: const Icon(Icons.insights_outlined), title: Text(texts.t('navigation:dashboard')), trailing: const Icon(Icons.chevron_right), onTap: () => context.push('/dashboard')),
            if (me.isDepartmentHead)
              ListTile(leading: const Icon(Icons.fact_check_outlined), title: Text(texts.t('navigation:approvals')), trailing: badge('ToApprove'), onTap: () => context.push('/approvals')),
            if (me.isProgrammerLead)
              ListTile(leading: const Icon(Icons.playlist_add_check), title: Text(texts.t('navigation:triage')), trailing: badge('ToTriage'), onTap: () => context.push('/triage')),
            if (me.isProgrammer)
              ListTile(leading: const Icon(Icons.view_kanban_outlined), title: Text(texts.t('navigation:kanban')), trailing: const Icon(Icons.chevron_right), onTap: () => context.push('/board')),
            ListTile(leading: const Icon(Icons.add_circle_outline), title: Text(texts.t('navigation:newTicket')), trailing: const Icon(Icons.chevron_right), onTap: () => context.push('/ticket/new')),
          ]),
        ),
        const SizedBox(height: 12),
        AppCard(
          padding: EdgeInsets.zero,
          child: Column(children: [
            ListTile(
              leading: const Icon(Icons.translate),
              title: Text(texts.t('language')),
              trailing: SegmentedButton<String>(
                segments: const [ButtonSegment(value: 'th', label: Text('TH')), ButtonSegment(value: 'en', label: Text('EN'))],
                selected: {lang},
                showSelectedIcon: false,
                onSelectionChanged: (v) => ref.read(sessionProvider.notifier).setLanguage(v.first),
              ),
            ),
            ListTile(leading: const Icon(Icons.key_outlined), title: Text(texts.t('common:changePassword')), trailing: const Icon(Icons.chevron_right), onTap: () => context.push('/change-password')),
            ListTile(
              leading: const Icon(Icons.logout, color: AppColors.errorText),
              title: Text(texts.t('common:logout'), style: AppText.body.copyWith(color: AppColors.errorText)),
              onTap: () => ref.read(sessionProvider.notifier).logout(),
            ),
          ]),
        ),
        const SizedBox(height: 12),
        Center(child: Text('SWCare · ${ref.watch(serverConfigProvider)}', style: AppText.caption)),
      ]),
    );
  }
}
