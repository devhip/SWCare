import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api_client.dart';
import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../widgets/common.dart';
import '../tickets/ticket_list.dart';

/// เมนู: who is logged in, the pages of each role (no Admin pages in the app, Q44), ตั้งค่า, logout.
class MorePage extends ConsumerWidget {
  const MorePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final me = ref.watch(meProvider);
    final counts = ref.watch(countsProvider).value;

    Widget badge(String scope) {
      final n = counts?[scope] ?? 0;
      return n > 0 ? Pill('$n', bg: AppColors.orange600, fg: Colors.white, strong: true) : const Icon(Icons.chevron_right);
    }

    final roles = [
      if (me.isAdmin) texts.t('roles.admin'),
      if (me.isExecutive) texts.t('roles.executive'),
      if (me.isProgrammerLead) texts.t('roles.programmerLead') else if (me.isProgrammer) texts.t('roles.programmer'),
      if (me.isSupportHead) texts.t('roles.supportHead') else if (me.isDepartmentHead && !me.isProgrammerLead) texts.t('roles.departmentHead'),
      if (me.canViewAllTickets) texts.t('roles.viewAll'),
    ];

    return Scaffold(
      appBar: AppBar(flexibleSpace: const HeaderBackground(), title: Text(texts.t('menu'))),
      body: ListView(padding: const EdgeInsets.fromLTRB(12, 12, 12, 32), children: [
        AppCard(
          child: Row(children: [
            Container(
              width: 52,
              height: 52,
              alignment: Alignment.center,
              decoration: const BoxDecoration(shape: BoxShape.circle, gradient: headerGradient),
              child: Text(me.displayName.characters.first, style: AppText.sectionTitle.copyWith(color: Colors.white)),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(me.displayName, style: AppText.sectionTitle),
                Text('${me.username} · ${me.departmentName}', style: AppText.small),
                if (roles.isNotEmpty) ...[
                  const SizedBox(height: 6),
                  Wrap(spacing: 6, runSpacing: 4, children: [for (final r in roles) Pill(r, bg: AppColors.orange50, fg: AppColors.orange600)]),
                ],
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
          child: ListTile(
            leading: const Icon(Icons.settings_outlined),
            title: Text(texts.t('settings')),
            subtitle: Text(texts.t('settingsHint'), style: AppText.caption),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => context.push('/settings'),
          ),
        ),
        const SizedBox(height: 12),
        // Apart from the rest, so it is not tapped by mistake.
        AppCard(
          padding: EdgeInsets.zero,
          child: ListTile(
            leading: const Icon(Icons.logout, color: AppColors.errorText),
            title: Text(texts.t('common:logout'), style: AppText.body.copyWith(color: AppColors.errorText)),
            onTap: () => ref.read(sessionProvider.notifier).logout(),
          ),
        ),
        const SizedBox(height: 16),
        Center(child: Text('SWCare · ${ref.watch(serverConfigProvider)}', style: AppText.caption)),
      ]),
    );
  }
}
