import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/format.dart';
import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../data/api.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';
import '../tickets/ticket_list.dart';

final boardProvider = FutureProvider<Board>((ref) {
  ref.watch(ticketListsVersionProvider);
  return ref.watch(apiProvider).board();
});

const _columns = ['PendingTriage', 'Queued', 'InProgress', 'WaitingInfo', 'Resolved'];

/// Kanban on the phone: lists by status, no dragging (plan 9, Q44). Drags and queue order are on the web.
class BoardPage extends ConsumerStatefulWidget {
  const BoardPage({super.key});

  @override
  ConsumerState<BoardPage> createState() => _BoardPageState();
}

class _BoardPageState extends ConsumerState<BoardPage> {
  /// Lane: a programmer's id, 0 = not triaged yet, null = everyone.
  int? _lane;
  bool _chosen = false;

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final me = ref.watch(meProvider);
    final board = ref.watch(boardProvider);
    // A programmer starts on their own cards; the lead on everyone's.
    final lane = _chosen ? _lane : (me.isProgrammerLead ? null : me.userId);

    return Scaffold(
      appBar: AppBar(flexibleSpace: const HeaderBackground(), title: Text(texts.t('navigation:kanban'))),
      body: board.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(e, onRetry: () => ref.invalidate(boardProvider)),
        data: (b) {
          final cards = b.cards.where((c) => lane == null || (lane == 0 ? c.assigneeId == null : c.assigneeId == lane)).toList()
            ..sort((a, c) => a.queueOrder.compareTo(c.queueOrder));
          String nameOf(int? id) => b.programmers.where((p) => p.id == id).firstOrNull?.displayName ?? texts.t('ticket:kanban.unassigned');
          return RefreshIndicator(
            onRefresh: () => ref.refresh(boardProvider.future),
            child: ListView(padding: const EdgeInsets.fromLTRB(12, 8, 12, 32), children: [
              Text(texts.t('boardHint'), style: AppText.small),
              const SizedBox(height: 8),
              SizedBox(
                height: 40,
                child: ListView(scrollDirection: Axis.horizontal, children: [
                  for (final (value, label) in [
                    (null, texts.t('everyone')),
                    (0, texts.t('ticket:kanban.unassigned')),
                    for (final p in b.programmers) (p.id, p.displayName),
                  ])
                    Padding(
                      padding: const EdgeInsets.only(right: 6),
                      child: ChoiceChip(
                        label: Text(label),
                        selected: lane == value,
                        showCheckmark: false,
                        onSelected: (_) => setState(() {
                          _chosen = true;
                          _lane = value;
                        }),
                      ),
                    ),
                ]),
              ),
              for (final status in _columns) ...[
                const SizedBox(height: 14),
                Row(children: [
                  StatusChip(status),
                  const SizedBox(width: 8),
                  Text('${cards.where((c) => c.status == status).length}', style: AppText.label),
                ]),
                const SizedBox(height: 8),
                for (final c in cards.where((c) => c.status == status))
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: AppCard(
                      padding: const EdgeInsets.fromLTRB(14, 10, 14, 10),
                      onTap: () => context.push('/ticket/${c.id}'),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(c.ticketNo, style: AppText.label),
                        Text(c.title, style: AppText.bodyStrong, maxLines: 2, overflow: TextOverflow.ellipsis),
                        Text([c.productName, if (c.customerName?.isNotEmpty ?? false) c.customerName!].join(' · '), style: AppText.small),
                        const SizedBox(height: 6),
                        Wrap(spacing: 8, runSpacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
                          PriorityChip(c.actualPriority ?? c.requestedPriority),
                          if (lane == null) Text(nameOf(c.assigneeId), style: AppText.caption),
                          if (c.plannedDueDate != null)
                            Text(
                              formatDate(c.plannedDueDate, texts.lang),
                              style: AppText.caption.copyWith(color: c.isOverdue ? AppColors.errorText : null, fontWeight: c.isOverdue ? FontWeight.w600 : null),
                            ),
                          if (c.isOverdue) Pill(texts.t('ticket:list.overdue'), bg: AppColors.errorBg, fg: AppColors.errorText),
                        ]),
                      ]),
                    ),
                  ),
                if (!cards.any((c) => c.status == status)) Text(texts.t('ticket:list.empty'), style: AppText.caption),
              ],
            ]),
          );
        },
      ),
    );
  }
}
