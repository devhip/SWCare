import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../data/api.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';
import '../../widgets/file_picker_bar.dart';
import '../../widgets/user_search_dialog.dart';
import 'lookups.dart';
import 'ticket_actions.dart';
import 'ticket_list.dart';

final _testedTemplateProvider = FutureProvider<String>((ref) => ref.watch(apiProvider).testedDetailsTemplate());

/// สร้างงาน / แก้ไขงาน (TicketForm.tsx). New tickets take files (camera, video, gallery, files) in the same request.
class TicketFormPage extends ConsumerStatefulWidget {
  const TicketFormPage({super.key, this.editId});

  final int? editId;

  @override
  ConsumerState<TicketFormPage> createState() => _TicketFormPageState();
}

class _TicketFormPageState extends ConsumerState<TicketFormPage> {
  final _form = GlobalKey<FormState>();
  final _title = TextEditingController();
  final _version = TextEditingController();
  final _customer = TextEditingController();
  final _description = TextEditingController();
  final _tested = TextEditingController();
  String _type = 'Bug';
  int? _productId;
  String _priority = 'Normal';
  UserBrief? _requester;
  final Set<int> _models = {};
  List<UploadFile> _files = [];
  bool _ready = false;
  bool _busy = false;
  Object? _error;

  bool get _editing => widget.editId != null;

  @override
  void initState() {
    super.initState();
    _start();
  }

  Future<void> _start() async {
    try {
      if (_editing) {
        final t = await ref.read(apiProvider).ticket(widget.editId!);
        _title.text = t.title;
        _version.text = t.version ?? '';
        _customer.text = t.customerName ?? '';
        _description.text = t.description;
        _tested.text = t.testedDetails ?? '';
        _type = t.type;
        _productId = t.product.id;
        _priority = t.requestedPriority;
        _requester = t.requester;
        _models.addAll(t.deviceModels.map((m) => m.id));
      } else {
        _tested.text = await ref.read(_testedTemplateProvider.future);
      }
      if (mounted) setState(() => _ready = true);
    } catch (e) {
      if (mounted) setState(() => _error = e);
    }
  }

