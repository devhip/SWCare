// Every role through the real app on a device: log in on the login page, check the menu, the tabs and the pages
// each role may open, the buttons of a ticket parked at each stage, then press the buttons of every flow.
//
// Needs a fresh, empty database (the test creates its own users and tickets) — see README "ทดสอบทุกบทบาท".
import 'dart:convert';
import 'dart:math';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:swcare/core/api_client.dart';
import 'package:swcare/core/format.dart';
import 'package:swcare/core/i18n.dart';
import 'package:swcare/core/router.dart';
import 'package:swcare/features/board/board_page.dart';
import 'package:swcare/features/dashboard/dashboard_page.dart';
import 'package:swcare/features/tickets/my_tasks_page.dart';
import 'package:swcare/features/tickets/scope_page.dart';
import 'package:swcare/features/tickets/ticket_actions.dart';
import 'package:swcare/features/tickets/ticket_detail_page.dart';
import 'package:swcare/features/tickets/ticket_form_page.dart';
import 'package:swcare/main.dart' as app;
import 'package:swcare/widgets/common.dart';
import 'package:swcare/widgets/file_picker_bar.dart';
import 'package:swcare/widgets/user_search_dialog.dart';

const apiUrl = String.fromEnvironment('API_URL');
const adminSeedPassword = String.fromEnvironment('ADMIN_PW');
const password = String.fromEnvironment('TEST_PW');

/// Role key -> (username, display name, department, flags).
const people = {
  'supHead': ('sup.head', 'หัวหน้า Support', 'Customer Support', <String, bool>{}),
  'sup1': ('sup1', 'สุชาติ Support', 'Customer Support', <String, bool>{}),
  'head': ('sale.head', 'หัวหน้า เซลล์', 'Sale', <String, bool>{}),
  'rep': ('sale1', 'สมชาย เซลล์', 'Sale', <String, bool>{}),
  'req': ('sale2', 'สมหญิง เซลล์', 'Sale', <String, bool>{}),
  'viewAll': ('sale3', 'สมศักดิ์ ดูทั้งหมด', 'Sale', {'CanViewAllTickets': true}),
  'otherHead': ('prod.head', 'หัวหน้า Product', 'Product & Innovation', <String, bool>{}),
  'outsider': ('prod1', 'คนนอก Product', 'Product & Innovation', <String, bool>{}),
  'watch': ('watch1', 'ผู้ติดตาม Product', 'Product & Innovation', <String, bool>{}),
  'exec': ('exec1', 'ผู้บริหาร', 'Product & Innovation', {'IsExecutive': true}),
  'lead': ('lead', 'หัวหน้า โปรแกรมเมอร์', 'Software', <String, bool>{}),
  'dev1': ('dev1', 'พี่ป้อม', 'Software', <String, bool>{}),
  'dev2': ('dev2', 'พี่เกม', 'Software', <String, bool>{}),
};

const statusActions = ['Approve', 'Reject', 'Resubmit', 'Triage', 'Start', 'Resolve', 'Close', 'SendBack', 'Pause', 'RequestInfo', 'Reschedule', 'Reassign', 'Decline', 'Cancel'];

/// What each role may do at each stage (the table of verify-roles / the web test, written from the plan).
/// null = the ticket is not visible to that role.
Map<String, List<String>?> expected(String stage, String? assignee) {
  final e = <String, List<String>?>{
    'rep': ['Comment', 'ManageWatchers'], 'head': ['Comment', 'Follow', 'ManageWatchers'], 'req': ['Comment'], 'watch': ['Comment', 'Unfollow'],
    'supHead': ['Comment', 'Follow'], 'lead': ['Comment', 'Follow', 'ManageWatchers'], 'dev1': ['Comment', 'Follow'], 'dev2': ['Comment', 'Follow'],
    'admin': ['Comment', 'Follow'], 'exec': ['Follow'], 'viewAll': ['Follow'], 'otherHead': null, 'outsider': null,
  };
  void add(String? k, List<String> a) => e[k]!.addAll(a);
  switch (stage) {
    case 'PendingApproval': add('rep', ['Edit', 'AddAttachment', 'Cancel']); add('head', ['Approve', 'Reject']);
    case 'Rejected': add('rep', ['Edit', 'AddAttachment', 'Resubmit', 'Cancel']);
    case 'PendingTriage': add('lead', ['Triage', 'RequestInfo', 'Decline']); add('rep', ['Cancel']);
    case 'WaitingInfo/untriaged': add('lead', ['Decline']); add('rep', ['Cancel']);
    case 'Queued/notStarted': add('lead', ['Start', 'Reschedule', 'Reassign', 'Decline']); add(assignee, ['Start', 'Reschedule']); add('rep', ['Cancel']);
    case 'Queued/started': add('lead', ['Start', 'Reschedule', 'Reassign', 'Decline']); add(assignee, ['Start', 'Reschedule']);
    case 'InProgress': add('lead', ['Pause', 'Reschedule', 'Reassign', 'Decline']); add(assignee, ['Pause', 'Reschedule', 'Resolve', 'RequestInfo']);
    case 'WaitingInfo/started': add('lead', ['Decline', 'Reschedule', 'Reassign']); add(assignee, ['Reschedule']);
    case 'Resolved': add('rep', ['Close', 'SendBack']); add('head', ['Close', 'SendBack']);
  }
  return e;
}

