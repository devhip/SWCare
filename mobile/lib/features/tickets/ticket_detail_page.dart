import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

import '../../core/format.dart';
import '../../core/i18n.dart';
import '../../core/session.dart';
import '../../core/theme.dart';
import '../../data/api.dart';
import '../../models/models.dart';
import '../../widgets/common.dart';
import '../../widgets/file_picker_bar.dart';
import '../../widgets/user_search_dialog.dart';
import 'ticket_actions.dart';
import 'ticket_list.dart';

/// รายละเอียดงาน: everything of one ticket, with the buttons the API allows this user.
class TicketDetailPage extends ConsumerStatefulWidget {
  const TicketDetailPage({super.key, required this.id});

  final int id;

  @override
  ConsumerState<TicketDetailPage> createState() => _TicketDetailPageState();
}

class _TicketDetailPageState extends ConsumerState<TicketDetailPage> {
  TicketDetail? _ticket;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final ticket = await ref.read(apiProvider).ticket(widget.id);
      if (mounted) {
        setState(() {
          _ticket = ticket;
          _error = null;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _error = e);
    }
  }

  void _changed(TicketDetail ticket) {
    setState(() => _ticket = ticket);
    ref.read(ticketListsVersionProvider.notifier).bump();
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final ticket = _ticket;
    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const HeaderBackground(),
        title: Text(ticket?.ticketNo ?? texts.t('ticket:detailTitle')),
        actions: [
          if (ticket != null && ticket.can('Edit'))
            IconButton(
              tooltip: texts.t('ticket:edit'),
              icon: const Icon(Icons.edit_outlined),
              onPressed: () async {
                await context.push('/ticket/${ticket.id}/edit');
                _load();
              },
            ),
        ],
      ),
      body: ticket == null
          ? (_error != null ? ErrorView(_error!, onRetry: _load) : const Center(child: CircularProgressIndicator()))
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(padding: const EdgeInsets.fromLTRB(12, 12, 12, 32), children: [
                _Header(ticket: ticket, onChanged: _changed),
                const SizedBox(height: 12),
                _Details(ticket: ticket, onChanged: _changed),
                const SizedBox(height: 12),
                _Comments(ticket: ticket, onChanged: _changed),
                const SizedBox(height: 12),
                _Info(ticket: ticket),
                const SizedBox(height: 12),
                _People(ticket: ticket, onChanged: _changed),
                const SizedBox(height: 12),
                _Dates(ticket: ticket),
                const SizedBox(height: 12),
                _Timeline(ticket: ticket),
              ]),
            ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.children});

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) => AppCard(
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(title, style: AppText.sectionTitle),
          const SizedBox(height: 10),
          ...children,
        ]),
      );
}

/// Label above value, like the info cards of the web.
class _Field extends StatelessWidget {
  const _Field(this.label, this.value);

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(label, style: AppText.label),
        Text(value, style: AppText.body),
      ]);
}

/// Two fields per row.
class _Grid extends StatelessWidget {
  const _Grid(this.fields);

  final List<_Field> fields;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
        builder: (context, box) => Wrap(
          spacing: 12,
          runSpacing: 10,
          children: [for (final f in fields) SizedBox(width: (box.maxWidth - 12) / 2, child: f)],
        ),
      );
}

class _Header extends ConsumerWidget {
  const _Header({required this.ticket, required this.onChanged});