  @override
  void dispose() {
    for (final c in [_title, _version, _customer, _description, _tested]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    final texts = ref.read(textsProvider);
    String? orNull(TextEditingController c) => c.text.trim().isEmpty ? null : c.text.trim();
    final body = {
      'Title': _title.text.trim(),
      'Type': _type,
      'ProductId': _productId,
      'Version': orNull(_version),
      'CustomerName': orNull(_customer),
      'Description': _description.text.trim(),
      'TestedDetails': orNull(_tested),
      'RequestedPriority': _priority,
      'RequesterId': _requester?.id,
      'DeviceModelIds': _models.toList(),
    };
    setState(() => _busy = true);
    final api = ref.read(apiProvider);
    final saved = await guard(context, ref, () => _editing ? api.updateTicket(widget.editId!, body) : api.createTicket(body, _files));
    if (!mounted) return;
    setState(() => _busy = false);
    if (saved == null) return;
    ref.read(ticketListsVersionProvider.notifier).bump();
    showMessage(context, _editing ? texts.t('ticket:saved') : texts.t('ticket:created', {'no': saved.ticketNo}));
    if (_editing) {
      context.pop();
    } else {
      context.pushReplacement('/ticket/${saved.id}');
    }
  }

  Future<void> _pickRequester() async {
    final picked = await showDialog<UserLookup>(context: context, builder: (_) => UserSearchDialog(title: ref.read(textsProvider).t('ticket:field.requester')));
    if (picked != null) setState(() => _requester = UserBrief.fromJson({'Id': picked.id, 'DisplayName': picked.displayName}));
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final me = ref.watch(meProvider);
    final products = ref.watch(productsProvider).value ?? const <Product>[];
    final models = ref.watch(deviceModelsProvider).value ?? const <DeviceModel>[];
    final isSupport = me.departmentKind == 'Support';
    String? required(String? v) => (v?.trim().isEmpty ?? true) ? texts.t('validation:required') : null;

    return Scaffold(
      appBar: AppBar(flexibleSpace: const HeaderBackground(), title: Text(_editing ? texts.t('ticket:editTitle') : texts.t('ticket:newTitle'))),
      body: !_ready
          ? (_error != null ? ErrorView(_error!, onRetry: _start) : const Center(child: CircularProgressIndicator()))
          : Form(
              key: _form,
              autovalidateMode: AutovalidateMode.onUserInteraction,
              child: ListView(padding: const EdgeInsets.fromLTRB(12, 12, 12, 32), children: [
                AppCard(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    TextFormField(
                      controller: _title,
                      maxLength: 200,
                      decoration: InputDecoration(labelText: '${texts.t('ticket:field.title')} *'),
                      validator: required,
                    ),
                    const SizedBox(height: 4),
                    DropdownButtonFormField<String>(
                      initialValue: _type,
                      isExpanded: true,
                      decoration: InputDecoration(labelText: '${texts.t('ticket:field.type')} *'),
                      items: [for (final t in ticketTypes) DropdownMenuItem(value: t, child: Text(texts.t('ticket:type.$t')))],
                      onChanged: (v) => setState(() => _type = v ?? _type),
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<int>(
                      initialValue: products.any((p) => p.id == _productId) ? _productId : null,
                      isExpanded: true,
                      decoration: InputDecoration(labelText: '${texts.t('ticket:field.product')} *'),
                      items: [for (final p in products) DropdownMenuItem(value: p.id, child: Text(p.name))],
                      onChanged: (v) => setState(() => _productId = v),
                      validator: (v) => v == null ? texts.t('validation:required') : null,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(controller: _version, decoration: InputDecoration(labelText: texts.t('ticket:field.version'))),
                    const SizedBox(height: 12),
                    _CustomerField(controller: _customer),
                    const SizedBox(height: 12),
                    ModelChips(models: models, selected: _models, onChanged: () => setState(() {})),
                  ]),
                ),
                const SizedBox(height: 12),
                AppCard(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    TextFormField(
                      controller: _description,
                      minLines: 4,
                      maxLines: 12,
                      decoration: InputDecoration(labelText: '${texts.t('ticket:field.description')} *', alignLabelWithHint: true),
                      validator: required,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _tested,
                      minLines: 5,
                      maxLines: 14,
                      decoration: InputDecoration(
                        labelText: texts.t('ticket:field.testedDetails'),
                        alignLabelWithHint: true,
                        helperText: isSupport ? texts.t('ticket:hint.testedRequired') : null,
                        helperMaxLines: 3,
                      ),
                    ),
                  ]),
                ),
                const SizedBox(height: 12),
                AppCard(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                    Text(texts.t('ticket:field.requestedPriority'), style: AppText.label),
                    const SizedBox(height: 6),
                    SegmentedButton<String>(
                      segments: [for (final p in priorities) ButtonSegment(value: p, label: Text(texts.t('ticket:priority.$p'), style: AppText.caption.copyWith(color: AppColors.ink)))],
                      selected: {_priority},
                      showSelectedIcon: false,
                      onSelectionChanged: (v) => setState(() => _priority = v.first),
                    ),
                    const SizedBox(height: 12),
                    InkWell(
                      onTap: _pickRequester,
                      child: InputDecorator(
                        decoration: InputDecoration(
                          labelText: texts.t('ticket:field.requester'),
                          helperText: texts.t('ticket:hint.requester'),
                          helperMaxLines: 2,
                          suffixIcon: _requester == null
                              ? const Icon(Icons.person_search_outlined)
                              : IconButton(icon: const Icon(Icons.close), onPressed: () => setState(() => _requester = null)),
                        ),
                        child: Text(_requester?.displayName ?? '', style: AppText.body),
                      ),
                    ),
                    if (!_editing) ...[
                      const SizedBox(height: 12),
                      Text(texts.t('ticket:field.attachments'), style: AppText.label),
                      const SizedBox(height: 6),
                      FilePickerBar(files: _files, enabled: !_busy, onChanged: (v) => setState(() => _files = v)),
                    ],
                  ]),
                ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _busy ? null : _submit,
                  child: Text(_busy ? texts.t('ticket:submitting') : (_editing ? texts.t('ticket:saveChanges') : texts.t('ticket:submit'))),
                ),
              ]),
            ),
    );
  }
}

/// ลูกค้า/หน้างาน with names from earlier tickets (plan 6.1).
class _CustomerField extends ConsumerWidget {
  const _CustomerField({required this.controller});

  final TextEditingController controller;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    return RawAutocomplete<String>(
      textEditingController: controller,
      focusNode: FocusNode(),
      optionsBuilder: (value) async {
        if (value.text.trim().length < 2) return const [];
        try {
          return await ref.read(apiProvider).customerNames(value.text.trim());
        } catch (_) {
          return const [];
        }
      },
      fieldViewBuilder: (context, c, focus, _) => TextFormField(
        controller: c,
        focusNode: focus,
        decoration: InputDecoration(labelText: texts.t('ticket:field.customerName')),
      ),
      optionsViewBuilder: (context, onSelected, options) => Align(
        alignment: Alignment.topLeft,
        child: Material(
          elevation: 4,
          borderRadius: BorderRadius.circular(12),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxHeight: 220, maxWidth: 360),
            child: ListView(
              padding: EdgeInsets.zero,
              shrinkWrap: true,
              children: [for (final o in options) ListTile(dense: true, title: Text(o), onTap: () => onSelected(o))],
            ),
          ),
        ),
      ),
    );
  }
}
