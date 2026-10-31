import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/api_client.dart';
import '../core/format.dart';
import '../core/i18n.dart';
import '../core/theme.dart';
import '../models/models.dart';

/// Same colours as StatusChip.tsx of the web.
const _statusColors = <String, (Color, Color)>{
  'PendingApproval': (AppColors.warningBg, AppColors.warningText),
  'Rejected': (AppColors.errorBg, AppColors.errorText),
  'PendingTriage': (AppColors.warningBg, AppColors.warningText),
  'WaitingInfo': (AppColors.violetBg, AppColors.violet),
  'Queued': (AppColors.infoBg, AppColors.infoText),
  'InProgress': (AppColors.blueBg, AppColors.blue),
  'Resolved': (AppColors.tealBg, AppColors.teal),
  'Closed': (AppColors.successBg, AppColors.successText),
  'Cancelled': (AppColors.orange50, AppColors.muted),
  'Declined': (AppColors.orange50, AppColors.muted),
};

class Pill extends StatelessWidget {
  const Pill(this.text, {super.key, required this.bg, required this.fg, this.strong = false});

  final String text;
  final Color bg;
  final Color fg;
  final bool strong;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 2),
        decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(99)),
        child: Text(text, style: AppText.caption.copyWith(color: fg, fontWeight: strong ? FontWeight.w600 : FontWeight.w500)),
      );
}

class StatusChip extends ConsumerWidget {
  const StatusChip(this.status, {super.key});

  final String status;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final (bg, fg) = _statusColors[status] ?? (AppColors.orange50, AppColors.muted);
    return Pill(ref.watch(textsProvider).t('ticket:status.$status'), bg: bg, fg: fg, strong: true);
  }
}

class PriorityChip extends ConsumerWidget {
  const PriorityChip(this.priority, {super.key});

  final String priority;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final (bg, fg) = switch (priority) {
      'Immediate' => (AppColors.errorBg, AppColors.errorText),
      'Urgent' => (AppColors.warningBg, AppColors.warningText),
      _ => (AppColors.orange50, AppColors.muted),
    };
    return Pill(ref.watch(textsProvider).t('ticket:priority.$priority'), bg: bg, fg: fg);
  }
}

/// The header of the web (AppShell.tsx): the orange gradient with a soft white circle at the top right.
/// `AppBar(flexibleSpace: const HeaderBackground())` on every page.
class HeaderBackground extends StatelessWidget {
  const HeaderBackground({super.key, this.circle = 240});

  final double circle;

  @override
  Widget build(BuildContext context) => DecoratedBox(
        decoration: const BoxDecoration(gradient: headerGradient),
        child: Stack(children: [
          Positioned(
            right: -circle * .27,
            top: -circle / 2,
            child: Container(width: circle, height: circle, decoration: const BoxDecoration(shape: BoxShape.circle, color: Color(0x1AFFFFFF))),
          ),
        ]),
      );
}

/// The "SW" mark of the web's Brand: white on the header, orange on a light page.
class BrandMark extends StatelessWidget {
  const BrandMark({super.key, this.size = 40, this.onHeader = false});

  final double size;
  final bool onHeader;

  @override
  Widget build(BuildContext context) => Container(
        width: size,
        height: size,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          color: onHeader ? const Color(0x38FFFFFF) : AppColors.orange100,
          borderRadius: BorderRadius.circular(size * .33),
          border: onHeader ? Border.all(color: const Color(0x59FFFFFF), width: 2) : null,
        ),
        child: Text('SW', style: TextStyle(fontSize: size * .37, fontWeight: FontWeight.w700, letterSpacing: .5, color: onHeader ? Colors.white : AppColors.orange600)),
      );
}

/// White rounded card of the HIPHR look.
class AppCard extends StatelessWidget {
  const AppCard({super.key, required this.child, this.padding = const EdgeInsets.all(16), this.onTap});

  final Widget child;
  final EdgeInsets padding;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => Container(
        decoration: cardDecoration,
        child: Material(
          color: Colors.transparent,
          borderRadius: BorderRadius.circular(18),
          clipBehavior: Clip.antiAlias,
          child: InkWell(onTap: onTap, child: Padding(padding: padding, child: child)),
        ),
      );
}

/// One ticket in a list: number, status, title, system · customer · reporter, priority, assignee, due date.
class TicketCard extends ConsumerWidget {
  const TicketCard(this.item, {super.key, this.onTap});

  final TicketListItem item;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    final meta = [item.productName, if (item.customerName?.isNotEmpty ?? false) item.customerName!, texts.t('reporterShort', {'name': item.reporter.displayName})];
    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(children: [
            Text(item.ticketNo, style: AppText.label),
            const Spacer(),
            StatusChip(item.status),
          ]),
          const SizedBox(height: 4),
          Text(item.title, style: AppText.bodyStrong, maxLines: 2, overflow: TextOverflow.ellipsis),
          const SizedBox(height: 2),
          Text(meta.join(' · '), style: AppText.small, maxLines: 1, overflow: TextOverflow.ellipsis),
          const SizedBox(height: 8),
          Wrap(spacing: 8, runSpacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
            PriorityChip(item.priority),
            if (item.assignee != null) Text(texts.t('assigneeShort', {'name': item.assignee!.displayName}), style: AppText.caption),
            if (item.plannedDueDate != null)
              Text(
                texts.t('dueShort', {'date': formatDate(item.plannedDueDate, texts.lang)}),
                style: AppText.caption.copyWith(color: item.isOverdue ? AppColors.errorText : null, fontWeight: item.isOverdue ? FontWeight.w600 : null),
              ),
            if (item.isOverdue) Pill(texts.t('ticket:list.overdue'), bg: AppColors.errorBg, fg: AppColors.errorText),
          ]),
        ],
      ),
    );
  }
}

class EmptyView extends StatelessWidget {
  const EmptyView(this.text, {super.key, this.icon = Icons.inbox_outlined});

  final String text;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Icon(icon, size: 48, color: AppColors.mutedLight),
            const SizedBox(height: 8),
            Text(text, style: AppText.small, textAlign: TextAlign.center),
          ]),
        ),
      );
}

class ErrorView extends ConsumerWidget {
  const ErrorView(this.error, {super.key, this.onRetry});

  final Object error;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final texts = ref.watch(textsProvider);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          const Icon(Icons.cloud_off_outlined, size: 48, color: AppColors.mutedLight),
          const SizedBox(height: 8),
          Text(errorText(error, texts), style: AppText.small, textAlign: TextAlign.center),
          if (onRetry != null) ...[
            const SizedBox(height: 12),
            OutlinedButton(onPressed: onRetry, child: Text(texts.t('retry'))),
          ],
        ]),
      ),
    );
  }
}

String errorText(Object error, Texts texts) => error is ApiException ? error.text(texts) : texts.t('errors:requestFailed');

void showMessage(BuildContext context, String text, {bool error = false}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(
      content: Text(text),
      backgroundColor: error ? AppColors.errorText : AppColors.ink,
    ));
}

/// Runs an API call and shows its error as a snack bar; returns null when it failed.
Future<T?> guard<T>(BuildContext context, WidgetRef ref, Future<T> Function() call) async {
  try {
    return await call();
  } catch (e) {
    if (context.mounted) showMessage(context, errorText(e, ref.read(textsProvider)), error: true);
    return null;
  }
}