const dashboardRoles = {'exec', 'admin', 'supHead', 'lead', 'head', 'otherHead'};
const headRoles = {'supHead', 'lead', 'head', 'otherHead'};
const programmerRoles = {'lead', 'dev1', 'dev2', 'admin'};

/// Direct API calls: the fixtures before the test, the true state of a ticket after each button.
class Server {
  final dio = Dio(BaseOptions(baseUrl: apiUrl, validateStatus: (_) => true));
  final tokens = <String, String>{};
  final ids = <String, int>{};
  final fixtures = <String, ({int id, String stage, String? assignee})>{};
  late String productName;

  Future<dynamic> call(String method, String path, {Object? body, String? as}) async {
    final r = await dio.request<dynamic>(path,
        data: body, options: Options(method: method, headers: {if (as != null) 'Authorization': 'Bearer ${tokens[as]}'}));
    if (r.statusCode != 200) throw StateError('$method $path: ${r.statusCode} ${r.data}');
    return (r.data as Map<String, dynamic>)['Result'];
  }

  Future<Map<String, dynamic>> ticket(int id) async => (await call('GET', '/api/Tickets/$id', as: 'admin')) as Map<String, dynamic>;

  Future<void> setUp() async {
    final login = await call('POST', '/api/Auth/Login', body: {'Username': 'admin', 'Password': adminSeedPassword});
    tokens['admin'] = login['AccessToken'] as String;
    final changed = await call('POST', '/api/Auth/ChangePassword', body: {'CurrentPassword': adminSeedPassword, 'NewPassword': password}, as: 'admin');
    tokens['admin'] = changed['AccessToken'] as String;
    ids['admin'] = (await call('GET', '/api/Auth/Me', as: 'admin'))['UserId'] as int;
    final depts = {for (final d in await call('GET', '/api/Departments', as: 'admin') as List) d['Name'] as String: d as Map<String, dynamic>};
    final temp = 'Temp-$password';
    for (final MapEntry(key: key, value: (username, display, dept, flags)) in people.entries) {
      final u = await call('POST', '/api/Users', as: 'admin', body: {
        'Username': username, 'DisplayName': display, 'DepartmentId': depts[dept]!['Id'],
        'IsAdmin': false, 'IsExecutive': false, 'CanViewAllTickets': false, ...flags, 'Password': temp,
      });
      final l = await call('POST', '/api/Auth/Login', body: {'Username': username, 'Password': temp});
      tokens[key] = l['AccessToken'] as String;
      final c = await call('POST', '/api/Auth/ChangePassword', body: {'CurrentPassword': temp, 'NewPassword': password}, as: key);
      tokens[key] = c['AccessToken'] as String;
      ids[key] = u['Id'] as int;
    }
    for (final (dept, key) in [('Customer Support', 'supHead'), ('Sale', 'head'), ('Product & Innovation', 'otherHead'), ('Software', 'lead')]) {
      await call('PUT', '/api/Departments/${depts[dept]!['Id']}', as: 'admin', body: {'Name': dept, 'HeadUserId': ids[key], 'IsActive': true});
    }
    final products = await call('GET', '/api/Products', as: 'admin') as List;
    final models = await call('GET', '/api/DeviceModels', as: 'admin') as List;
    final p = products.first as Map<String, dynamic>;
    productName = p['Name'] as String;
    await call('PUT', '/api/Products/${p['Id']}', as: 'admin', body: {'Name': p['Name'], 'OwnerUserId': ids['dev1'], 'SortOrder': p['SortOrder'], 'IsActive': true});

    final today = thaiToday();
    Future<void> act(String key, int id, String action, [Map<String, dynamic> body = const {}]) => call('POST', '/api/Tickets/$id/$action', as: key, body: body);
    Future<void> triage(int id) => act('lead', id, 'Triage', {
          'ActualPriority': 'Urgent', 'AssigneeId': ids['dev1'], 'PlannedStartDate': today, 'PlannedDueDate': addDays(today, 3),
          'ProductId': p['Id'], 'Type': 'Bug', 'DeviceModelIds': [models.first['Id']], 'CustomerName': 'ทรูทองหล่อ',
        });
    Future<void> make(String name, String stage, String? assignee, Future<void> Function(int id) steps) async {
      final form = FormData.fromMap({
        'Ticket': jsonEncode({
          'Title': '[$name] ทดสอบปุ่ม', 'Type': 'Bug', 'ProductId': p['Id'], 'Version': '4.0.2', 'CustomerName': 'ทรูทองหล่อ',
          'Description': 'ลบผู้ใช้ไม่ได้ ขึ้น Error', 'TestedDetails': null, 'RequestedPriority': 'Urgent', 'RequesterId': ids['req'],
          'DeviceModelIds': [models.first['Id']],
        }),
        'Files': MultipartFile.fromBytes(utf8.encode('log line'), filename: 'log.txt'),
      });
      final t = await call('POST', '/api/Tickets', as: 'rep', body: form);
      final id = t['Id'] as int;
      await call('POST', '/api/Tickets/$id/Watchers', as: 'rep', body: {'UserId': ids['watch']});
      await steps(id);
      fixtures[name] = (id: id, stage: stage, assignee: assignee);
    }

    await make('PendingApproval', 'PendingApproval', null, (id) async {});
    await make('Rejected', 'Rejected', null, (id) => act('head', id, 'Reject', {'Reason': 'ข้อมูลไม่พอ'}));
    await make('PendingTriage', 'PendingTriage', null, (id) => act('head', id, 'Approve'));
    await make('WaitingInfoNew', 'WaitingInfo/untriaged', null, (id) async { await act('head', id, 'Approve'); await act('lead', id, 'RequestInfo', {'Reason': 'ขอ log'}); });
    await make('QueuedNew', 'Queued/notStarted', 'dev1', (id) async { await act('head', id, 'Approve'); await triage(id); });
    await make('QueuedDev2', 'Queued/notStarted', 'dev2', (id) async { await act('head', id, 'Approve'); await triage(id); await act('lead', id, 'Reassign', {'AssigneeId': ids['dev2']}); });
    await make('InProgress', 'InProgress', 'dev1', (id) async { await act('head', id, 'Approve'); await triage(id); await act('dev1', id, 'Start'); });
    await make('WaitingInfoStarted', 'WaitingInfo/started', 'dev1', (id) async {
      await act('head', id, 'Approve'); await triage(id); await act('dev1', id, 'Start'); await act('dev1', id, 'RequestInfo', {'Reason': 'ขอรุ่นเครื่อง'});
    });
    await make('Paused', 'Queued/started', 'dev1', (id) async { await act('head', id, 'Approve'); await triage(id); await act('dev1', id, 'Start'); await act('dev1', id, 'Pause'); });
    await make('Resolved', 'Resolved', 'dev1', (id) async {
      await act('head', id, 'Approve'); await triage(id); await act('dev1', id, 'Start'); await act('dev1', id, 'Resolve', {'Resolution': 'แก้แล้ว 4.0.3'});
    });
    await make('Closed', 'Final', 'dev1', (id) async {
      await act('head', id, 'Approve'); await triage(id); await act('dev1', id, 'Start'); await act('dev1', id, 'Resolve', {'Resolution': 'แก้แล้ว 4.0.3'}); await act('rep', id, 'Close');
    });
    await make('Cancelled', 'Final', null, (id) => act('rep', id, 'Cancel', {'Reason': 'ลูกค้าแก้เองได้'}));
    await make('Declined', 'Final', 'dev1', (id) async {
      await act('head', id, 'Approve'); await triage(id); await act('dev1', id, 'Start'); await act('lead', id, 'Decline', {'Reason': 'ไม่ใช่ปัญหาโปรแกรม'});
    });
  }
}

