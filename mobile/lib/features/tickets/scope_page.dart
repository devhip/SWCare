import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n.dart';
import '../../core/theme.dart';
import 'ticket_list.dart';

/// รออนุมัติ (department heads) and รอจัดคิว (programmer lead), like ScopePage.tsx of the web.
class ScopePage extends ConsumerWidget {
  const ScopePage({super.key, required this.scope, required this.titleKey, required this.hintKey});

  final String scope;
  final String titleKey;
  final String hintKey;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    return Scaffold(
      appBar: AppBar(title: Text(texts.t(titleKey))),
      body: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Padding(padding: const EdgeInsets.fromLTRB(16, 12, 16, 0), child: Text(texts.t(hintKey), style: AppText.small)),
        Expanded(child: TicketListView(scope: scope)),
      ]),
    );
  }
}
