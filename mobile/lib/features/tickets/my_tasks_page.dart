import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';
import 'ticket_list.dart';

/// งานของฉัน: same tabs as app/MyTasks of the web, opening on the first one with work waiting.
class MyTasksPage extends ConsumerStatefulWidget {
  const MyTasksPage({super.key});

  @override
  ConsumerState<MyTasksPage> createState() => _MyTasksPageState();
}

class _MyTasksPageState extends ConsumerState<MyTasksPage> {
  String? _chosen;

  List<String> _tabs(Me me) => [
        if (me.isProgrammer) 'ToDo',
        if (me.isDepartmentHead) 'ToApprove',
        'ToClose',
        'Reported',
        'Following',
      ];

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final me = ref.watch(meProvider);
    final counts = ref.watch(countsProvider);
    final tabs = _tabs(me);
    final firstWithWork = counts.value == null ? null : tabs.firstWhere((s) => counts.value![s] > 0, orElse: () => 'Reported');
    final scope = _chosen ?? firstWithWork;

    return Scaffold(
      appBar: AppBar(flexibleSpace: const HeaderBackground(), title: Text(texts.t('navigation:myTasks'))),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/ticket/new'),
        backgroundColor: AppColors.orange600,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add),
        label: Text(texts.t('navigation:newTicket')),
      ),
      body: scope == null
          ? (counts.hasError ? ErrorView(counts.error!, onRetry: () => ref.invalidate(countsProvider)) : const Center(child: CircularProgressIndicator()))
          : Column(children: [
              SizedBox(
                height: 52,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  padding: const EdgeInsets.fromLTRB(12, 10, 12, 4),
                  children: [
                    for (final tab in tabs)
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          selected: tab == scope,
                          onSelected: (_) => setState(() => _chosen = tab),
                          selectedColor: AppColors.orange100,
                          backgroundColor: Colors.white,
                          showCheckmark: false,
                          label: Row(mainAxisSize: MainAxisSize.min, children: [
                            Text(texts.t('ticket:tab.$tab')),
                            if ((counts.value?[tab] ?? 0) > 0) ...[
                              const SizedBox(width: 6),
                              Pill('${counts.value![tab]}', bg: AppColors.orange600, fg: Colors.white, strong: true),
                            ],
                          ]),
                        ),
                      ),
                  ],
                ),
              ),
              Expanded(
                child: TicketListView(
                  key: ValueKey(scope),
                  scope: scope,
                  // ที่ฉันแจ้ง and ที่ร้องขอ/ติดตาม show open work; the other lists are open by definition.
                  statuses: scope == 'Reported' || scope == 'Following' ? openStatuses : const [],
                ),
              ),
            ]),
    );
  }
}
