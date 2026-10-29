import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/format.dart';
import '../../core/i18n.dart';
import '../../core/theme.dart';
import '../../data/api.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';
import 'lookups.dart';
import 'ticket_list.dart';

/// Buttons in this order (TicketActions.tsx); Edit, Comment, files and watchers have their own places.
const _order = ['Approve', 'Reject', 'Resubmit', 'Triage', 'Start', 'Resolve', 'Close', 'SendBack', 'Pause', 'RequestInfo', 'Reschedule', 'Reassign', 'Decline', 'Cancel'];
const _primary = {'Approve', 'Resubmit', 'Triage', 'Start', 'Resolve', 'Close'};
const _danger = {'Reject', 'Decline', 'Cancel'};
const _withReason = {'Reject', 'Cancel', 'Decline', 'SendBack', 'RequestInfo'};

/// First guess of the due date from the priority; the lead changes it (Q21).
const _dueDays = {'Immediate': 0, 'Urgent': 2, 'Normal': 7};

/// The status buttons the API allows this user (AllowedActions), each with its dialog.
class TicketActions extends ConsumerStatefulWidget {
  const TicketActions({super.key, required this.ticket, required this.onChanged});

  final TicketDetail ticket;
  final ValueChanged<TicketDetail> onChanged;

  @override
  ConsumerState<TicketActions> createState() => _TicketActionsState();
}

class _TicketActionsState extends ConsumerState<TicketActions> {
  bool _busy = false;

  Future<void> _run(String action, [Map<String, dynamic>? body]) async {
    setState(() => _busy = true);
    final result = await guard(context, ref, () => ref.read(apiProvider).action(widget.ticket.id, action, body));
    if (!mounted) return;
    setState(() => _busy = false);
    if (result != null) {
      widget.onChanged(result);
      ref.read(ticketListsVersionProvider.notifier).bump();
      showMessage(context, ref.read(textsProvider).t('ticket:action.done'));
    }
  }

  Future<void> _press(String action) async {
    final texts = ref.read(textsProvider);
    Map<String, dynamic>? body;
    if (_withReason.contains(action)) {
      final text = await showTextDialog(context, texts, title: texts.t('ticket:action.$action'), label: texts.t('ticket:dialog.reason.$action'));
      if (text == null) return;
      body = {'Reason': text};
    } else if (action == 'Resolve') {
      final text = await showTextDialog(context, texts, title: texts.t('ticket:action.Resolve'), label: texts.t('ticket:dialog.resolution'), lines: 5);
      if (text == null) return;
      body = {'Resolution': text};
    } else if (action == 'Close') {
      final ok = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: Text(texts.t('ticket:action.Close')),
          content: Text(texts.t('ticket:action.closeConfirm')),
          actions: [
            TextButton(onPressed: () => Navigator.pop(context, false), child: Text(texts.t('common:cancel'))),
            FilledButton(onPressed: () => Navigator.pop(context, true), child: Text(texts.t('ticket:action.confirm'))),
          ],
        ),
      );
      if (ok != true) return;
    } else if (action == 'Triage') {
      if (!mounted) return;
      body = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        builder: (_) => TriageSheet(ticket: widget.ticket),
      );
      if (body == null) return;
    } else if (action == 'Reschedule') {
      if (!mounted) return;
      body = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        builder: (_) => RescheduleSheet(ticket: widget.ticket),
      );
      if (body == null) return;
    } else if (action == 'Reassign') {
      if (!mounted) return;
      body = await showModalBottomSheet<Map<String, dynamic>>(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        builder: (_) => ReassignSheet(ticket: widget.ticket),
      );
      if (body == null) return;
    }
    await _run(action, body);
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final actions = _order.where(widget.ticket.can).toList();
    if (actions.isEmpty) return const SizedBox.shrink();
    return Wrap(spacing: 8, runSpacing: 8, children: [
      for (final action in actions)
        _primary.contains(action)
            ? FilledButton(onPressed: _busy ? null : () => _press(action), child: Text(texts.t('ticket:action.$action')))
            : OutlinedButton(
                onPressed: _busy ? null : () => _press(action),
                style: _danger.contains(action)
                    ? OutlinedButton.styleFrom(foregroundColor: AppColors.errorText, side: const BorderSide(color: AppColors.error))
                    : null,
                child: Text(texts.t('ticket:action.$action')),
              ),
    ]);
  }
}

