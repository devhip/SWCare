import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/api.dart';
import '../models/models.dart';
import 'api_client.dart';
import 'i18n.dart';

/// The logged-in user, or null. Loading while the saved tokens are checked at start.
class SessionController extends AsyncNotifier<Me?> {
  TokenStore get _tokens => ref.read(tokenStoreProvider);
  SwcareApi get _api => ref.read(apiProvider);

  @override
  Future<Me?> build() async {
    // The API client is rebuilt when the server changes; keep telling us when the session runs out.
    ref.listen(apiClientProvider, (_, client) => client.onSessionExpired = _expired, fireImmediately: true);
    if (_tokens.access == null) return null;
    try {
      final me = await _api.me();
      _useLanguage(me);
      return me;
    } on ApiException catch (e) {
      if (e.code == 'SESSION_EXPIRED' || e.status == 401) {
        await _tokens.clear();
        return null;
      }
      rethrow;
    }
  }

  Future<void> login(String username, String password) async {
    final result = await _api.login(username, password);
    await _start(result);
  }

  Future<void> changePassword(String current, String next) async {
    final result = await _api.changePassword(current, next);
    await _start(result);
  }

  Future<void> logout() async {
    final refresh = _tokens.refresh;
    if (refresh != null) {
      try {
        await _api.logout(refresh);
      } on ApiException {
        // Logged out on the phone anyway.
      }
    }
    await _tokens.clear();
    state = const AsyncData(null);
  }

  /// The account remembers its language (Q49); the phone follows it after login.
  Future<void> setLanguage(String lang) async {
    ref.read(langProvider.notifier).set(lang);
    if (state.value == null) return;
    try {
      state = AsyncData(await _api.setLanguage(lang));
    } on ApiException {
      // Kept on the phone; the account is updated next time.
    }
  }

  Future<void> reload() async {
    if (_tokens.access == null) return;
    state = AsyncData(await _api.me());
  }

  Future<void> _start(Map<String, dynamic> loginResult) async {
    await _tokens.save(loginResult['AccessToken'] as String, loginResult['RefreshToken'] as String);
    final me = Me.fromJson(loginResult['User'] as Map<String, dynamic>);
    _useLanguage(me);
    state = AsyncData(me);
  }

  void _useLanguage(Me me) => ref.read(langProvider.notifier).set(me.language);

  void _expired() {
    _tokens.clear();
    state = const AsyncData(null);
  }
}

final sessionProvider = AsyncNotifierProvider<SessionController, Me?>(SessionController.new);

/// The user of a logged-in page (the router only shows those pages with a user).
final meProvider = NotifierProvider<_MeHolder, Me>(_MeHolder.new);

/// On logout the pages of the tabs rebuild once before the router takes them down: they keep the last user.
class _MeHolder extends Notifier<Me> {
  @override
  Me build() {
    final me = ref.watch(sessionProvider).value;
    if (me != null) return me;
    if (stateOrNull case final last?) return last;
    throw StateError('meProvider read before login');
  }
}
