import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api_client.dart';
import '../../core/format.dart';
import '../../core/providers.dart';
import '../../core/theme.dart';
import 'order_models.dart';

/// Pill showing an order status with a small dot, coloured by [OrderStatus.tone].
class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    final tone = OrderStatus.tone(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: tone.background, borderRadius: BorderRadius.circular(999)),
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        Container(width: 6, height: 6, decoration: BoxDecoration(color: tone.foreground, shape: BoxShape.circle)),
        const SizedBox(width: 6),
        Text(
          OrderStatus.label(status),
          style: TextStyle(color: tone.foreground, fontSize: 12, fontWeight: FontWeight.w600, height: 1.2),
        ),
      ]),
    );
  }
}

IconData statusIcon(String status) => switch (status) {
      OrderStatus.paid || OrderStatus.confirmed => Icons.inventory_2_outlined,
      OrderStatus.processing => Icons.autorenew,
      OrderStatus.shipped => Icons.local_shipping_outlined,
      OrderStatus.delivered => Icons.check_circle_outline,
      OrderStatus.cancelled || OrderStatus.paymentFailed => Icons.cancel_outlined,
      _ => Icons.receipt_long_outlined,
    };

/// One order in the list: status icon, order id (without the ORD- prefix), date, total and status chip.
class OrderCard extends StatelessWidget {
  const OrderCard({super.key, required this.order, required this.onTap});
  final OrderSummary order;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final tone = OrderStatus.tone(order.status);
    final muted = theme.colorScheme.onSurfaceVariant;

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(Gap.s4),
          child: Row(children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(color: tone.background, shape: BoxShape.circle),
              child: Icon(statusIcon(order.status), color: tone.foreground, size: 22),
            ),
            const SizedBox(width: Gap.s3),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(shortOrderId(order.orderNumber), style: theme.textTheme.titleMedium, maxLines: 1, overflow: TextOverflow.ellipsis),
                const SizedBox(height: 2),
                Text(formatDateTime(order.createdAtUtc), style: theme.textTheme.bodySmall?.copyWith(color: muted), maxLines: 1, overflow: TextOverflow.ellipsis),
              ]),
            ),
            const SizedBox(width: Gap.s3),
            Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
              Text(formatMoney(order.grandTotal, order.currency), style: theme.textTheme.titleMedium),
              const SizedBox(height: 6),
              StatusChip(status: order.status),
            ]),
          ]),
        ),
      ),
    );
  }
}

/// Grey placeholder shaped like an [OrderCard], shown while the first page loads.
class OrderCardSkeleton extends StatelessWidget {
  const OrderCardSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    final base = Theme.of(context).colorScheme.outlineVariant;
    Widget bar(double w, double h) => Container(
          width: w,
          height: h,
          decoration: BoxDecoration(color: base, borderRadius: BorderRadius.circular(6)),
        );
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(Gap.s4),
        child: Row(children: [
          Container(width: 44, height: 44, decoration: BoxDecoration(color: base, shape: BoxShape.circle)),
          const SizedBox(width: Gap.s3),
          Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [bar(130, 14), const SizedBox(height: 8), bar(90, 11)])),
          Column(crossAxisAlignment: CrossAxisAlignment.end, children: [bar(70, 14), const SizedBox(height: 8), bar(56, 18)]),
        ]),
      ),
    );
  }
}

/// Centered message used for empty and error states.
/// A failed load, with enough to tell what is going on: the server's own message, which server the app is talking to,
/// and which retry this is (number and time), so pressing "Reintentar" always visibly does something.
class LoadError extends ConsumerWidget {
  const LoadError({super.key, required this.title, required this.error, required this.onRetry});
  final String title;
  final Object error;
  final VoidCallback onRetry;

  static String _clock(DateTime t) =>
      '${t.hour.toString().padLeft(2, '0')}:${t.minute.toString().padLeft(2, '0')}:${t.second.toString().padLeft(2, '0')}';

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final info = ref.watch(retryInfoProvider);
    final status = error is ApiException ? (error as ApiException).statusCode : null;
    final lines = <String>[
      error.toString(),
      if (status != null && status >= 500) 'Es un error del servidor, no de la app: revisa que su base de datos tenga las migraciones al día.',
      'Servidor: ${ref.watch(apiUrlProvider)}',
      if (info != null) 'Intento ${info.attempt} · ${_clock(info.at)}',
    ];
    return StateMessage(
      icon: Icons.cloud_off_outlined,
      title: title,
      message: lines.join('\n'),
      action: FilledButton.icon(
        onPressed: () {
          ref.read(retryInfoProvider.notifier).mark();
          onRetry();
        },
        icon: const Icon(Icons.refresh),
        label: const Text('Reintentar'),
      ),
    );
  }
}

class StateMessage extends StatelessWidget {
  const StateMessage({super.key, required this.icon, required this.title, this.message, this.action});
  final IconData icon;
  final String title;
  final String? message;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    // Scrollable so a long message (e.g. a server error with its explanation) never overflows a small area.
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(Gap.s6),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Icon(icon, size: 48, color: theme.colorScheme.onSurfaceVariant),
          const SizedBox(height: Gap.s3),
          Text(title, style: theme.textTheme.titleMedium, textAlign: TextAlign.center),
          if (message != null) ...[
            const SizedBox(height: Gap.s1),
            Text(message!, style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant), textAlign: TextAlign.center),
          ],
          if (action != null) ...[const SizedBox(height: Gap.s4), action!],
        ]),
      ),
    );
  }
}
