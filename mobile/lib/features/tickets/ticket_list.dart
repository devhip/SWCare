import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/i18n.dart';
import '../../data/api.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';

/// Bumped after any change to a ticket so every list and counter reloads (TICKET_LISTS_KEY of the web).
class TicketListsVersion extends Notifier<int> {
  @override
  int build() => 0;

  void bump() => state++;
}

final ticketListsVersionProvider = NotifierProvider<TicketListsVersion, int>(TicketListsVersion.new);

final countsProvider = FutureProvider<TicketCounts>((ref) {
  ref.watch(ticketListsVersionProvider);
  return ref.watch(apiProvider).counts();
});

/// Tickets of one scope, 20 at a time, loading more at the bottom; pull to refresh.
class TicketListView extends ConsumerStatefulWidget {
  const TicketListView({super.key, this.scope = 'All', this.statuses = const [], this.search, this.emptyText});

  final String scope;
  final List<String> statuses;
  final String? search;
  final String? emptyText;

  @override
  ConsumerState<TicketListView> createState() => _TicketListViewState();
}

class _TicketListViewState extends ConsumerState<TicketListView> {
  final _items = <TicketListItem>[];
  int _total = 0;
  int _page = 0;
  bool _loading = false;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  @override
  void didUpdateWidget(TicketListView old) {
    super.didUpdateWidget(old);
    if (old.scope != widget.scope || old.search != widget.search || old.statuses.join() != widget.statuses.join()) _reload();
  }

  Future<void> _reload() async {
    _page = 0;
    _items.clear();
    _total = 0;
    await _more();
  }

  Future<void> _more() async {
    if (_loading) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final (items, total) = await ref.read(apiProvider).tickets(
            scope: widget.scope,
            statuses: widget.statuses,
            search: widget.search,
            page: _page + 1,
          );
      if (!mounted) return;
      setState(() {
        _page++;
        _items.addAll(items);
        _total = total;
      });
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    ref.listen(ticketListsVersionProvider, (_, _) => _reload());
    final texts = ref.watch(textsProvider);

    if (_items.isEmpty && _error != null) return ErrorView(_error!, onRetry: _reload);
    if (_items.isEmpty && _loading) return const Center(child: CircularProgressIndicator());

    return RefreshIndicator(
      onRefresh: _reload,
      child: _items.isEmpty
          ? ListView(children: [EmptyView(widget.emptyText ?? texts.t('ticket:list.empty'))])
          : NotificationListener<ScrollNotification>(
              onNotification: (n) {
                if (n.metrics.extentAfter < 400 && _items.length < _total && !_loading && _error == null) _more();
                return false;
              },
              child: ListView.separated(
                padding: const EdgeInsets.fromLTRB(12, 12, 12, 96),
                itemCount: _items.length + 1,
                separatorBuilder: (_, _) => const SizedBox(height: 10),
                itemBuilder: (context, i) {
                  if (i == _items.length) {
                    return Padding(
                      padding: const EdgeInsets.all(8),
                      child: Center(
                        child: _loading
                            ? const CircularProgressIndicator()
                            : Text(texts.t('ticket:list.total', {'count': _total}), style: Theme.of(context).textTheme.bodySmall),
                      ),
                    );
                  }
                  final item = _items[i];
                  return TicketCard(item, onTap: () => context.push('/ticket/${item.id}'));
                },
              ),
            ),
    );
  }
}
