import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_svg/flutter_svg.dart';

import '../../core/api_client.dart';
import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../widgets/common.dart';
import 'auth_card.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _form = GlobalKey<FormState>();
  final _username = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;
  bool _show = false;

  @override
  void dispose() {
    _username.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() => _busy = true);
    await guard(context, ref, () => ref.read(sessionProvider.notifier).login(_username.text.trim(), _password.text));
    if (mounted) setState(() => _busy = false);
  }

  Future<void> _server() async {
    final texts = ref.read(textsProvider);
    final controller = TextEditingController(text: ref.read(serverConfigProvider));
    final value = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(texts.t('server.title')),
        content: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          TextField(controller: controller, keyboardType: TextInputType.url, decoration: InputDecoration(labelText: texts.t('server.url'))),
          const SizedBox(height: 8),
          Text(texts.t('server.hint'), style: AppText.caption),
        ]),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: Text(texts.t('common:cancel'))),
          FilledButton(onPressed: () => Navigator.pop(context, controller.text), child: Text(texts.t('save'))),
        ],
      ),
    );
    if (value != null) ref.read(serverConfigProvider.notifier).set(value);
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final server = ref.watch(serverConfigProvider);
    return AuthCard(
      title: texts.t('auth:loginTitle'),
      subtitle: texts.t('auth:loginSubtitle'),
      child: Form(
        key: _form,
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          TextFormField(
            controller: _username,
            autofillHints: const [AutofillHints.username],
            textInputAction: TextInputAction.next,
            decoration: InputDecoration(labelText: texts.t('auth:username'), prefixIcon: const Icon(Icons.person_outline)),
            validator: (v) => (v?.trim().isEmpty ?? true) ? texts.t('validation:required') : null,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _password,
            obscureText: !_show,
            autofillHints: const [AutofillHints.password],
            onFieldSubmitted: (_) => _submit(),
            decoration: InputDecoration(
              labelText: texts.t('auth:password'),
              prefixIcon: const Icon(Icons.lock_outline),
              suffixIcon: IconButton(
                tooltip: texts.t(_show ? 'auth:hidePassword' : 'auth:showPassword'),
                icon: Icon(_show ? Icons.visibility_off_outlined : Icons.visibility_outlined),
                onPressed: () => setState(() => _show = !_show),
              ),
            ),
            validator: (v) => (v?.isEmpty ?? true) ? texts.t('validation:required') : null,
          ),
          const SizedBox(height: 16),
          FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? texts.t('auth:loggingIn') : texts.t('auth:login'))),
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 12),
            child: Row(children: [
              const Expanded(child: Divider()),
              Padding(padding: const EdgeInsets.symmetric(horizontal: 12), child: Text(texts.t('auth:or'), style: AppText.caption)),
              const Expanded(child: Divider()),
            ]),
          ),
          // Google login (plan 8, D15) is not wired yet, as on the web.
          OutlinedButton.icon(
            onPressed: () => showMessage(context, texts.t('auth:googleSoon')),
            icon: SvgPicture.string(_googleLogo, width: 18, height: 18),
            label: Text(texts.t('auth:loginWithGoogle')),
          ),
          const SizedBox(height: 12),
          Text(texts.t('auth:forgotPasswordHint'), style: AppText.caption, textAlign: TextAlign.center),
          const SizedBox(height: 4),
          TextButton.icon(
            onPressed: _server,
            icon: const Icon(Icons.dns_outlined, size: 16),
            label: Text(server, style: AppText.caption, overflow: TextOverflow.ellipsis),
          ),
        ]),
      ),
    );
  }
}

/// The four-colour "G" of GoogleLogo in the web's login page (Google brand guidelines keep its colours as they are).
const _googleLogo = '''
<svg viewBox="0 0 48 48" xmlns="http://www.w3.org/2000/svg">
<path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
<path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
<path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
<path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.15 1.45-4.92 2.3-8.16 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
</svg>''';
