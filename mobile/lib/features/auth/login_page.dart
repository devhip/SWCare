import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

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
            decoration: InputDecoration(labelText: texts.t('auth:username')),
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
          const SizedBox(height: 8),
          // Google login (plan 8, D15) is not wired yet, as on the web.
          OutlinedButton.icon(
            onPressed: () => showMessage(context, texts.t('auth:googleSoon')),
            icon: const Icon(Icons.g_mobiledata, size: 28),
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