/// A required text (reason, การแก้ไข): ยืนยัน stays off until something is written.
Future<String?> showTextDialog(BuildContext context, Texts texts, {required String title, required String label, int lines = 3}) {
  final controller = TextEditingController();
  return showDialog<String>(
    context: context,
    builder: (context) => StatefulBuilder(
      builder: (context, setState) => AlertDialog(
        title: Text(title),
        content: SizedBox(
          width: 480,
          child: TextField(
            controller: controller,
            autofocus: true,
            minLines: lines,
            maxLines: lines + 4,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(labelText: '$label *', alignLabelWithHint: true),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: Text(texts.t('common:cancel'))),
          FilledButton(
            onPressed: controller.text.trim().isEmpty ? null : () => Navigator.pop(context, controller.text.trim()),
            child: Text(texts.t('ticket:action.confirm')),
          ),
        ],
      ),
    ),
  );
}

/// A bottom sheet with a title, the fields and ยกเลิก / ยืนยัน.
class _Sheet extends ConsumerWidget {
  const _Sheet({required this.title, required this.children, required this.canSubmit, required this.onSubmit});

  final String title;
  final List<Widget> children;
  final bool canSubmit;
  final VoidCallback onSubmit;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, mainAxisSize: MainAxisSize.min, children: [
          Text(title, style: AppText.sectionTitle),
          const SizedBox(height: 12),
          ...children,
          const SizedBox(height: 16),
          Row(children: [
            Expanded(child: OutlinedButton(onPressed: () => Navigator.pop(context), child: Text(texts.t('common:cancel')))),
            const SizedBox(width: 12),
            Expanded(child: FilledButton(onPressed: canSubmit ? onSubmit : null, child: Text(texts.t('ticket:action.confirm')))),
          ]),
        ]),
      ),
    );
  }
}

/// yyyy-MM-dd field that opens the date picker.
class DateField extends ConsumerWidget {
  const DateField({super.key, required this.label, required this.value, required this.onChanged, this.min, this.error = false});

  final String label;
  final String value;
  final String? min;
  final bool error;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final lang = ref.watch(langProvider);
    return InkWell(
      onTap: () async {
        final first = DateTime.parse(min ?? '2020-01-01');
        final current = DateTime.parse(value);
        final picked = await showDatePicker(
          context: context,
          initialDate: current.isBefore(first) ? first : current,
          firstDate: first,
          lastDate: DateTime(2100),
          locale: Locale(lang),
        );
        if (picked != null) onChanged(addDays(picked.toIso8601String().substring(0, 10), 0));
      },
      child: InputDecorator(
        decoration: InputDecoration(labelText: label, errorText: error ? '' : null, suffixIcon: const Icon(Icons.event)),
        child: Text(formatDate(value, lang), style: AppText.body),
      ),
    );
  }
}

/// The programmer picker of triage and reassign.
class ProgrammerField extends ConsumerWidget {
  const ProgrammerField({super.key, required this.value, required this.onChanged});

  final int? value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final programmers = ref.watch(programmersProvider).value ?? const <UserLookup>[];
    final known = programmers.any((p) => p.id == value);
    return DropdownButtonFormField<int>(
      initialValue: known ? value : null,
      isExpanded: true,
      decoration: InputDecoration(labelText: '${texts.t('ticket:field.assignee')} *'),
      items: [for (final p in programmers) DropdownMenuItem(value: p.id, child: Text(p.displayName))],
      onChanged: (v) => v == null ? null : onChanged(v),
    );
  }
}

