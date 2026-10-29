import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../core/format.dart';
import '../core/i18n.dart';
import '../core/theme.dart';
import '../data/api.dart';
import 'common.dart';

/// Same limits as AttachmentRules in the API and lib/fileRules.ts of the web (Q20).
const maxFileBytes = 20 * 1024 * 1024;
const maxFiles = 10;
const allowedExtensions = [
  'jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp', 'heic', //
  'pdf', 'xls', 'xlsx', 'csv', 'doc', 'docx', 'txt', 'log', 'zip', 'rar', '7z', 'mp4', 'mov', 'webm',
];

/// The problem with a file, as a text of ticket:validation, or null when it may be sent.
String? fileProblem(String name, int size, Texts texts) {
  final dot = name.lastIndexOf('.');
  final ext = dot >= 0 ? name.substring(dot + 1).toLowerCase() : '';
  if (!allowedExtensions.contains(ext)) return texts.t('ticket:validation.fileType', {'name': name});
  if (size > maxFileBytes) return texts.t('ticket:validation.fileTooLarge', {'name': name});
  return null;
}

/// Camera, video, gallery and files (plan 9: ถ่ายรูป / อัดวิดีโอแนบได้), with the picked files as chips.
class FilePickerBar extends ConsumerWidget {
  const FilePickerBar({super.key, required this.files, required this.onChanged, this.enabled = true});

  final List<UploadFile> files;
  final ValueChanged<List<UploadFile>> onChanged;
  final bool enabled;

  Future<void> _add(BuildContext context, WidgetRef ref, Future<List<UploadFile>> Function() pick) async {
    final texts = ref.read(textsProvider);
    List<UploadFile> picked;
    try {
      picked = await pick();
    } catch (_) {
      if (context.mounted) showMessage(context, texts.t('files.pickFailed'), error: true);
      return;
    }
    if (picked.isEmpty || !context.mounted) return;
    final ok = <UploadFile>[];
    for (final file in picked) {
      final problem = fileProblem(file.name, file.size, texts);
      if (problem != null) {
        showMessage(context, problem, error: true);
      } else {
        ok.add(file);
      }
    }
    final all = [...files, ...ok];
    if (all.length > maxFiles) {
      showMessage(context, texts.t('ticket:validation.tooManyFiles', {'max': maxFiles}), error: true);
      return;
    }
    onChanged(all);
  }

  static Future<List<UploadFile>> _fromXFiles(List<XFile> list) async =>
      [for (final x in list) UploadFile(name: x.name, bytes: await x.readAsBytes())];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final picker = ImagePicker();
    final camera = !kIsWeb && (defaultTargetPlatform == TargetPlatform.android || defaultTargetPlatform == TargetPlatform.iOS);

    Widget button(IconData icon, String label, Future<List<UploadFile>> Function() pick) => OutlinedButton.icon(
          onPressed: enabled ? () => _add(context, ref, pick) : null,
          style: OutlinedButton.styleFrom(minimumSize: const Size(0, 36), padding: const EdgeInsets.symmetric(horizontal: 10)),
          icon: Icon(icon, size: 18),
          label: Text(label),
        );

    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Wrap(spacing: 6, runSpacing: 6, children: [
        if (camera)
          button(Icons.photo_camera_outlined, texts.t('files.camera'), () async {
            final x = await picker.pickImage(source: ImageSource.camera, imageQuality: 85, maxWidth: 2400);
            return x == null ? <UploadFile>[] : _fromXFiles([x]);
          }),
        if (camera)
          button(Icons.videocam_outlined, texts.t('files.video'), () async {
            final x = await picker.pickVideo(source: ImageSource.camera, maxDuration: const Duration(minutes: 1));
            return x == null ? <UploadFile>[] : _fromXFiles([x]);
          }),
        button(Icons.photo_library_outlined, texts.t('files.gallery'), () async => _fromXFiles(await picker.pickMultipleMedia(imageQuality: 85, maxWidth: 2400))),
        button(Icons.attach_file, texts.t('files.file'), () async {
          final picked = await FilePicker.pickFiles(type: FileType.custom, allowedExtensions: allowedExtensions);
          return [for (final f in picked) UploadFile(name: f.name, bytes: await f.readAsBytes())];
        }),
      ]),
      if (files.isNotEmpty) ...[
        const SizedBox(height: 6),
        Wrap(spacing: 6, runSpacing: 6, children: [
          for (final f in files)
            InputChip(
              label: Text('${f.name} · ${formatSize(f.size)}', style: AppText.caption),
              onDeleted: enabled ? () => onChanged([...files]..remove(f)) : null,
              deleteButtonTooltipMessage: texts.t('ticket:files.remove'),
            ),
        ]),
      ],
      const SizedBox(height: 4),
      Text(texts.t('ticket:hint.files', {'max': maxFiles}), style: AppText.caption),
    ]);
  }
}
