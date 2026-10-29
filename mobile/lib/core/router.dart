import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/change_password_page.dart';
import '../features/auth/login_page.dart';
import '../features/board/board_page.dart';
import '../features/dashboard/dashboard_page.dart';
import '../features/more/more_page.dart';
import '../features/notifications/notifications_page.dart';
import '../features/shell/home_shell.dart';
import '../features/tickets/all_tickets_page.dart';
import '../features/tickets/my_tasks_page.dart';
import '../features/tickets/scope_page.dart';
import '../features/tickets/ticket_detail_page.dart';
import '../features/tickets/ticket_form_page.dart';
import '../models/models.dart';
import 'session.dart';

/// Pages a role may open, like the RoleGuard of the web (the API checks again anyway).
bool _allowed(String path, Me me) => switch (path) {
      '/approvals' => me.isDepartmentHead,
      '/triage' => me.isProgrammerLead,
      '/board' => me.isProgrammer,
      '/dashboard' => me.seesDashboard,
      _ => true,
    };

final routerProvider = Provider<GoRouter>((ref) {
  // Re-run the redirect when the session changes, without rebuilding the router.
  final session = ValueNotifier<AsyncValue<Me?>>(ref.read(sessionProvider));
  ref.listen(sessionProvider, (_, next) => session.value = next);
  ref.onDispose(session.dispose);

  return GoRouter(
    initialLocation: '/my',
    refreshListenable: session,
    redirect: (context, state) {
      final value = session.value;
      if (value.isLoading && !value.hasValue) return state.matchedLocation == '/splash' ? null : '/splash';
      final me = value.value;
      final at = state.matchedLocation;
      if (me == null) return at == '/login' ? null : '/login';
      if (me.mustChangePassword) return at == '/change-password' ? null : '/change-password';
      if (at == '/login' || at == '/splash') return me.isExecutive && !me.isAdmin && !me.isDepartmentHead && !me.isProgrammer ? '/dashboard' : '/my';
      if (!_allowed(at, me)) return '/my';
      return null;
    },
    routes: [
      GoRoute(path: '/splash', builder: (_, _) => const Scaffold(body: Center(child: CircularProgressIndicator()))),
      GoRoute(path: '/login', builder: (_, _) => const LoginPage()),
      GoRoute(path: '/change-password', builder: (_, _) => const ChangePasswordPage()),
      StatefulShellRoute.indexedStack(
        builder: (_, _, shell) => HomeShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [GoRoute(path: '/my', builder: (_, _) => const MyTasksPage())]),
          StatefulShellBranch(routes: [GoRoute(path: '/tickets', builder: (_, _) => const AllTicketsPage())]),
          StatefulShellBranch(routes: [GoRoute(path: '/notifications', builder: (_, _) => const NotificationsPage())]),
          StatefulShellBranch(routes: [GoRoute(path: '/more', builder: (_, _) => const MorePage())]),
        ],
      ),
      GoRoute(path: '/ticket/new', builder: (_, _) => const TicketFormPage()),
      GoRoute(
        path: '/ticket/:id',
        builder: (_, state) => TicketDetailPage(id: int.parse(state.pathParameters['id']!)),
        routes: [
          GoRoute(path: 'edit', builder: (_, state) => TicketFormPage(editId: int.parse(state.pathParameters['id']!))),
        ],
      ),
      GoRoute(
        path: '/approvals',
        builder: (_, _) => const ScopePage(scope: 'ToApprove', titleKey: 'navigation:approvals', hintKey: 'ticket:page.approvalsHint'),
      ),
      GoRoute(
        path: '/triage',
        builder: (_, _) => const ScopePage(scope: 'ToTriage', titleKey: 'navigation:triage', hintKey: 'ticket:page.triageHint'),
      ),
      GoRoute(path: '/board', builder: (_, _) => const BoardPage()),
      GoRoute(path: '/dashboard', builder: (_, _) => const DashboardPage()),
    ],
  );
});
