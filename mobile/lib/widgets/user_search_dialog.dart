import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/i18n.dart';
import '../core/theme.dart';
import '../data/api.dart';
import '../models/models.dart';

/// Search people by name (ผู้ร้องขอ, เพิ่มผู้ติดตาม); returns the one tapped.
class UserSearchDialog extends ConsumerStatefulWidget {
  const UserSearchDialog({super.key, required this.title});

  final String title;

  @override
  ConsumerState<UserSearchDialog> createState() => _UserSearchDialogState();
}

class _UserSearchDialogState extends ConsumerState<UserSearchDialog> {
  List<UserLookup> _users = [];
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    _search('');
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  void _search(String text) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () async {
      try {
        final users = await ref.read(apiProvider).userLookup(text.trim());
        if (mounted) setState(() => _users = users);
      } catch (_) {
        // Keep the last results.
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    return AlertDialog(
      title: Text(widget.title),
      contentPadding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
      content: SizedBox(
        width: 400,
        height: 360,
        child: Column(children: [
          TextField(autofocus: true, onChanged: _search, decoration: InputDecoration(prefixIcon: const Icon(Icons.search), hintText: texts.t('searchName'))),
          const SizedBox(height: 8),
          Expanded(
            child: ListView(children: [
              for (final u in _users)
                ListTile(
                  dense: true,
                  title: Text(u.displayName, style: AppText.body),
                  subtitle: Text(u.departmentName, style: AppText.caption),
                  onTap: () => Navigator.pop(context, u),
                ),
            ]),
          ),
        ]),
      ),
      actions: [TextButton(onPressed: () => Navigator.pop(context), child: Text(texts.t('common:cancel')))],
    );
  }
}
