import 'dart:async';
import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'i18n.dart';

/// Where the API is. The build sets it with `--dart-define=API_URL=https://...`; the login page can change it
/// (like public/config.json of the web: one build for every server).
class ServerConfig extends Notifier<String> {
  static const _key = 'swcare.apiUrl';

  static String get buildDefault {
    const fromBuild = String.fromEnvironment('API_URL');
    if (fromBuild.isNotEmpty) return fromBuild;
    // The Android emulator reaches the PC as 10.0.2.2.
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) return 'http://10.0.2.2:5300';
    return 'http://localhost:5300';
  }

  @override
  String build() => ref.watch(prefsProvider).getString(_key) ?? buildDefault;

  void set(String url) {
    final value = url.trim().replaceAll(RegExp(r'/+$'), '');
    state = value.isEmpty ? buildDefault : value;
    ref.read(prefsProvider).setString(_key, state);
  }
}

final serverConfigProvider = NotifierProvider<ServerConfig, String>(ServerConfig.new);

/// An API call that failed: [code] is the envelope's Code (ErrorCodes in the API), NETWORK or SESSION_EXPIRED.
class ApiException implements Exception {
  ApiException(this.code, this.message, {this.status});

  final String code;
  final String? message;
  final int? status;

  /// The text for the user: errors.json by code, like lib/apiErrorToast.ts of the web.
  String text(Texts texts) {
    final known = texts.lookup('errors:$code');
    if (known != null) return known;
    if (code == 'NETWORK') return texts.t('errors.network');
    return (message?.isNotEmpty ?? false) ? message! : texts.t('errors:requestFailed');
  }

  @override
  String toString() => 'ApiException($code, $message)';
}

/// Access and refresh token, kept in the phone's secure storage.
class TokenStore {
  TokenStore(this._storage);

  final FlutterSecureStorage _storage;
  String? access;
  String? refresh;

  Future<void> load() async {
    access = await _storage.read(key: 'swcare.access');
    refresh = await _storage.read(key: 'swcare.refresh');
  }

  Future<void> save(String accessToken, String refreshToken) async {
    access = accessToken;
    refresh = refreshToken;
    await _storage.write(key: 'swcare.access', value: accessToken);
    await _storage.write(key: 'swcare.refresh', value: refreshToken);
  }

  Future<void> clear() async {
    access = null;
    refresh = null;
    await _storage.delete(key: 'swcare.access');
    await _storage.delete(key: 'swcare.refresh');
  }
}

final tokenStoreProvider = Provider<TokenStore>((ref) => throw UnimplementedError());

/// Every response is the envelope { Success, Message, Result, Code } (PascalCase).
class Envelope {
  Envelope(this.json);

  final Map<String, dynamic> json;

  dynamic get result => json['Result'];
  int get totalRecords => (json['PageInfo'] as Map<String, dynamic>?)?['TotalRecords'] as int? ?? 0;
}

/// dio with the token, one retry after a refresh on 401 (each refresh token works once), like lib/http.ts.
class ApiClient {
  ApiClient(this._baseUrl, this._tokens, {this.onSessionExpired});

  final String _baseUrl;
  final TokenStore _tokens;
  void Function()? onSessionExpired;
  Future<bool>? _refreshing;

  late final Dio _dio = Dio(BaseOptions(
    baseUrl: '$_baseUrl/',
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 60),
    validateStatus: (_) => true,
  ));

  String get baseUrl => _baseUrl;

  static const _authCall = ['api/Auth/Login', 'api/Auth/RefreshToken', 'api/Auth/Logout'];

  Future<Envelope> get(String path, {Map<String, dynamic>? query}) => _send('GET', path, query: query);
  Future<Envelope> post(String path, [Object? body]) => _send('POST', path, body: () => body ?? <String, dynamic>{});
  Future<Envelope> put(String path, [Object? body]) => _send('PUT', path, body: () => body ?? <String, dynamic>{});
  Future<Envelope> delete(String path) => _send('DELETE', path);

  /// Multipart: [build] makes a new FormData for each try, as one can be sent once only.
  Future<Envelope> postForm(String path, FormData Function() build) => _send('POST', path, body: build);

  /// Bytes of a file (attachments go through the API, never a public URL).
  Future<List<int>> download(String path) async {
    final res = await _raw('GET', path, responseType: ResponseType.bytes);
    if (res.statusCode == 200) return res.data as List<int>;
    throw _failure(res);
  }

  Future<Envelope> _send(String method, String path, {Object? Function()? body, Map<String, dynamic>? query}) async {
    final res = await _raw(method, path, body: body, query: query);
    final data = res.data;
    final json = data is Map<String, dynamic> ? data : (data is String && data.isNotEmpty ? _tryJson(data) : null);
    if (res.statusCode != null && res.statusCode! >= 200 && res.statusCode! < 300 && json != null && json['Success'] != false) {
      return Envelope(json);
    }
    throw _failure(res, json);
  }

  Future<Response<dynamic>> _raw(String method, String path,
      {Object? Function()? body, Map<String, dynamic>? query, ResponseType responseType = ResponseType.json}) async {
    Future<Response<dynamic>> once() => _dio.request<dynamic>(
          path,
          data: body?.call(),
          queryParameters: query,
          options: Options(
            method: method,
            responseType: responseType,
            headers: {if (_tokens.access != null) 'Authorization': 'Bearer ${_tokens.access}'},
          ),
        );

    Response<dynamic> res;
    try {
      res = await once();
    } on DioException catch (e) {
      throw ApiException('NETWORK', e.message);
    }

    if (res.statusCode == 401 && !_authCall.contains(path)) {
      final failed = _tokens.access;
      _refreshing ??= _refresh(failed).whenComplete(() => _refreshing = null);
      if (await _refreshing!) {
        try {
          return await once();
        } on DioException catch (e) {
          throw ApiException('NETWORK', e.message);
        }
      }
      onSessionExpired?.call();
      throw ApiException('SESSION_EXPIRED', null, status: 401);
    }
    return res;
  }

  Future<bool> _refresh(String? failedAccess) async {
    if (_tokens.access != null && _tokens.access != failedAccess) return true;
    final refresh = _tokens.refresh;
    if (refresh == null) return false;
    try {
      final res = await _dio.post<dynamic>('api/Auth/RefreshToken', data: {'RefreshToken': refresh});
      final json = res.data is Map<String, dynamic> ? res.data as Map<String, dynamic> : null;
      if (res.statusCode != 200 || json == null || json['Success'] != true) return false;
      final result = json['Result'] as Map<String, dynamic>;
      await _tokens.save(result['AccessToken'] as String, result['RefreshToken'] as String);
      return true;
    } on DioException {
      return false;
    }
  }

  static Map<String, dynamic>? _tryJson(String text) {
    try {
      final value = jsonDecode(text);
      return value is Map<String, dynamic> ? value : null;
    } on FormatException {
      return null;
    }
  }

  static ApiException _failure(Response<dynamic> res, [Map<String, dynamic>? json]) {
    json ??= res.data is Map<String, dynamic> ? res.data as Map<String, dynamic> : null;
    final code = (json?['Code'] as String?)?.trim();
    return ApiException(
      (code == null || code.isEmpty) ? 'HTTP_${res.statusCode}' : code,
      json?['Message'] as String?,
      status: res.statusCode,
    );
  }
}

final apiClientProvider = Provider<ApiClient>((ref) => ApiClient(ref.watch(serverConfigProvider), ref.watch(tokenStoreProvider)));