/// Drives the running app.
class Ui {
  Ui(this.tester, this.server);

  final WidgetTester tester;
  final Server server;
  final passed = <String>[];
  final failed = <String>[];
  String? current;

  ProviderContainer get container => ProviderScope.containerOf(tester.element(find.byType(app.SwcareApp)));
  String tr(String key, [Map<String, Object?> args = const {}]) => container.read(textsProvider).t(key, args);
  GoRouter get router => container.read(routerProvider);
  String get location => router.routerDelegate.currentConfiguration.uri.toString();

  void check(String name, bool ok, [String extra = '']) {
    if (ok) {
      passed.add(name);
    } else {
      failed.add('$name $extra');
      debugPrint('FAIL $name $extra');
    }
  }

  Future<void> settle([int ms = 400]) async {
    final end = DateTime.now().add(Duration(milliseconds: ms));
    while (DateTime.now().isBefore(end)) {
      await tester.pump();
      await Future<void>.delayed(const Duration(milliseconds: 50));
    }
  }

  Future<bool> waitFor(Finder f, {int seconds = 15, bool gone = false}) async {
    final end = DateTime.now().add(Duration(seconds: seconds));
    while (DateTime.now().isBefore(end)) {
      await tester.pump();
      if (f.evaluate().isNotEmpty != gone) return true;
      await Future<void>.delayed(const Duration(milliseconds: 100));
    }
    return false;
  }

  Future<void> must(Finder f, String what, {int seconds = 15}) async {
    if (!await waitFor(f, seconds: seconds)) throw StateError('not found: $what');
  }

  Future<void> tap(Finder f) async {
    await tester.ensureVisible(f.first);
    await tester.pump();
    await tester.tap(f.first);
    await settle();
  }

  Future<void> type(Finder f, String text) async {
    await tester.ensureVisible(f.first);
    await tester.enterText(f.first, text);
    await tester.pump();
  }

  /// Any Material button (FilledButton.icon is a subclass, so byType would miss it).
  Finder button(String label, {Finder? within}) {
    final b = find.ancestor(of: find.text(label), matching: find.byWidgetPredicate((w) => w is ButtonStyleButton));
    return within == null ? b : find.descendant(of: within, matching: b);
  }

  bool enabled(Finder f) => (tester.widget(f.first) as ButtonStyleButton).enabled;

  Finder pageScrollable(Type page) => find.descendant(of: find.byType(page), matching: find.byType(Scrollable)).first;

  Future<void> scrollTo(Finder f, Type page) async {
    if (f.evaluate().isEmpty) await tester.scrollUntilVisible(f, 300, scrollable: pageScrollable(page), maxScrolls: 40);
    await tester.ensureVisible(f.first);
    await tester.pump();
  }

  void clearSnacks() => ScaffoldMessenger.of(tester.element(find.byType(Navigator).first)).clearSnackBars();

  Future<void> go(String path) async {
    router.go(path);
    await settle(700);
  }

  // ---- login / logout