/// จัดคิว (Q21): real priority, assignee (the system's main programmer suggested, Q3), dates, classification.
class TriageSheet extends ConsumerStatefulWidget {
  const TriageSheet({super.key, required this.ticket});

  final TicketDetail ticket;

  @override
  ConsumerState<TriageSheet> createState() => _TriageSheetState();
}

class _TriageSheetState extends ConsumerState<TriageSheet> {
  late String _priority = widget.ticket.requestedPriority;
  int? _assignee;
  late String _start = thaiToday();
  late String _due = addDays(_start, _dueDays[widget.ticket.requestedPriority] ?? 7);
  late int _productId = widget.ticket.product.id;
  late String _type = widget.ticket.type;
  late final Set<int> _models = widget.ticket.deviceModels.map((m) => m.id).toSet();
  late final _customer = TextEditingController(text: widget.ticket.customerName ?? '');

  @override
  void dispose() {
    _customer.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final products = ref.watch(productsProvider).value ?? const <Product>[];
    final models = ref.watch(deviceModelsProvider).value ?? const <DeviceModel>[];
    final owner = products.where((p) => p.id == _productId).firstOrNull?.ownerUserId;
    final assignee = _assignee ?? owner;

    return _Sheet(
      title: texts.t('ticket:action.Triage'),
      canSubmit: assignee != null && _due.compareTo(_start) >= 0,
      onSubmit: () => Navigator.pop(context, {
        'ActualPriority': _priority,
        'AssigneeId': assignee,
        'PlannedStartDate': _start,
        'PlannedDueDate': _due,
        'ProductId': _productId,
        'Type': _type,
        'DeviceModelIds': _models.toList(),
        'CustomerName': _customer.text.trim().isEmpty ? null : _customer.text.trim(),
      }),
      children: [
        Text(texts.t('ticket:dialog.triageHint'), style: AppText.small),
        const SizedBox(height: 12),
        Text('${texts.t('ticket:field.actualPriority')} · ${texts.t('ticket:dialog.requested', {'priority': texts.t('ticket:priority.${widget.ticket.requestedPriority}')})}', style: AppText.label),
        const SizedBox(height: 6),
        SegmentedButton<String>(
          segments: [for (final p in priorities) ButtonSegment(value: p, label: Text(texts.t('ticket:priority.$p'), style: AppText.caption.copyWith(color: AppColors.ink)))],
          selected: {_priority},
          showSelectedIcon: false,
          onSelectionChanged: (v) => setState(() {
            _priority = v.first;
            _due = addDays(_start, _dueDays[_priority] ?? 7);
          }),
        ),
        const SizedBox(height: 12),
        ProgrammerField(value: assignee, onChanged: (v) => setState(() => _assignee = v)),
        const SizedBox(height: 12),
        Row(children: [
          Expanded(child: DateField(label: texts.t('ticket:field.plannedStartDate'), value: _start, onChanged: (v) => setState(() => _start = v))),
          const SizedBox(width: 10),
          Expanded(
            child: DateField(
              label: texts.t('ticket:field.plannedDueDate'),
              value: _due,
              min: _start,
              error: _due.compareTo(_start) < 0,
              onChanged: (v) => setState(() => _due = v),
            ),
          ),
        ]),
        const SizedBox(height: 16),
        Text(texts.t('ticket:dialog.classification'), style: AppText.bodyStrong),
        const SizedBox(height: 8),
        DropdownButtonFormField<String>(
          initialValue: _type,
          isExpanded: true,
          decoration: InputDecoration(labelText: texts.t('ticket:field.type')),
          items: [for (final t in ticketTypes) DropdownMenuItem(value: t, child: Text(texts.t('ticket:type.$t')))],
          onChanged: (v) => setState(() => _type = v ?? _type),
        ),
        const SizedBox(height: 12),
        DropdownButtonFormField<int>(
          initialValue: products.any((p) => p.id == _productId) ? _productId : null,
          isExpanded: true,
          decoration: InputDecoration(labelText: texts.t('ticket:field.product')),
          items: [for (final p in products) DropdownMenuItem(value: p.id, child: Text(p.name))],
          onChanged: (v) => setState(() {
            _productId = v ?? _productId;
            _assignee = null;
          }),
        ),
        const SizedBox(height: 12),
        ModelChips(models: models, selected: _models, onChanged: () => setState(() {})),
        const SizedBox(height: 12),
        TextField(controller: _customer, decoration: InputDecoration(labelText: texts.t('ticket:field.customerName'))),
      ],
    );
  }
}