  final TicketDetail ticket;
  final ValueChanged<TicketDetail> onChanged;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    return AppCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(
          [ticket.ticketNo, if (ticket.legacyRef != null) '${texts.t('ticket:field.legacyRef')} ${ticket.legacyRef}'].join(' · '),
          style: AppText.label,
        ),
        const SizedBox(height: 2),
        SelectableText(ticket.title, style: AppText.pageTitle.copyWith(fontSize: 19, height: 1.4)),
        const SizedBox(height: 8),
        Wrap(spacing: 6, runSpacing: 6, children: [
          StatusChip(ticket.status),
          PriorityChip(ticket.actualPriority ?? ticket.requestedPriority),
          Pill(texts.t('ticket:type.${ticket.type}'), bg: AppColors.orange50, fg: AppColors.ink),
          if (ticket.needsLegacyReview) Pill(texts.t('ticket:legacyReview'), bg: AppColors.warningBg, fg: AppColors.warningText, strong: true),
        ]),
        if (ticket.allowedActions.isNotEmpty) ...[
          const SizedBox(height: 12),
          TicketActions(ticket: ticket, onChanged: onChanged),
        ],
      ]),
    );
  }
}

class _Details extends ConsumerStatefulWidget {
  const _Details({required this.ticket, required this.onChanged});

  final TicketDetail ticket;
  final ValueChanged<TicketDetail> onChanged;

  @override
  ConsumerState<_Details> createState() => _DetailsState();
}

class _DetailsState extends ConsumerState<_Details> {
  List<UploadFile> _files = [];
  bool _uploading = false;

  Future<void> _upload() async {
    setState(() => _uploading = true);
    final result = await guard(context, ref, () => ref.read(apiProvider).addFiles(widget.ticket.id, _files));
    if (!mounted) return;
    setState(() => _uploading = false);
    if (result != null) {
      setState(() => _files = []);
      widget.onChanged(result);
      showMessage(context, ref.read(textsProvider).t('ticket:files.uploaded'));
    }
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final t = widget.ticket;
    Widget block(String label, String text) => Padding(
          padding: const EdgeInsets.only(bottom: 12),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(label, style: AppText.label),
            SelectableText(text, style: AppText.body),
          ]),
        );
    return AppCard(
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        block(texts.t('ticket:field.description'), t.description),
        if (t.testedDetails?.isNotEmpty ?? false) block(texts.t('ticket:field.testedDetails'), t.testedDetails!),
        if (t.resolution?.isNotEmpty ?? false) block(texts.t('ticket:field.resolution'), t.resolution!),
        Text(texts.t('ticket:field.attachments'), style: AppText.label),
        AttachmentList(ticketId: t.id, attachments: t.attachments, showEmpty: true),
        if (t.can('AddAttachment')) ...[
          const SizedBox(height: 8),
          FilePickerBar(files: _files, enabled: !_uploading, onChanged: (v) => setState(() => _files = v)),
          if (_files.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: FilledButton(onPressed: _uploading ? null : _upload, child: Text(texts.t('ticket:files.add'))),
            ),
        ],
      ]),
    );
  }
}

/// Files go through the API with the token: downloaded to the phone, then opened in the matching app.
class AttachmentList extends ConsumerWidget {
  const AttachmentList({super.key, required this.ticketId, required this.attachments, this.showEmpty = false});

  final int ticketId;
  final List<Attachment> attachments;
  final bool showEmpty;

  Future<void> _open(BuildContext context, WidgetRef ref, Attachment a) async {
    final texts = ref.read(textsProvider);
    if (kIsWeb) {
      showMessage(context, texts.t('files.openOnPhone'));
      return;
    }
    showMessage(context, texts.t('files.downloading', {'name': a.fileName}));
    final bytes = await guard(context, ref, () => ref.read(apiProvider).attachmentBytes(ticketId, a.id));
    if (bytes == null) return;
    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/swcare_${a.id}_${a.fileName}');
    await file.writeAsBytes(bytes, flush: true);
    final result = await OpenFilex.open(file.path);
    if (result.type != ResultType.done && context.mounted) showMessage(context, texts.t('files.noApp'), error: true);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    if (attachments.isEmpty) return showEmpty ? Text(texts.t('ticket:files.none'), style: AppText.small) : const SizedBox.shrink();
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      for (final a in attachments)
        InkWell(
          onTap: () => _open(context, ref, a),
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 4),
            child: Row(children: [
              Icon(_icon(a.contentType), size: 18, color: AppColors.orange600),
              const SizedBox(width: 6),
              Flexible(child: Text(a.fileName, style: AppText.body.copyWith(color: AppColors.orange600), overflow: TextOverflow.ellipsis)),
              const SizedBox(width: 6),
              Text(formatSize(a.sizeBytes), style: AppText.caption),
            ]),
          ),
        ),
    ]);
  }

  static IconData _icon(String type) => type.startsWith('image/')
      ? Icons.image_outlined
      : type.startsWith('video/')
          ? Icons.movie_outlined
          : Icons.insert_drive_file_outlined;
}

