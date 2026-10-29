import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../widgets/common.dart';
import 'auth_card.dart';

const _minLength = 8;
const _maxLength = 64;

/// Forced after the first login (and after the Admin sets a new password), or opened from the menu.
class ChangePasswordPage extends ConsumerStatefulWidget {
  const ChangePasswordPage({super.key});

  @override
  ConsumerState<ChangePasswordPage> createState() => _ChangePasswordPageState();
}

class _ChangePasswordPageState extends ConsumerState<ChangePasswordPage> {
  final _form = GlobalKey<FormState>();
  final _current = TextEditingController();
  final _next = TextEditingController();
  final _confirm = TextEditingController();
  bool _busy = false;

  @override
  void dispose() {
    _current.dispose();
    _next.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    final forced = ref.read(sessionProvider).value?.mustChangePassword ?? false;
    setState(() => _busy = true);
    final ok = await guard(context, ref, () async {
      await ref.read(sessionProvider.notifier).changePassword(_current.text, _next.text);
      return true;
    });
    if (!mounted) return;
    setState(() => _busy = false);
    if (ok == true) {
      showMessage(context, ref.read(textsProvider).t('auth:passwordChanged'));
      if (!forced && context.canPop()) context.pop();
    }
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final me = ref.watch(sessionProvider).value;
    final forced = me?.mustChangePassword ?? false;
    String? rule(String? v) {
      if (v == null || v.isEmpty) return texts.t('validation:required');
      if (v.length < _minLength || v.length > _maxLength || v.toLowerCase() == me?.username.toLowerCase()) {
        return texts.t('auth:passwordRule', {'min': _minLength});
      }
      return null;
    }

    return AuthCard(
      title: texts.t('auth:changePasswordTitle'),
      subtitle: forced ? texts.t('auth:changePasswordForced') : '',
      child: Form(
        key: _form,
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          TextFormField(
            controller: _current,
            obscureText: true,
            decoration: InputDecoration(labelText: texts.t('auth:currentPassword')),
            validator: (v) => (v?.isEmpty ?? true) ? texts.t('validation:required') : null,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _next,
            obscureText: true,
            decoration: InputDecoration(labelText: texts.t('auth:newPassword'), helperText: texts.t('auth:passwordRule', {'min': _minLength})),
            validator: rule,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _confirm,
            obscureText: true,
            decoration: InputDecoration(labelText: texts.t('auth:confirmPassword')),
            validator: (v) => v != _next.text ? texts.t('passwordMismatch') : null,
          ),
          const SizedBox(height: 16),
          FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? texts.t('auth:saving') : texts.t('auth:saveNewPassword'))),
          if (forced) ...[
            const SizedBox(height: 8),
            TextButton(onPressed: () => ref.read(sessionProvider.notifier).logout(), child: Text(texts.t('common:logout'))),
          ],
        ]),
      ),
    );
  }
}
