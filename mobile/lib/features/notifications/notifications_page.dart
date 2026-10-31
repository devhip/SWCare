import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/format.dart';
import '../../core/i18n.dart';
import '../../core/theme.dart';
import '../../data/api.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';
import '../tickets/ticket_list.dart';

/// Unread count for the badge: every minute like the bell of the web, and after any ticket change.
class UnreadCount extends AsyncNotifier<int> {
  Timer? _timer;

  @override
  Future<int> build() {
    ref.watch(ticketListsVersionProvider);
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(minutes: 1), (_) => ref.invalidateSelf());
    ref.onDispose(() => _timer?.cancel());
    return ref.watch(apiProvider).unreadCount();
  }
}

final unreadCountProvider = AsyncNotifierProvider<UnreadCount, int>(UnreadCount.new);

final notificationsProvider = FutureProvider<List<AppNotification>>((ref) {
  ref.watch(ticketListsVersionProvider);
  return ref.watch(apiProvider).notifications();
});

/// The text of one notification, built like NotificationBell.tsx (type + Data in the reader's language).
String notificationText(AppNotification item, Texts texts) {
  Map<String, dynamic> data = const {};
  try {
    if (item.data != null) data = jsonDecode(item.data!) as Map<String, dynamic>;
  } on FormatException {
    // Old rows without data still show their type.
  }
  return texts.t('notification:${item.type}', {
    'actor': item.actorName ?? texts.t('notification:system'),
    'no': data['TicketNo'] ?? '',
    'assignee': data['Assignee'] ?? '',
    'due': formatDate((data['PlannedDueDate'] ?? data['NewDueDate']) as String?, texts.lang),
    'date': formatDate(data['CloseDate'] as String?, texts.lang),
    'count': data['Count'] ?? 0,
  });
}

class NotificationsPage extends ConsumerWidget {
  const NotificationsPage({super.key});

  Future<void> _open(BuildContext context, WidgetRef ref, AppNotification item) async {
    if (!item.isRead) {
      await ref.read(apiProvider).markRead(item.id).catchError((_) {});
      ref.invalidate(notificationsProvider);
      ref.invalidate(unreadCountProvider);
    }
    if (!context.mounted) return;
    if (item.ticketId != null) {
      context.push('/ticket/${item.ticketId}');
    } else if (item.type == 'PendingApprovalReminder') {
      context.push('/approvals');
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final list = ref.watch(notificationsProvider);
    return Scaffold(
      appBar: AppBar(flexibleSpace: const HeaderBackground(), title: Text(texts.t('notification:title')), actions: [
        TextButton(
          style: TextButton.styleFrom(foregroundColor: Colors.white),
          onPressed: () async {
            await guard(context, ref, () => ref.read(apiProvider).markAllRead());
            ref.invalidate(notificationsProvider);
            ref.invalidate(unreadCountProvider);
          },
          child: Text(texts.t('notification:readAll')),
        ),
      ]),
      body: list.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorView(e, onRetry: () => ref.invalidate(notificationsProvider)),
        data: (items) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(unreadCountProvider);
            return ref.refresh(notificationsProvider.future);
          },
          child: items.isEmpty
              ? ListView(children: [EmptyView(texts.t('notification:empty'), icon: Icons.notifications_none)])
              : ListView.separated(
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const Divider(),
                  itemBuilder: (context, i) {
                    final item = items[i];
                    return ListTile(
                      tileColor: item.isRead ? null : AppColors.orange50,
                      leading: Icon(item.isRead ? Icons.notifications_none : Icons.notifications_active, color: AppColors.orange600),
                      title: Text(notificationText(item, texts), style: item.isRead ? AppText.body : AppText.bodyStrong),
                      subtitle: Text(formatDateTime(item.createdAt, texts.lang), style: AppText.caption),
                      onTap: () => _open(context, ref, item),
                    );
                  },
                ),
        ),
      ),
    );
  }
}
