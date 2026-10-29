import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/format.dart';
import '../../core/i18n.dart';
import '../../core/theme.dart';
import '../../data/api.dart';
import '../../widgets/common.dart';
import '../tickets/ticket_list.dart';

final dashboardProvider = FutureProvider<Map<String, dynamic>>((ref) {
  ref.watch(ticketListsVersionProvider);
  return ref.watch(apiProvider).dashboard();
});

/// Dashboard on the phone is a summary (plan 9): the numbers, open work by status and system, the team.
/// The last 6 months as the API's default; charts, filters and Excel are on the web.
class DashboardPage extends ConsumerWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final data = ref.watch(dashboardProvider);
    return Scaffold(
      appBar: AppBar(
        title: Text(texts.t('dashboard:title')),
        // An executive starts here (homeRoute of the web): the way to the tabs.
        leading: context.canPop()
            ? null
            : IconButton(tooltip: texts.t('navigation:myTasks'), icon: const Icon(Icons.home_outlined), onPressed: () => context.go('/my')),
      ),
      body: data.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(e, onRetry: () => ref.invalidate(dashboardProvider)),
        data: (d) => RefreshIndicator(
          onRefresh: () => ref.refresh(dashboardProvider.future),
          child: _Body(d: d, texts: texts),
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.d, required this.texts});

  final Map<String, dynamic> d;
  final Texts texts;

  String _days(Object? value) => value == null ? texts.t('dashboard:kpi.none') : texts.t('dashboard:kpi.days', {'value': (value as num).toStringAsFixed(1)});

  @override
  Widget build(BuildContext context) {
    final kpi = d['Kpi'] as Map<String, dynamic>;
    final onTime = kpi['OnTimePercent'] as num?;
    final range = '${formatDate(d['From'] as String?, texts.lang)} – ${formatDate(d['To'] as String?, texts.lang)}';
    final open = (d['OpenByStatus'] as List<dynamic>).cast<Map<String, dynamic>>();
    final products = (d['ByProduct'] as List<dynamic>).cast<Map<String, dynamic>>().take(5).toList();
    final team = (d['Team'] as List<dynamic>).cast<Map<String, dynamic>>();

    final tiles = [
      (texts.t('dashboard:kpi.open'), '${kpi['OpenNow']}', texts.t('dashboard:kpi.openSub', {'approval': kpi['WaitingApprovalNow'], 'triage': kpi['WaitingTriageNow']})),
      (texts.t('dashboard:kpi.overdue'), '${kpi['OverdueNow']}', texts.t('dashboard:kpi.overdueSub')),
      (texts.t('dashboard:kpi.new'), '${kpi['NewInRange']}', texts.t('dashboard:kpi.newSub')),
      (texts.t('dashboard:kpi.closed'), '${kpi['ClosedInRange']}', texts.t('dashboard:kpi.closedSub', {'declined': kpi['DeclinedInRange'], 'cancelled': kpi['CancelledInRange']})),
      (texts.t('dashboard:kpi.onTime'), onTime == null ? texts.t('dashboard:kpi.none') : '${onTime.round()}%', texts.t('dashboard:kpi.onTimeSub', {'count': kpi['OnTimeCount'], 'base': kpi['OnTimeBase']})),
      (texts.t('dashboard:kpi.avgClose'), _days(kpi['AvgDaysToClose']), texts.t('dashboard:kpi.avgCloseSub')),
    ];

    return ListView(padding: const EdgeInsets.fromLTRB(12, 12, 12, 32), children: [
      Text(texts.t('dashboardRange', {'range': range}), style: AppText.small),
      const SizedBox(height: 8),
      LayoutBuilder(
        builder: (context, box) => Wrap(spacing: 10, runSpacing: 10, children: [
          for (final (label, value, sub) in tiles)
            SizedBox(
              width: (box.maxWidth - 10) / 2,
              child: AppCard(
                padding: const EdgeInsets.all(12),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(label, style: AppText.label),
                  Text(value, style: AppText.kpi.copyWith(fontSize: 26)),
                  Text(sub, style: AppText.caption, maxLines: 2, overflow: TextOverflow.ellipsis),
                ]),
              ),
            ),
        ]),
      ),
      const SizedBox(height: 12),
      _Bars(title: texts.t('dashboard:openByStatus'), rows: [for (final r in open) (texts.t('ticket:status.${r['Status']}'), (r['Count'] as num).toInt())]),
      const SizedBox(height: 12),
      _Bars(title: texts.t('dashboard:byProduct'), hint: texts.t('dashboard:breakdownHint'), rows: [for (final r in products) (r['Label'] as String, (r['Count'] as num).toInt())]),
      if (team.isNotEmpty) ...[
        const SizedBox(height: 12),
        AppCard(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(texts.t('dashboard:team.title'), style: AppText.sectionTitle),
            Text(texts.t('dashboard:team.hint'), style: AppText.caption),
            const SizedBox(height: 8),
            for (final m in team)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(m['Name'] as String, style: AppText.bodyStrong),
                  Text(
                    [
                      '${texts.t('dashboard:team.open')} ${m['OpenNow']}',
                      '${texts.t('dashboard:team.overdue')} ${m['OverdueNow']}',
                      '${texts.t('dashboard:team.resolved')} ${m['ResolvedInRange']}',
                      '${texts.t('dashboard:team.reschedules')} ${m['Reschedules']}',
                    ].join(' · '),
                    style: AppText.small,
                  ),
                ]),
              ),
          ]),
        ),
      ],
      const SizedBox(height: 12),
      Text(texts.t('dashboardOnWeb'), style: AppText.caption, textAlign: TextAlign.center),
    ]);
  }
}

/// Counts as thin bars in the brand colour, the number beside each one (one series: no legend).
class _Bars extends StatelessWidget {
  const _Bars({required this.title, required this.rows, this.hint});

  final String title;
  final String? hint;
  final List<(String, int)> rows;

  @override
  Widget build(BuildContext context) {
    final max = rows.fold<int>(0, (m, r) => r.$2 > m ? r.$2 : m);
    return AppCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(title, style: AppText.sectionTitle),
        if (hint != null) Text(hint!, style: AppText.caption),
        const SizedBox(height: 8),
        for (final (label, count) in rows)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 4),
            child: Row(children: [
              SizedBox(width: 120, child: Text(label, style: AppText.small, overflow: TextOverflow.ellipsis)),
              Expanded(
                child: LayoutBuilder(
                  builder: (context, box) => Align(
                    alignment: Alignment.centerLeft,
                    child: Container(
                      height: 10,
                      width: max == 0 ? 0 : box.maxWidth * count / max,
                      decoration: BoxDecoration(color: AppColors.orange500, borderRadius: BorderRadius.circular(4)),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 8),
              SizedBox(width: 28, child: Text('$count', style: AppText.bodyStrong, textAlign: TextAlign.right)),
            ]),
          ),
      ]),
    );
  }
}
