import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/i18n.dart';
import '../notifications/notifications_page.dart';
import '../tickets/ticket_list.dart';

/// Bottom tabs: งานของฉัน · รายงานทั้งหมด · แจ้งเตือน · เมนู.
class HomeShell extends ConsumerWidget {
  const HomeShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final unread = ref.watch(unreadCountProvider).value ?? 0;
    final counts = ref.watch(countsProvider).value;
    final waiting = counts == null ? 0 : counts['ToDo'] + counts['ToApprove'] + counts['ToClose'] + counts['ToTriage'];

    Widget withBadge(Widget icon, int n) => n > 0 ? Badge(label: Text(n > 99 ? '99+' : '$n'), child: icon) : icon;

    return Scaffold(
      body: shell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: shell.currentIndex,
        onDestinationSelected: (i) => shell.goBranch(i, initialLocation: i == shell.currentIndex),
        destinations: [
          NavigationDestination(icon: withBadge(const Icon(Icons.assignment_ind_outlined), waiting), selectedIcon: const Icon(Icons.assignment_ind), label: texts.t('navigation:myTasks')),
          NavigationDestination(icon: const Icon(Icons.list_alt_outlined), selectedIcon: const Icon(Icons.list_alt), label: texts.t('navigation:allTickets')),
          NavigationDestination(icon: withBadge(const Icon(Icons.notifications_none), unread), selectedIcon: const Icon(Icons.notifications), label: texts.t('notification:title')),
          NavigationDestination(icon: const Icon(Icons.menu), label: texts.t('menu')),
        ],
      ),
    );
  }
}