class _Comments extends ConsumerStatefulWidget {
  const _Comments({required this.ticket, required this.onChanged});

  final TicketDetail ticket;
  final ValueChanged<TicketDetail> onChanged;

  @override
  ConsumerState<_Comments> createState() => _CommentsState();
}

class _CommentsState extends ConsumerState<_Comments> {
  final _body = TextEditingController();
  List<UploadFile> _files = [];
  bool _sending = false;

  @override
  void dispose() {
    _body.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    setState(() => _sending = true);
    final result = await guard(context, ref, () => ref.read(apiProvider).addComment(widget.ticket.id, _body.text.trim(), _files));
    if (!mounted) return;
    setState(() => _sending = false);
    if (result != null) {
      _body.clear();
      setState(() => _files = []);
      widget.onChanged(result);
    }
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final me = ref.watch(meProvider);
    final t = widget.ticket;
    return _Section(title: texts.t('ticket:section.comments'), children: [
      if (t.comments.isEmpty) Text(texts.t('ticket:comment.empty'), style: AppText.small),
      for (final c in t.comments)
        Padding(
          padding: const EdgeInsets.only(bottom: 12),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('${c.author.displayName} · ${formatDateTime(c.createdAt, texts.lang)}', style: AppText.label),
            if (c.body.isNotEmpty) SelectableText(c.body, style: AppText.body),
            AttachmentList(ticketId: t.id, attachments: c.attachments),
          ]),
        ),
      if (t.can('Comment')) ...[
        if (t.status == 'WaitingInfo' && t.reporter.id == me.userId)
          Container(
            margin: const EdgeInsets.only(bottom: 8),
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(color: AppColors.infoBg, borderRadius: BorderRadius.circular(10)),
            child: Text(texts.t('ticket:comment.answerHint'), style: AppText.small.copyWith(color: AppColors.infoText)),
          ),
        TextField(
          controller: _body,
          minLines: 2,
          maxLines: 6,
          enabled: !_sending,
          onChanged: (_) => setState(() {}),
          decoration: InputDecoration(hintText: texts.t('ticket:comment.placeholder')),
        ),
        const SizedBox(height: 8),
        FilePickerBar(files: _files, enabled: !_sending, onChanged: (v) => setState(() => _files = v)),
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerRight,
          child: FilledButton.icon(
            onPressed: _sending || (_body.text.trim().isEmpty && _files.isEmpty) ? null : _send,
            icon: const Icon(Icons.send, size: 18),
            label: Text(_sending ? texts.t('ticket:comment.sending') : texts.t('ticket:comment.send')),
          ),
        ),
      ],
    ]);
  }
}

class _Info extends ConsumerWidget {
  const _Info({required this.ticket});

  final TicketDetail ticket;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final t = ticket;
    String or(String? v) => (v?.isNotEmpty ?? false) ? v! : texts.t('ticket:none');
    return _Section(title: texts.t('ticket:section.info'), children: [
      _Grid([
        _Field(texts.t('ticket:field.product'), t.product.name),
        _Field(texts.t('ticket:field.version'), or(t.version)),
        _Field(texts.t('ticket:field.customerName'), or(t.customerName)),
        _Field(texts.t('ticket:field.deviceModels'), or(t.deviceModels.map((m) => m.name).join(', '))),
        _Field(texts.t('ticket:field.requestedPriority'), texts.t('ticket:priority.${t.requestedPriority}')),
        _Field(texts.t('ticket:field.actualPriority'), t.actualPriority == null ? texts.t('ticket:none') : texts.t('ticket:priority.${t.actualPriority}')),
      ]),
    ]);
  }
}