/// Device models as chips (few enough to pick from on a phone).
class ModelChips extends ConsumerWidget {
  const ModelChips({super.key, required this.models, required this.selected, required this.onChanged});

  final List<DeviceModel> models;
  final Set<int> selected;
  final VoidCallback onChanged;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(texts.t('ticket:field.deviceModels'), style: AppText.label),
      const SizedBox(height: 6),
      Wrap(spacing: 6, runSpacing: 6, children: [
        for (final m in models)
          FilterChip(
            label: Text(m.name, style: AppText.caption.copyWith(color: AppColors.ink)),
            selected: selected.contains(m.id),
            showCheckmark: false,
            selectedColor: AppColors.orange100,
            onSelected: (on) {
              on ? selected.add(m.id) : selected.remove(m.id);
              onChanged();
            },
          ),
      ]),
    ]);
  }
}

class RescheduleSheet extends ConsumerStatefulWidget {
  const RescheduleSheet({super.key, required this.ticket});

  final TicketDetail ticket;

  @override
  ConsumerState<RescheduleSheet> createState() => _RescheduleSheetState();
}

class _RescheduleSheetState extends ConsumerState<RescheduleSheet> {
  late String _start = widget.ticket.plannedStartDate ?? thaiToday();
  late String _due = widget.ticket.plannedDueDate ?? thaiToday();
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    return _Sheet(
      title: texts.t('ticket:action.Reschedule'),
      canSubmit: _reason.text.trim().isNotEmpty && _due.compareTo(_start) >= 0,
      onSubmit: () => Navigator.pop(context, {'PlannedStartDate': _start, 'PlannedDueDate': _due, 'Reason': _reason.text.trim()}),
      children: [
        Row(children: [
          Expanded(child: DateField(label: texts.t('ticket:field.plannedStartDate'), value: _start, onChanged: (v) => setState(() => _start = v))),
          const SizedBox(width: 10),
          Expanded(
            child: DateField(
              label: texts.t('ticket:field.plannedDueDate'),
              value: _due,
              min: _start,
              error: _due.compareTo(_start) < 0,
              onChanged: (v) => setState(() => _due = v),
            ),
          ),
        ]),
        const SizedBox(height: 12),
        TextField(
          controller: _reason,
          minLines: 2,
          maxLines: 5,
          onChanged: (_) => setState(() {}),
          decoration: InputDecoration(labelText: '${texts.t('ticket:dialog.reason.Reschedule')} *'),
        ),
      ],
    );
  }
}

class ReassignSheet extends ConsumerStatefulWidget {
  const ReassignSheet({super.key, required this.ticket});

  final TicketDetail ticket;

  @override
  ConsumerState<ReassignSheet> createState() => _ReassignSheetState();
}

class _ReassignSheetState extends ConsumerState<ReassignSheet> {
  late int? _assignee = widget.ticket.assignee?.id;

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    return _Sheet(
      title: texts.t('ticket:action.Reassign'),
      canSubmit: _assignee != null && _assignee != widget.ticket.assignee?.id,
      onSubmit: () => Navigator.pop(context, {'AssigneeId': _assignee}),
      children: [ProgrammerField(value: _assignee, onChanged: (v) => setState(() => _assignee = v))],
    );
  }
}
