import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/api_client.dart';
import '../models/models.dart';

/// A file picked on the phone (camera, gallery or files), sent as multipart.
class PickedFile {
  PickedFile({required this.name, required this.bytes});

  final String name;
  final List<int> bytes;

  int get size => bytes.length;

  MultipartFile toMultipart() => MultipartFile.fromBytes(bytes, filename: name);
}

/// The calls of web/lib/*.api.ts for the app.
class SwcareApi {
  SwcareApi(this._c);

  final ApiClient _c;

  // Auth
  Future<Map<String, dynamic>> login(String username, String password) async =>
      (await _c.post('api/Auth/Login', {'Username': username, 'Password': password})).result as Map<String, dynamic>;

  Future<Map<String, dynamic>> changePassword(String current, String next) async =>
      (await _c.post('api/Auth/ChangePassword', {'CurrentPassword': current, 'NewPassword': next})).result as Map<String, dynamic>;

  Future<void> logout(String refreshToken) => _c.post('api/Auth/Logout', {'RefreshToken': refreshToken});

  Future<Me> me() async => Me.fromJson((await _c.get('api/Auth/Me')).result as Map<String, dynamic>);

  Future<Me> setLanguage(String lang) async =>
      Me.fromJson((await _c.put('api/Auth/Me/Language', {'Language': lang})).result as Map<String, dynamic>);

  // Tickets
  Future<(List<TicketListItem>, int)> tickets({
    String scope = 'All',
    List<String> statuses = const [],
    String? search,
    int page = 1,
    int pageLimit = 20,
  }) async {
    // ASP.NET binds a list from repeated keys: Statuses=A&Statuses=B (dio repeats a List with ListFormat.multi).
    final res = await _c.get('api/Tickets', query: {
      'Scope': scope,
      if (statuses.isNotEmpty) 'Statuses': ListParam(statuses, ListFormat.multi),
      if (search != null && search.isNotEmpty) 'Search': search,
      'PageNumber': page,
      'PageLimit': pageLimit,
    });
    return (
      (res.result as List<dynamic>).map((e) => TicketListItem.fromJson(e as Map<String, dynamic>)).toList(),
      res.totalRecords,
    );
  }

  Future<TicketCounts> counts() async => TicketCounts.fromJson((await _c.get('api/Tickets/Counts')).result as Map<String, dynamic>);

  Future<Board> board() async => Board.fromJson((await _c.get('api/Tickets/Board')).result as Map<String, dynamic>);

  Future<TicketDetail> ticket(int id) async => _detail(await _c.get('api/Tickets/$id'));

  /// One endpoint per status change: POST api/Tickets/{id}/{action} (plan 4.3).
  Future<TicketDetail> action(int id, String action, [Map<String, dynamic>? body]) async =>
      _detail(await _c.post('api/Tickets/$id/$action', body));

  Future<TicketDetail> addWatcher(int id, int userId) async => _detail(await _c.post('api/Tickets/$id/Watchers', {'UserId': userId}));

  Future<TicketDetail> removeWatcher(int id, int userId) async => _detail(await _c.delete('api/Tickets/$id/Watchers/$userId'));

  Future<TicketDetail> follow(int id) async => _detail(await _c.post('api/Tickets/$id/Follow'));

  Future<TicketDetail> unfollow(int id) async => _detail(await _c.delete('api/Tickets/$id/Follow'));

  /// Multipart: the ticket as JSON plus its files, in one request.
  Future<TicketDetail> createTicket(Map<String, dynamic> ticket, List<PickedFile> files) async =>
      _detail(await _c.postForm('api/Tickets', () => FormData.fromMap({
            'Ticket': jsonEncode(ticket),
            'Files': [for (final f in files) f.toMultipart()],
          })));

  Future<TicketDetail> updateTicket(int id, Map<String, dynamic> ticket) async => _detail(await _c.put('api/Tickets/$id', ticket));

  Future<TicketDetail> addFiles(int id, List<PickedFile> files) async =>
      _detail(await _c.postForm('api/Tickets/$id/Attachments', () => FormData.fromMap({
            'Files': [for (final f in files) f.toMultipart()],
          })));

  Future<TicketDetail> addComment(int id, String body, List<PickedFile> files) async =>
      _detail(await _c.postForm('api/Tickets/$id/Comments', () => FormData.fromMap({
            'Body': body,
            'Files': [for (final f in files) f.toMultipart()],
          })));

  Future<List<int>> attachmentBytes(int ticketId, int attachmentId) => _c.download('api/Tickets/$ticketId/Attachments/$attachmentId');

  Future<List<String>> customerNames(String search) async =>
      ((await _c.get('api/Tickets/CustomerNames', query: {'search': search})).result as List<dynamic>).cast<String>();

  // Master data
  Future<List<Product>> products() async =>
      ((await _c.get('api/Products')).result as List<dynamic>).map((e) => Product.fromJson(e as Map<String, dynamic>)).toList();

  Future<List<DeviceModel>> deviceModels() async =>
      ((await _c.get('api/DeviceModels')).result as List<dynamic>).map((e) => DeviceModel.fromJson(e as Map<String, dynamic>)).toList();

  Future<String> testedDetailsTemplate() async =>
      ((await _c.get('api/Settings')).result as Map<String, dynamic>)['TestedDetailsTemplate'] as String? ?? '';

  Future<List<UserLookup>> userLookup(String search) async =>
      ((await _c.get('api/Users/Lookup', query: {'search': search})).result as List<dynamic>)
          .map((e) => UserLookup.fromJson(e as Map<String, dynamic>))
          .toList();

  Future<List<UserLookup>> programmers() async =>
      ((await _c.get('api/Users/Programmers')).result as List<dynamic>).map((e) => UserLookup.fromJson(e as Map<String, dynamic>)).toList();

  // Notifications
  Future<List<AppNotification>> notifications({int pageLimit = 50}) async =>
      ((await _c.get('api/Notifications', query: {'PageLimit': pageLimit})).result as List<dynamic>)
          .map((e) => AppNotification.fromJson(e as Map<String, dynamic>))
          .toList();

  Future<int> unreadCount() async => ((await _c.get('api/Notifications/UnreadCount')).result as num).toInt();

  Future<void> markRead(int id) => _c.post('api/Notifications/$id/Read');

  Future<void> markAllRead() => _c.post('api/Notifications/ReadAll');

  // Dashboard (summary on the phone, plan 9)
  Future<Map<String, dynamic>> dashboard() async => (await _c.get('api/Dashboard')).result as Map<String, dynamic>;

  static TicketDetail _detail(Envelope e) => TicketDetail.fromJson(e.result as Map<String, dynamic>);
}

final apiProvider = Provider<SwcareApi>((ref) => SwcareApi(ref.watch(apiClientProvider)));