class _People extends ConsumerStatefulWidget {
  const _People({required this.ticket, required this.onChanged});

  final TicketDetail ticket;
  final ValueChanged<TicketDetail> onChanged;

  @override
  ConsumerState<_People> createState() => _PeopleState();
}

class _PeopleState extends ConsumerState<_People> {
  bool _busy = false;

  Future<void> _change(Future<TicketDetail> Function() call) async {
    setState(() => _busy = true);
    final result = await guard(context, ref, call);
    if (!mounted) return;
    setState(() => _busy = false);
    if (result != null) widget.onChanged(result);
  }

  Future<void> _add() async {
    final picked = await showDialog<UserLookup>(context: context, builder: (_) => UserSearchDialog(title: ref.read(textsProvider).t('ticket:watchers.add')));
    if (picked != null) await _change(() => ref.read(apiProvider).addWatcher(widget.ticket.id, picked.id));
  }

  @override
  Widget build(BuildContext context) {
    final texts = ref.watch(textsProvider);
    final t = widget.ticket;
    final api = ref.read(apiProvider);
    String person(UserBrief? user, String? legacy) => user?.displayName ?? legacy ?? texts.t('ticket:none');
    final canManage = t.can('ManageWatchers');

    return _Section(title: texts.t('ticket:section.people'), children: [
      _Grid([
        _Field(texts.t('ticket:field.reporter'), person(t.reporter, t.legacyReporterName)),
        _Field(texts.t('ticket:field.department'), t.department.name),
        _Field(texts.t('ticket:field.requester'), person(t.requester, t.legacyRequesterName)),
        _Field(texts.t('ticket:field.approver'), person(t.approver, t.legacyApproverName)),
        _Field(texts.t('ticket:field.assignee'), person(t.assignee, t.legacyAssigneeName)),
      ]),
      const SizedBox(height: 12),
      Text(texts.t('ticket:field.watchers'), style: AppText.label),
      const SizedBox(height: 4),
      if (t.watchers.isEmpty) Text(texts.t('ticket:watchers.none'), style: AppText.small),
      Wrap(spacing: 6, runSpacing: 6, children: [
        for (final w in t.watchers)
          InputChip(
            label: Text(w.displayName, style: AppText.caption.copyWith(color: AppColors.ink)),
            onDeleted: canManage && !_busy ? () => _change(() => api.removeWatcher(t.id, w.id)) : null,
            deleteButtonTooltipMessage: texts.t('ticket:watchers.remove'),
          ),
      ]),
      if (canManage)
        TextButton.icon(onPressed: _busy ? null : _add, icon: const Icon(Icons.person_add_alt, size: 18), label: Text(texts.t('ticket:watchers.add'))),
      if (t.can('Follow')) ...[
        const SizedBox(height: 4),
        OutlinedButton.icon(
          onPressed: _busy ? null : () => _change(() => api.follow(t.id)),
          icon: const Icon(Icons.notifications_active_outlined, size: 18),
          label: Text(texts.t('ticket:watchers.follow')),
        ),
        Text(texts.t('ticket:watchers.followHint'), style: AppText.caption),
      ],
      if (t.can('Unfollow'))
        TextButton.icon(
          onPressed: _busy ? null : () => _change(() => api.unfollow(t.id)),
          icon: const Icon(Icons.notifications_off_outlined, size: 18),
          label: Text(texts.t('ticket:watchers.unfollow')),
        ),
    ]);
  }
}

class _Dates extends ConsumerWidget {
  const _Dates({required this.ticket});