  Future<void> login(String role) async {
    if (current == role) return;
    if (current != null) await logout();
    final username = role == 'admin' ? 'admin' : people[role]!.$1;
    await must(button(tr('auth:login')), 'login page');
    await type(find.widgetWithText(TextFormField, tr('auth:username')), username);
    await type(find.widgetWithText(TextFormField, tr('auth:password')), password);
    await tap(button(tr('auth:login')));
    await must(find.byWidgetPredicate((w) => w is MyTasksPage || w is DashboardPage), 'home after login as $role');
    await settle(500);
    current = role;
  }

  Future<void> logout() async {
    await go('/more');
    final out = find.text(tr('common:logout'));
    await must(out, 'logout');
    await tap(out);
    await must(button(tr('auth:login')), 'login page after logout');
    current = null;
  }

  // ---- ticket page

  Future<bool> openTicket(int id, String title) async {
    if (location == '/ticket/$id') await go('/my');
    await go('/ticket/$id');
    clearSnacks();
    final loaded = find.byWidgetPredicate((w) => (w is SelectableText && w.data == title) || w is ErrorView);
    await must(loaded, 'ticket $id');
    return find.byType(ErrorView).evaluate().isEmpty;
  }

  static bool _inSection(Element e) {
    var found = false;
    e.visitAncestorElements((a) {
      found = a.widget.runtimeType.toString() == '_Section';
      return !found;
    });
    return found;
  }

  /// Everything this user can do on the open ticket page: the status buttons, edit, comment box, file upload, watchers.
  Future<Set<String>> scanTicket() async {
    final seen = <String>{};
    void collect() {
      for (final a in statusActions) {
        if (find.descendant(of: find.byType(TicketActions), matching: find.text(tr('ticket:action.$a'))).evaluate().isNotEmpty) seen.add(a);
      }
      if (find.byTooltip(tr('ticket:edit')).evaluate().isNotEmpty) seen.add('Edit');
      if (find.text(tr('ticket:comment.placeholder')).evaluate().isNotEmpty) seen.add('Comment');
      // Buttons only: the timeline line WatcherAdded has the same words.
      if (button(tr('ticket:watchers.add')).evaluate().isNotEmpty) seen.add('ManageWatchers');
      if (button(tr('ticket:watchers.follow')).evaluate().isNotEmpty) seen.add('Follow');
      if (button(tr('ticket:watchers.unfollow')).evaluate().isNotEmpty) seen.add('Unfollow');
      if (find.byType(FilePickerBar).evaluate().any((e) => !_inSection(e))) seen.add('AddAttachment');
    }

    final position = tester.state<ScrollableState>(pageScrollable(TicketDetailPage)).position;
    position.jumpTo(0);
    await tester.pump();
    while (true) {
      collect();
      if (position.pixels >= position.maxScrollExtent) break;
      position.jumpTo(min(position.pixels + 350, position.maxScrollExtent));
      await tester.pump();
    }
    position.jumpTo(0);
    await tester.pump();
    return seen;
  }

  String sorted(Iterable<String> xs) => (xs.toList()..sort()).join(',');

  /// Press a status button, fill its dialog or sheet, and check the result against the API.
  Future<Map<String, dynamic>?> act(String role, int id, String title, String action, {String? text, Future<void> Function()? fill, String? expect}) async {
    await login(role);
    await openTicket(id, title);
    final label = '$action #$id by $role';
    final b = button(tr('ticket:action.$action'), within: find.byType(TicketActions));
    check('$label: button shown', b.evaluate().isNotEmpty);
    if (b.evaluate().isEmpty) return null;
    await tap(b);
    if (text != null) {
      final dialog = find.byType(AlertDialog);
      await must(dialog, '$action dialog');
      final confirm = button(tr('ticket:action.confirm'), within: dialog);
      check('$label: ยืนยัน off while empty', !enabled(confirm));
      await type(find.descendant(of: dialog, matching: find.byType(TextField)), text);
      check('$label: ยืนยัน on after typing', enabled(confirm));
      await tap(confirm);
    }
    if (fill != null) await fill();
    check('$label: snack ${tr('ticket:action.done')}', await waitFor(find.text(tr('ticket:action.done'))));
    final t = await server.ticket(id);
    if (expect != null) check('$label: status $expect', t['Status'] == expect, 'got ${t['Status']}');
    return t;
  }

  Future<void> sheetConfirm(Type sheet) => tap(button(tr('ticket:action.confirm'), within: find.byType(sheet)));

  Future<void> pickProgrammer(Type sheet, String name) async {
    await tap(find.descendant(of: find.byType(sheet), matching: find.byType(ProgrammerField)));
    await tap(find.text(name).last);
  }

