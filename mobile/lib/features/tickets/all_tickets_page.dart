import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';
import 'ticket_list.dart';

/// รายงานทั้งหมด: every ticket the user may see, with search and a status filter (open by default, like the web).
class AllTicketsPage extends ConsumerStatefulWidget {
  const AllTicketsPage({super.key});

  @override
  ConsumerState<AllTicketsPage> createState() => _AllTicketsPageState();
}

class _AllTicketsPageState extends ConsumerState<AllTicketsPage> {
  final _search = TextEditingController();
  Timer? _debounce;
  String _query = '';
  String _status = 'open';

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    super.dispose();
  }

  void _onSearch(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), () => setState(() => _query = value.trim()));
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final statuses = switch (_status) {
      'all' => const <String>[],
      'open' => openStatuses,
      _ => [_status],
    };
    return Scaffold(
      appBar: AppBar(flexibleSpace: const HeaderBackground(), title: Text(texts.t('navigation:allTickets'))),
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 0),
          child: TextField(
            controller: _search,
            onChanged: _onSearch,
            textInputAction: TextInputAction.search,
            decoration: InputDecoration(prefixIcon: const Icon(Icons.search), hintText: texts.t('ticket:list.search')),
          ),
        ),
        SizedBox(
          height: 50,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.fromLTRB(12, 8, 12, 4),
            children: [
              for (final value in ['open', 'all', ...allStatuses])
                Padding(
                  padding: const EdgeInsets.only(right: 6),
                  child: ChoiceChip(
                    selected: _status == value,
                    showCheckmark: false,
                    onSelected: (_) => setState(() => _status = value),
                    label: Text(switch (value) {
                      'open' => texts.t('openOnly'),
                      'all' => texts.t('ticket:list.allStatuses'),
                      _ => texts.t('ticket:status.$value'),
                    }),
                  ),
                ),
            ],
          ),
        ),
        Expanded(child: TicketListView(key: ValueKey('$_status|$_query'), statuses: statuses, search: _query)),
      ]),
    );
  }
}