  final TicketDetail ticket;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final t = ticket;
    final lang = texts.lang;
    return _Section(title: texts.t('ticket:section.dates'), children: [
      _Grid([
        _Field(texts.t('ticket:field.reportedAt'), formatDateTime(t.reportedAt, lang)),
        _Field(texts.t('ticket:field.approvedAt'), formatDateTime(t.approvedAt, lang)),
        _Field(texts.t('ticket:field.plannedStartDate'), formatDate(t.plannedStartDate, lang)),
        _Field(texts.t('ticket:field.plannedDueDate'), formatDate(t.plannedDueDate, lang)),
        _Field(texts.t('ticket:field.startedAt'), formatDateTime(t.startedAt, lang)),
        _Field(texts.t('ticket:field.resolvedAt'), formatDateTime(t.resolvedAt, lang)),
        _Field(texts.t('ticket:field.closedAt'), formatDateTime(t.closedAt, lang)),
      ]),
    ]);
  }
}

/// Every status change and edit, oldest first, with the line under each one like Timeline.tsx.
class _Timeline extends ConsumerWidget {
  const _Timeline({required this.ticket});

  final TicketDetail ticket;

  static String? _detail(Activity a, Texts texts) {
    Map<String, dynamic> d = const {};
    try {
      if (a.data != null) d = jsonDecode(a.data!) as Map<String, dynamic>;
    } on FormatException {
      return null;
    }
    final lang = texts.lang;
    switch (a.type) {
      case 'Edited':
      case 'ClassificationChanged':
        final changes = (d['Changes'] as List<dynamic>? ?? const []).map((c) => texts.t('ticket:changeField.${(c as Map<String, dynamic>)['Field']}'));
        return changes.isEmpty ? null : texts.t('ticket:activity.changed', {'fields': changes.join(', ')});
      case 'Triaged':
        return [
          if (d['ActualPriority'] != null) texts.t('ticket:priority.${d['ActualPriority']}'),
          if (d['Assignee'] != null) '${texts.t('ticket:field.assignee')} ${d['Assignee']}',
          if (d['PlannedDueDate'] != null) '${texts.t('ticket:field.plannedDueDate')} ${formatDate(d['PlannedDueDate'] as String, lang)}',
        ].join(' · ');
      case 'Rescheduled':
        return '${texts.t('ticket:field.plannedDueDate')} ${formatDate(d['OldDueDate'] as String?, lang)} → ${formatDate(d['NewDueDate'] as String?, lang)}';
      case 'Reassigned':
        return '${d['Old'] ?? '-'} → ${d['New'] ?? '-'}';
      case 'WatcherAdded':
      case 'WatcherRemoved':
        return d['User'] as String?;
      default:
        return null;
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final list = ticket.activities;
    return _Section(title: texts.t('ticket:section.timeline'), children: [
      for (var i = 0; i < list.length; i++)
        IntrinsicHeight(
          child: Row(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Column(children: [
              Container(
                margin: const EdgeInsets.only(top: 6),
                width: 10,
                height: 10,
                decoration: const BoxDecoration(color: AppColors.orange500, shape: BoxShape.circle),
              ),
              if (i < list.length - 1) Expanded(child: Container(width: 2, margin: const EdgeInsets.symmetric(vertical: 3), color: AppColors.lineSoft)),
            ]),
            const SizedBox(width: 10),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.only(bottom: 14),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(texts.t('ticket:activity.${list[i].type}'), style: AppText.bodyStrong),
                  if (_detail(list[i], texts) case final line? when line.isNotEmpty) Text(line, style: AppText.small),
                  if (list[i].note?.isNotEmpty ?? false) Text(list[i].note!, style: AppText.small.copyWith(color: AppColors.ink)),
                  Text(
                    '${list[i].actor?.displayName ?? texts.t('ticket:activity.system')} · ${formatDateTime(list[i].createdAt, texts.lang)}',
                    style: AppText.caption,
                  ),
                ]),
              ),
            ),
          ]),
        ),
    ]);
  }
}