  Future<void> comment(String role, int id, String title, String body) async {
    await login(role);
    await openTicket(id, title);
    final box = find.widgetWithText(TextField, tr('ticket:comment.placeholder'));
    await scrollTo(box, TicketDetailPage);
    await type(box, body);
    await tap(button(tr('ticket:comment.send')));
    check('comment by $role on #$id shown', await waitFor(find.byWidgetPredicate((w) => w is SelectableText && w.data == body)));
  }
}

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('every role, every button', (tester) async {
    expect(apiUrl.isNotEmpty && adminSeedPassword.isNotEmpty && password.isNotEmpty, isTrue, reason: 'pass API_URL, ADMIN_PW, TEST_PW with --dart-define');
    final server = Server();
    await server.setUp();
    final fx = server.fixtures;
    String title(String name) => '[$name] ทดสอบปุ่ม';

    await const FlutterSecureStorage().deleteAll();
    await (await SharedPreferences.getInstance()).clear();
    await app.main();
    final ui = Ui(tester, server);
    await ui.must(find.byType(app.SwcareApp), 'app');
    await ui.must(ui.button(ui.tr('auth:login')), 'login page');
    final tr = ui.tr;

    Future<void> stage(String name, Future<void> Function() body) async {
      debugPrint('--- $name');
      try {
        await body();
      } catch (e, s) {
        ui.check('$name: finished', false, '$e\n$s');
        // Back to a known state: logged out.
        try {
          if (ui.current != null) await ui.logout();
        } catch (_) {
          ui.current = null;
        }
      }
    }

    // ---- wrong password stays on the login page with an error
    await stage('login errors', () async {
      await ui.tap(ui.button(tr('auth:login')));
      ui.check('login: empty fields -> required', find.text(tr('validation:required')).evaluate().length == 2);
      await ui.type(find.widgetWithText(TextFormField, tr('auth:username')), 'sale1');
      await ui.type(find.widgetWithText(TextFormField, tr('auth:password')), 'wrong-password');
      await ui.tap(ui.button(tr('auth:login')));
      ui.check('login: wrong password -> snack, still on login', await ui.waitFor(find.byType(SnackBar)) && find.byType(MyTasksPage).evaluate().isEmpty);
      await ui.tap(ui.button(tr('auth:loginWithGoogle')));
      ui.check('login: Google -> coming soon', await ui.waitFor(find.text(tr('auth:googleSoon'))));
    });

    // ---- each role: home page, menu, tabs, guarded pages, the buttons on every fixture
    for (final role in ['rep', 'head', 'req', 'watch', 'supHead', 'sup1', 'lead', 'dev1', 'dev2', 'admin', 'exec', 'viewAll', 'otherHead', 'outsider']) {
      await stage('role $role', () async {
        await ui.login(role);
        final isExec = role == 'exec';
        ui.check('$role: lands on ${isExec ? 'Dashboard' : 'งานของฉัน'}', ui.location == (isExec ? '/dashboard' : '/my'), ui.location);
        if (isExec) {
          ui.check('exec: dashboard KPIs', await ui.waitFor(find.text(tr('dashboard:kpi.open'))));
          await ui.tap(find.byTooltip(tr('navigation:myTasks')));
          ui.check('exec: home button -> งานของฉัน', ui.location == '/my', ui.location);
        }

        // งานของฉัน tabs
        await ui.must(find.byType(ChoiceChip), 'my tasks tabs');
        final tabs = [if (programmerRoles.contains(role)) 'ToDo', if (headRoles.contains(role)) 'ToApprove', 'ToClose', 'Reported', 'Following'];
        // The chips scroll sideways: look at both ends.
        final seenTabs = <String>{};
        final chipRow = tester.state<ScrollableState>(find.ancestor(of: find.byType(ChoiceChip).first, matching: find.byType(Scrollable)).first).position;
        for (final at in [0.0, chipRow.maxScrollExtent]) {
          chipRow.jumpTo(at);
          await tester.pump();
          for (final t in ['ToDo', 'ToApprove', 'ToClose', 'Reported', 'Following']) {
            if (find.descendant(of: find.byType(ChoiceChip), matching: find.text(tr('ticket:tab.$t'))).evaluate().isNotEmpty) seenTabs.add(t);
          }
        }
        chipRow.jumpTo(0);
        final shown = [for (final t in ['ToDo', 'ToApprove', 'ToClose', 'Reported', 'Following']) if (seenTabs.contains(t)) t];
        ui.check('$role: งานของฉัน tabs', shown.join(',') == tabs.join(','), 'got $shown want $tabs');

        // เมนู tab
        await ui.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text(tr('menu'))));
        await ui.must(find.text(tr('common:logout')), 'menu page');
        for (final (key, roles) in [
          ('navigation:dashboard', dashboardRoles),
          ('navigation:approvals', headRoles),
          ('navigation:triage', {'lead'}),
          ('navigation:kanban', programmerRoles),
        ]) {
          final has = find.text(tr(key)).evaluate().isNotEmpty;
          ui.check('$role: menu ${tr(key)} ${roles.contains(role) ? 'shown' : 'hidden'}', has == roles.contains(role));
        }

        // Pages opened by address: the guard sends others back to งานของฉัน
        for (final (path, roles) in [('/dashboard', dashboardRoles), ('/approvals', headRoles), ('/triage', {'lead'}), ('/board', programmerRoles)]) {
          await ui.go(path);
          final allowed = roles.contains(role);
          ui.check('$role: $path ${allowed ? 'opens' : 'redirected'}', ui.location == (allowed ? path : '/my'), ui.location);
          if (allowed) ui.check('$role: $path loads without error', await ui.waitFor(find.byType(ErrorView), seconds: 2, gone: true) || find.byType(ErrorView).evaluate().isEmpty);
        }

        if (role == 'sup1') return; // not in the fixture table
        for (final MapEntry(key: name, value: f) in fx.entries) {
          final want = expected(f.stage, f.assignee)[role];
          final visible = await ui.openTicket(f.id, title(name));
          if (want == null) {
            final error = find.byType(ErrorView).evaluate().isEmpty ? null : tester.widget<ErrorView>(find.byType(ErrorView)).error;
            ui.check('$role: $name not visible (404)', !visible && error is ApiException && error.status == 404, 'visible=$visible error=$error');
            continue;
          }
          if (!visible) {
            ui.check('$role: $name visible', false, '${tester.widget<ErrorView>(find.byType(ErrorView)).error}');
            continue;
          }
          final seen = await ui.scanTicket();
          ui.check('$role: $name buttons', ui.sorted(seen) == ui.sorted(want), 'got ${ui.sorted(seen)} want ${ui.sorted(want)}');
        }
      });
    }

    // ---- lists
    await stage('lists', () async {
      await ui.login('lead');
      await ui.go('/more');
      await ui.tap(find.text(tr('navigation:triage')));
      ui.check('lead: รอจัดคิว lists [PendingTriage]', await ui.waitFor(find.text(title('PendingTriage'))));
      ui.check('lead: รอจัดคิว has no [QueuedNew]', find.text(title('QueuedNew')).evaluate().isEmpty);
      await ui.go('/board');
      final inProgress = find.text(title('InProgress'));
      await ui.must(find.byType(StatusChip), 'kanban');
      await tester.scrollUntilVisible(inProgress, 300, scrollable: ui.pageScrollable(BoardPage), maxScrolls: 40);
      ui.check('lead: Kanban shows [InProgress]', inProgress.evaluate().isNotEmpty);
      await ui.tap(inProgress);
      ui.check('lead: Kanban card opens the ticket', await ui.waitFor(find.byType(TicketDetailPage)));

      await ui.login('rep');
      await ui.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text(tr('navigation:allTickets'))));
      await ui.type(find.widgetWithText(TextField, tr('ticket:list.search')), 'InProgress');
      ui.check('rep: รายงานทั้งหมด search finds [InProgress]', await ui.waitFor(find.text(title('InProgress'))));
      ui.check('rep: search hides others', await ui.waitFor(find.text(title('PendingApproval')), gone: true));
      await ui.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text(tr('notification:title'))));
      final note = find.byType(ListTile);
      ui.check('rep: has notifications', await ui.waitFor(note));
      await ui.tap(note);
      ui.check('rep: notification opens its page', await ui.waitFor(find.byWidgetPredicate((w) => w is TicketDetailPage || w is ScopePage)));

      await ui.login('outsider');
      await ui.tap(find.descendant(of: find.byType(NavigationBar), matching: find.text(tr('navigation:allTickets'))));
      ui.check('outsider: รายงานทั้งหมด empty', await ui.waitFor(find.text(tr('ticket:list.empty'))) && find.byType(TicketCard).evaluate().isEmpty);

      await ui.login('sup1');
      await ui.go('/ticket/new');
      ui.check('sup1 (Support): ผลการทดสอบ required hint', await ui.waitFor(find.text(tr('ticket:hint.testedRequired'))));
    });

    // ---- head approves from รออนุมัติ
    await stage('approve from list', () async {
      await ui.login('head');
      await ui.go('/more');
      await ui.tap(find.text(tr('navigation:approvals')));
      final card = find.text(title('PendingApproval'));
      ui.check('head: รออนุมัติ lists [PendingApproval]', await ui.waitFor(card));
      await ui.tap(card);
      await ui.must(find.byType(TicketActions), 'actions');
      ui.clearSnacks();
      await ui.tap(ui.button(tr('ticket:action.Approve'), within: find.byType(TicketActions)));
      ui.check('approve from list: snack', await ui.waitFor(find.text(tr('ticket:action.done'))));
      final t = await server.ticket(fx['PendingApproval']!.id);
      ui.check('approve from list: PendingTriage', t['Status'] == 'PendingTriage', '${t['Status']}');
      await ui.go('/approvals');
      ui.check('head: approved ticket leaves รออนุมัติ', await ui.waitFor(card, gone: true));
    });

    // ---- a new ticket through every step
    var newId = 0;
    var newTitle = '[Mobile] แจ้งจากแอป';
    await stage('new ticket', () async {
      await ui.login('rep');
      await ui.go('/my');
      // The FAB on screen: the pages of the other tabs stay in the tree behind it.
      final fab = find.byType(FloatingActionButton).hitTestable();
      await ui.must(fab, 'สร้างงาน button');
      await ui.tap(fab);
      final titleField = find.widgetWithText(TextFormField, '${tr('ticket:field.title')} *');
      await ui.must(titleField, 'form');
      final submit = ui.button(tr('ticket:submit'));
      await ui.scrollTo(submit, TicketFormPage);
      await ui.tap(submit);
      ui.check('new: empty form -> required', find.text(tr('validation:required')).evaluate().isNotEmpty);
      tester.state<ScrollableState>(ui.pageScrollable(TicketFormPage)).position.jumpTo(0);
      await tester.pump();
      await ui.type(titleField, newTitle);
      await ui.tap(find.widgetWithText(DropdownButtonFormField<int>, '${tr('ticket:field.product')} *'));
      await ui.tap(find.text(server.productName).last);
      final desc = find.widgetWithText(TextFormField, '${tr('ticket:field.description')} *');
      await ui.scrollTo(desc, TicketFormPage);
      await ui.type(desc, 'ทดสอบแจ้งงานจากมือถือ');
      final requester = find.text(tr('ticket:field.requester'));
      await ui.scrollTo(requester, TicketFormPage);
      await ui.tap(requester);
      final dialog = find.byType(UserSearchDialog);
      await ui.must(dialog, 'requester dialog');
      await ui.type(find.descendant(of: dialog, matching: find.byType(TextField)), 'สมหญิง');
      final pick = find.descendant(of: dialog, matching: find.text('สมหญิง เซลล์'));
      await ui.must(pick, 'requester result');
      await ui.tap(pick);
      await ui.scrollTo(submit, TicketFormPage);
      await ui.tap(submit);
      await ui.must(find.byWidgetPredicate((w) => w is SelectableText && w.data == newTitle), 'created ticket page');
      newId = tester.widget<TicketDetailPage>(find.byType(TicketDetailPage).last).id;
      final t = await server.ticket(newId);
      ui.check('new: PendingApproval, requester สมหญิง', t['Status'] == 'PendingApproval' && t['Requester']?['DisplayName'] == 'สมหญิง เซลล์', '${t['Status']} ${t['Requester']}');
    });

    if (newId > 0) {
      await stage('new ticket flow', () async {
        await ui.act('head', newId, newTitle, 'Reject', text: 'ขอรายละเอียดเพิ่ม', expect: 'Rejected');

        // แก้ไข by the reporter
        await ui.login('rep');
        await ui.openTicket(newId, newTitle);
        await ui.tap(find.byTooltip(tr('ticket:edit')));
        final titleField = find.widgetWithText(TextFormField, '${tr('ticket:field.title')} *');
        await ui.must(titleField, 'edit form');
        newTitle = '$newTitle (แก้)';
        await ui.type(titleField, newTitle);
        final save = ui.button(tr('ticket:saveChanges'));
        await ui.scrollTo(save, TicketFormPage);
        await ui.tap(save);
        ui.check('edit: back on the ticket with the new title', await ui.waitFor(find.byWidgetPredicate((w) => w is SelectableText && w.data == newTitle)));
        ui.check('edit: saved', (await server.ticket(newId))['Title'] == newTitle);

        await ui.act('rep', newId, newTitle, 'Resubmit', expect: 'PendingApproval');
        await ui.act('head', newId, newTitle, 'Approve', expect: 'PendingTriage');
        await ui.act('lead', newId, newTitle, 'RequestInfo', text: 'ขอ log เพิ่ม', expect: 'WaitingInfo');
        await ui.comment('rep', newId, newTitle, 'แนบ log ให้แล้วครับ');
        ui.check('reporter answer -> back to PendingTriage', (await server.ticket(newId))['Status'] == 'PendingTriage');

        await ui.act('lead', newId, newTitle, 'Triage', expect: 'Queued', fill: () async {
          await ui.must(find.byType(TriageSheet), 'triage sheet');
          await ui.pickProgrammer(TriageSheet, 'พี่เกม');
          await ui.sheetConfirm(TriageSheet);
        });
        ui.check('triage: assigned to พี่เกม', (await server.ticket(newId))['Assignee']?['DisplayName'] == 'พี่เกม');

        await ui.act('dev2', newId, newTitle, 'Start', expect: 'InProgress');
        await ui.act('dev2', newId, newTitle, 'Pause', expect: 'Queued');
        await ui.act('dev2', newId, newTitle, 'Start', expect: 'InProgress');

        final before = await server.ticket(newId);
        final startDay = DateTime.parse(before['PlannedStartDate'] as String).day;
        await ui.act('dev2', newId, newTitle, 'Reschedule', fill: () async {
          await ui.must(find.byType(RescheduleSheet), 'reschedule sheet');
          final saveOff = ui.button(tr('ticket:action.confirm'), within: find.byType(RescheduleSheet));
          ui.check('reschedule: ยืนยัน off without a reason', !ui.enabled(saveOff));
          await ui.tap(find.descendant(of: find.byType(RescheduleSheet), matching: find.byType(DateField)).first);
          final picker = find.byType(DatePickerDialog);
          await ui.must(picker, 'date picker');
          await ui.tap(find.descendant(of: picker, matching: find.text(startDay == 1 ? '2' : '1')));
          await ui.tap(find.descendant(of: picker, matching: find.text(MaterialLocalizations.of(tester.element(picker)).okButtonLabel)));
          await ui.type(find.descendant(of: find.byType(RescheduleSheet), matching: find.byType(TextField)), 'รอข้อมูลลูกค้า');
          await ui.sheetConfirm(RescheduleSheet);
        });
        final after = await server.ticket(newId);
        ui.check('reschedule: start date moved', after['PlannedStartDate'] != before['PlannedStartDate'], '${after['PlannedStartDate']}');

        await ui.act('lead', newId, newTitle, 'Reassign', fill: () async {
          await ui.must(find.byType(ReassignSheet), 'reassign sheet');
          ui.check('reassign: ยืนยัน off for the same person', !ui.enabled(ui.button(tr('ticket:action.confirm'), within: find.byType(ReassignSheet))));
          await ui.pickProgrammer(ReassignSheet, 'พี่ป้อม');
          await ui.sheetConfirm(ReassignSheet);
        });
        ui.check('reassign: now พี่ป้อม', (await server.ticket(newId))['Assignee']?['DisplayName'] == 'พี่ป้อม');

        await ui.act('dev1', newId, newTitle, 'RequestInfo', text: 'ขอรุ่นเครื่อง', expect: 'WaitingInfo');
        await ui.comment('rep', newId, newTitle, 'รุ่น A920');
        ui.check('reporter answer -> back to InProgress', (await server.ticket(newId))['Status'] == 'InProgress');
        await ui.act('dev1', newId, newTitle, 'Resolve', text: 'แก้ใน 4.0.4', expect: 'Resolved');
        await ui.act('head', newId, newTitle, 'SendBack', text: 'ยังเจออยู่', expect: 'InProgress');
        await ui.act('dev1', newId, newTitle, 'Resolve', text: 'แก้ใน 4.0.5', expect: 'Resolved');
        await ui.act('rep', newId, newTitle, 'Close', expect: 'Closed', fill: () async {
          await ui.must(find.byType(AlertDialog), 'close confirm');
          await ui.tap(ui.button(tr('ticket:action.confirm'), within: find.byType(AlertDialog)));
        });
        await ui.openTicket(newId, newTitle);
        final seen = await ui.scanTicket();
        ui.check('closed: reporter only comments / watchers', ui.sorted(seen) == 'Comment,ManageWatchers', ui.sorted(seen));
      });
    }

    await stage('decline and cancel', () async {
      await ui.act('lead', fx['PendingTriage']!.id, title('PendingTriage'), 'Decline', text: 'ซ้ำกับงานเดิม', expect: 'Declined');
      await ui.act('rep', fx['QueuedNew']!.id, title('QueuedNew'), 'Cancel', text: 'ลูกค้าแก้เองได้', expect: 'Cancelled');
    });

    // ---- watchers, follow
    await stage('watchers', () async {
      final id = fx['InProgress']!.id;
      await ui.login('rep');
      await ui.openTicket(id, title('InProgress'));
      final add = find.text(tr('ticket:watchers.add'));
      await ui.scrollTo(add, TicketDetailPage);
      await ui.tap(add);
      final dialog = find.byType(UserSearchDialog);
      await ui.must(dialog, 'watcher dialog');
      await ui.type(find.descendant(of: dialog, matching: find.byType(TextField)), 'คนนอก');
      final pick = find.descendant(of: dialog, matching: find.text('คนนอก Product'));
      await ui.must(pick, 'watcher result');
      await ui.tap(pick);
      ui.check('watchers: คนนอก Product added', await ui.waitFor(find.widgetWithText(InputChip, 'คนนอก Product')));
      final chip = find.widgetWithText(InputChip, 'ผู้ติดตาม Product');
      await ui.scrollTo(chip, TicketDetailPage);
      await ui.tap(find.descendant(of: chip, matching: find.byTooltip(tr('ticket:watchers.remove'))));
      ui.check('watchers: ผู้ติดตาม Product removed', await ui.waitFor(chip, gone: true));
      final w = ((await server.ticket(id))['Watchers'] as List).map((x) => x['DisplayName']).toSet();
      ui.check('watchers: API agrees', w.contains('คนนอก Product') && !w.contains('ผู้ติดตาม Product'), '$w');

      await ui.login('outsider');
      ui.check('outsider: sees the ticket after being added', await ui.openTicket(id, title('InProgress')));
      await ui.login('watch');
      ui.check('watch1: no longer sees it', !await ui.openTicket(id, title('InProgress')));
    });

    await stage('follow', () async {
      for (final (role, name) in [('viewAll', 'InProgress'), ('exec', 'Resolved'), ('supHead', 'Paused')]) {
        final id = fx[name]!.id;
        await ui.login(role);
        await ui.openTicket(id, title(name));
        final follow = find.text(tr('ticket:watchers.follow'));
        await ui.scrollTo(follow, TicketDetailPage);
        await ui.tap(follow);
        ui.check('$role: ติดตาม -> เลิกติดตาม', await ui.waitFor(find.text(tr('ticket:watchers.unfollow'))));
        final seen = await ui.scanTicket();
        ui.check('$role: after follow can comment', seen.contains('Comment') && seen.contains('Unfollow') && !seen.contains('Follow'), ui.sorted(seen));
        await ui.comment(role, id, title(name), '$role ขอทราบความคืบหน้า');
        final unfollow = find.text(tr('ticket:watchers.unfollow'));
        await ui.scrollTo(unfollow, TicketDetailPage);
        await ui.tap(unfollow);
        ui.check('$role: เลิกติดตาม -> ติดตาม', await ui.waitFor(find.text(tr('ticket:watchers.follow'))));
        final back = await ui.scanTicket();
        final want = role == 'supHead' ? 'Comment,Follow' : 'Follow';
        ui.check('$role: back to before', ui.sorted(back) == want, ui.sorted(back));
      }
    });

    // ---- language
    await stage('language', () async {
      await ui.login('rep');
      await ui.go('/more');
      await ui.tap(find.text(tr('settings')));
      ui.check('settings: opens from เมนู', await ui.waitFor(find.text(tr('common:changePassword'))));
      await ui.tap(find.text('English'));
      ui.check('language: EN', await ui.waitFor(find.text('Settings')) && ui.container.read(langProvider) == 'en');
      ui.check('language: EN saved on the account', (await server.call('GET', '/api/Auth/Me', as: 'rep'))['Language'] == 'en');
      await ui.tap(find.text('ภาษาไทย'));
      ui.check('language: back to TH', await ui.waitFor(find.text('ตั้งค่า')) && ui.container.read(langProvider) == 'th');
    });

    if (ui.current != null) await ui.logout();
    debugPrint('PASS ${ui.passed.length} · FAIL ${ui.failed.length}');
    for (final f in ui.failed) {
      debugPrint('FAIL $f');
    }
    expect(ui.failed, isEmpty);
  }, timeout: const Timeout(Duration(minutes: 40)));
}
