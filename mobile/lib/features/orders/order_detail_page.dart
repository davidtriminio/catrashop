import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/format.dart';
import '../../core/providers.dart';
import '../../core/theme.dart';
import 'order_models.dart';
import 'receipt_page.dart';
import 'widgets.dart';

class OrderDetailPage extends ConsumerStatefulWidget {
  const OrderDetailPage({super.key, required this.orderId});
  final String orderId;

  @override
  ConsumerState<OrderDetailPage> createState() => _OrderDetailPageState();
}

class _OrderDetailPageState extends ConsumerState<OrderDetailPage> {
  bool _busy = false;

  /// Target status of an action, used to describe the change in the confirmation sheet.
  static String _target(OrderAction a) => switch (a) {
        OrderAction.process => OrderStatus.processing,
        OrderAction.ship => OrderStatus.shipped,
        OrderAction.deliver => OrderStatus.delivered,
        OrderAction.cancel => OrderStatus.cancelled,
      };

  Future<void> _run(OrderDetail o, OrderAction action) async {
    final reason = await showModalBottomSheet<String?>(
      context: context,
      isScrollControlled: true,
      builder: (_) => _ConfirmSheet(order: o, action: action, target: _target(action)),
    );
    if (reason == null) return; // dismissed or "Volver"

    setState(() => _busy = true);
    try {
      await ref.read(ordersRepositoryProvider).perform(o.id, action, reason: action == OrderAction.cancel ? reason : null);
      ref.invalidate(orderDetailProvider(o.id));
      ref.invalidate(ordersProvider);
      ref.invalidate(summaryProvider);
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Estado actualizado.')));
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final detail = ref.watch(orderDetailProvider(widget.orderId));
    final order = detail.value;

    return Scaffold(
      appBar: AppBar(title: Text(order?.orderNumber ?? 'Pedido')),
      body: detail.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => detail.isLoading ? const Center(child: CircularProgressIndicator()) : LoadError(
          title: 'No se pudo cargar el pedido',
          error: e,
          onRetry: () => ref.invalidate(orderDetailProvider(widget.orderId)),
        ),
        data: (o) => _Body(order: o),
      ),
      bottomNavigationBar: order == null ? null : _ActionBar(order: order, busy: _busy, onAction: (a) => _run(order, a)),
    );
  }
}

class _ActionBar extends StatelessWidget {
  const _ActionBar({required this.order, required this.busy, required this.onAction});
  final OrderDetail order;
  final bool busy;
  final void Function(OrderAction) onAction;

  @override
  Widget build(BuildContext context) {
    final next = OrderAction.nextFor(order.status);
    final canCancel = OrderAction.canCancel(order.status);
    if (next == null && !canCancel) return const SizedBox.shrink();
    final scheme = Theme.of(context).colorScheme;

    return Container(
      decoration: BoxDecoration(color: scheme.surface, border: Border(top: BorderSide(color: scheme.outlineVariant))),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(Gap.s4, Gap.s3, Gap.s4, Gap.s3),
          child: Row(children: [
            if (canCancel)
              Expanded(
                flex: 2,
                child: OutlinedButton(
                  style: OutlinedButton.styleFrom(foregroundColor: scheme.error, side: BorderSide(color: scheme.error.withValues(alpha: 0.5))),
                  onPressed: busy ? null : () => onAction(OrderAction.cancel),
                  child: const Text('Cancelar'),
                ),
              ),
            if (next != null && canCancel) const SizedBox(width: Gap.s3),
            if (next != null)
              Expanded(
                flex: 3,
                child: FilledButton.icon(
                  onPressed: busy ? null : () => onAction(next),
                  icon: busy
                      ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                      : Icon(next.icon, size: 20),
                  label: Text(next.label),
                ),
              ),
          ]),
        ),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.order});
  final OrderDetail order;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final muted = theme.colorScheme.onSurfaceVariant;
    final o = order;
    final payment = ref.watch(orderPaymentProvider(o.id)).value;
    final itemsCount = o.items.fold<int>(0, (sum, i) => sum + i.quantity);

    return ListView(
      padding: const EdgeInsets.fromLTRB(Gap.s4, Gap.s2, Gap.s4, Gap.s5),
      children: [
        // Summary
        Card(
          child: Padding(
            padding: const EdgeInsets.all(Gap.s5),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(children: [
                StatusChip(status: o.status),
                const Spacer(),
                Text(timeAgo(o.createdAtUtc), style: theme.textTheme.bodySmall?.copyWith(color: muted)),
              ]),
              const SizedBox(height: Gap.s4),
              Text(formatMoney(o.grandTotal, o.currency), style: theme.textTheme.headlineSmall?.copyWith(fontSize: 30)),
              const SizedBox(height: Gap.s1),
              Text('$itemsCount ${itemsCount == 1 ? 'artículo' : 'artículos'} · ${formatDateTime(o.createdAtUtc)}',
                  style: theme.textTheme.bodySmall?.copyWith(color: muted)),
            ]),
          ),
        ),

        if (payment != null) ...[
          _section(context, 'Pago'),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(Gap.s4),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Row(children: [
                  Icon(Icons.payments_outlined, color: muted),
                  const SizedBox(width: Gap.s3),
                  Expanded(child: Text(payment.methodLabel, style: theme.textTheme.titleMedium)),
                ]),
                const SizedBox(height: Gap.s1),
                Text(payment.statusLabel, style: theme.textTheme.bodySmall?.copyWith(color: muted, fontWeight: FontWeight.w600)),
                if (payment.reference != null) ...[
                  const SizedBox(height: Gap.s2),
                  Text('Referencia: ${payment.reference}${payment.bankAccountCode != null ? ' · ${payment.bankAccountCode}' : ''}',
                      style: theme.textTheme.bodyMedium?.copyWith(color: muted)),
                ],
                if (payment.reviewNote != null && payment.reviewNote!.isNotEmpty) ...[
                  const SizedBox(height: Gap.s2),
                  Text('Nota: ${payment.reviewNote}', style: theme.textTheme.bodyMedium?.copyWith(color: muted)),
                ],
                if (payment.hasReceipt) ...[
                  const SizedBox(height: Gap.s3),
                  OutlinedButton.icon(
                    onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(
                      builder: (_) => ReceiptPage(paymentId: payment.id, orderNumber: o.orderNumber),
                    )),
                    icon: const Icon(Icons.receipt_long_outlined, size: 20),
                    label: const Text('Ver comprobante'),
                  ),
                ],
                if (payment.status == 'UnderReview') ...[
                  const SizedBox(height: Gap.s3),
                  _TransferActions(orderId: o.id, payment: payment),
                ],
              ]),
            ),
          ),
        ],

        _section(context, o.pickupStoreName != null ? 'Recoger en tienda' : 'Envío'),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(Gap.s4),
            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Icon(Icons.place_outlined, color: muted),
              const SizedBox(width: Gap.s3),
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  if (o.pickupStoreName != null) Text(o.pickupStoreName!, style: theme.textTheme.titleMedium),
                  Text(o.pickupStoreName != null ? 'Retira: ${o.recipient}' : o.recipient,
                      style: o.pickupStoreName != null ? theme.textTheme.bodyMedium : theme.textTheme.titleMedium),
                  const SizedBox(height: Gap.s1),
                  for (final l in o.addressLines) Text(l, style: theme.textTheme.bodyMedium?.copyWith(color: muted)),
                ]),
              ),
            ]),
          ),
        ),

        _section(context, 'Artículos'),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(Gap.s4),
            child: Column(children: [
              for (var i = 0; i < o.items.length; i++) ...[
                if (i > 0) const Divider(height: Gap.s5),
                Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Expanded(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Text(o.items[i].productName, style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600)),
                      const SizedBox(height: 2),
                      Text('${o.items[i].sku} · ${o.items[i].quantity} × ${formatMoney(o.items[i].unitPrice, '').trim()}',
                          style: theme.textTheme.bodySmall?.copyWith(color: muted)),
                    ]),
                  ),
                  const SizedBox(width: Gap.s3),
                  Text(formatMoney(o.items[i].lineTotal, '').trim(), style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600)),
                ]),
              ],
              const Divider(height: Gap.s5),
              _total(context, 'Subtotal', o.subtotal, o.currency),
              if (o.discountTotal > 0) _total(context, 'Descuento', -o.discountTotal, o.currency),
              _total(context, 'Impuestos', o.taxTotal, o.currency),
              if (o.shippingTotal > 0) _total(context, 'Envío', o.shippingTotal, o.currency),
              const SizedBox(height: Gap.s1),
              _total(context, 'Total', o.grandTotal, o.currency, strong: true),
            ]),
          ),
        ),

        _section(context, 'Historial'),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(Gap.s4),
            child: Column(children: [
              for (var i = 0; i < o.history.length; i++)
                _TimelineRow(
                  entry: o.history.reversed.elementAt(i),
                  isFirst: i == 0,
                  isLast: i == o.history.length - 1,
                ),
            ]),
          ),
        ),
      ],
    );
  }

  Widget _section(BuildContext context, String title) => Padding(
        padding: const EdgeInsets.fromLTRB(Gap.s1, Gap.s5, Gap.s1, Gap.s2),
        child: Text(
          title.toUpperCase(),
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant,
                letterSpacing: 1,
                fontWeight: FontWeight.w700,
              ),
        ),
      );

  Widget _total(BuildContext context, String label, double v, String currency, {bool strong = false}) {
    final style = strong
        ? Theme.of(context).textTheme.titleMedium
        : Theme.of(context).textTheme.bodyMedium?.copyWith(color: Theme.of(context).colorScheme.onSurfaceVariant);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(children: [Text(label, style: style), const Spacer(), Text(formatMoney(v, currency), style: style)]),
    );
  }
}

class _TimelineRow extends StatelessWidget {
  const _TimelineRow({required this.entry, required this.isFirst, required this.isLast});
  final HistoryEntry entry;
  final bool isFirst;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final tone = OrderStatus.tone(entry.status);

    return IntrinsicHeight(
      child: Row(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        SizedBox(
          width: 20,
          child: Column(children: [
            Container(
              width: 12,
              height: 12,
              margin: const EdgeInsets.only(top: 4),
              decoration: BoxDecoration(
                color: isFirst ? tone.background : scheme.surface,
                shape: BoxShape.circle,
                border: Border.all(color: isFirst ? tone.foreground : scheme.outline, width: 2),
              ),
            ),
            if (!isLast) Expanded(child: Container(width: 2, color: scheme.outlineVariant)),
          ]),
        ),
        const SizedBox(width: Gap.s3),
        Expanded(
          child: Padding(
            padding: EdgeInsets.only(bottom: isLast ? 0 : Gap.s4),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(OrderStatus.label(entry.status), style: theme.textTheme.bodyLarge?.copyWith(fontWeight: isFirst ? FontWeight.w700 : FontWeight.w500)),
              const SizedBox(height: 2),
              Text(
                [formatDateTime(entry.at), if (entry.note != null && entry.note!.isNotEmpty) entry.note!].join(' · '),
                style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
              ),
            ]),
          ),
        ),
      ]),
    );
  }
}

/// Bottom sheet that confirms a state change (and asks for a reason when cancelling).
/// Pops with the reason (empty string when none) on confirm, or null when dismissed.
class _ConfirmSheet extends StatefulWidget {
  const _ConfirmSheet({required this.order, required this.action, required this.target});
  final OrderDetail order;
  final OrderAction action;
  final String target;

  @override
  State<_ConfirmSheet> createState() => _ConfirmSheetState();
}

class _ConfirmSheetState extends State<_ConfirmSheet> {
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final cancel = widget.action == OrderAction.cancel;

    return Padding(
      padding: EdgeInsets.fromLTRB(Gap.s5, Gap.s1, Gap.s5, MediaQuery.of(context).viewInsets.bottom + Gap.s5),
      child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(cancel ? 'Cancelar pedido' : widget.action.label, style: theme.textTheme.titleLarge),
        const SizedBox(height: Gap.s2),
        Row(children: [
          StatusChip(status: widget.order.status),
          const Padding(padding: EdgeInsets.symmetric(horizontal: Gap.s2), child: Icon(Icons.arrow_forward, size: 16)),
          StatusChip(status: widget.target),
        ]),
        const SizedBox(height: Gap.s3),
        Text(
          cancel
              ? 'El pedido ${widget.order.orderNumber} se cancelará y no se podrá reabrir.'
              : 'El pedido ${widget.order.orderNumber} cambiará de estado y el cliente recibirá el aviso.',
          style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
        ),
        if (cancel) ...[
          const SizedBox(height: Gap.s4),
          TextField(
            controller: _reason,
            autofocus: true,
            textCapitalization: TextCapitalization.sentences,
            decoration: const InputDecoration(labelText: 'Motivo (opcional)'),
          ),
        ],
        const SizedBox(height: Gap.s5),
        Row(children: [
          Expanded(child: OutlinedButton(onPressed: () => Navigator.pop(context), child: const Text('Volver'))),
          const SizedBox(width: Gap.s3),
          Expanded(
            child: FilledButton(
              style: cancel ? FilledButton.styleFrom(backgroundColor: scheme.error, foregroundColor: Colors.white) : null,
              onPressed: () => Navigator.pop(
                context,
                cancel ? (_reason.text.trim().isEmpty ? 'Cancelado por administración' : _reason.text.trim()) : '',
              ),
              child: Text(cancel ? 'Cancelar pedido' : 'Confirmar'),
            ),
          ),
        ]),
      ]),
    );
  }
}

/// Approve / reject buttons for a transfer whose receipt is waiting for verification.
class _TransferActions extends ConsumerStatefulWidget {
  const _TransferActions({required this.orderId, required this.payment});
  final String orderId;
  final OrderPayment payment;

  @override
  ConsumerState<_TransferActions> createState() => _TransferActionsState();
}

class _TransferActionsState extends ConsumerState<_TransferActions> {
  bool _busy = false;

  Future<void> _run(Future<void> Function() action, String done) async {
    setState(() => _busy = true);
    try {
      await action();
      ref.invalidate(orderPaymentProvider(widget.orderId));
      ref.invalidate(orderDetailProvider(widget.orderId));
      ref.invalidate(ordersProvider);
      ref.invalidate(summaryProvider);
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(done)));
    } catch (e) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _approve() async {
    final ok = await showModalBottomSheet<bool>(
      context: context,
      builder: (_) => const _VerifySheet(),
    );
    if (ok != true) return;
    await _run(() => ref.read(ordersRepositoryProvider).approvePayment(widget.payment.id), 'Transferencia verificada: el pedido quedó pagado.');
  }

  Future<void> _reject() async {
    final reason = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _RejectSheet(),
    );
    if (reason == null || reason.isEmpty) return;
    await _run(() => ref.read(ordersRepositoryProvider).rejectPayment(widget.payment.id, reason), 'Comprobante devuelto al cliente.');
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Row(children: [
      Expanded(
        child: OutlinedButton(
          style: OutlinedButton.styleFrom(foregroundColor: scheme.error, side: BorderSide(color: scheme.error.withValues(alpha: 0.5))),
          onPressed: _busy ? null : _reject,
          child: const Text('Rechazar'),
        ),
      ),
      const SizedBox(width: Gap.s3),
      Expanded(
        child: FilledButton.icon(
          onPressed: _busy ? null : _approve,
          icon: _busy
              ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : const Icon(Icons.verified_outlined, size: 20),
          label: const Text('Aprobar'),
        ),
      ),
    ]);
  }
}

/// Confirms that the receipt was checked against the bank statement. Pops true on confirm.
class _VerifySheet extends StatelessWidget {
  const _VerifySheet();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.fromLTRB(Gap.s5, Gap.s1, Gap.s5, Gap.s5),
      child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text('Aprobar transferencia', style: theme.textTheme.titleLarge),
        const SizedBox(height: Gap.s2),
        Text(
          'El pago se registrará como cobrado y el pedido pasará a Pagado. Confirma que el dinero ya está en la cuenta.',
          style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
        ),
        const SizedBox(height: Gap.s5),
        Row(children: [
          Expanded(child: OutlinedButton(onPressed: () => Navigator.pop(context, false), child: const Text('Volver'))),
          const SizedBox(width: Gap.s3),
          Expanded(child: FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Aprobar'))),
        ]),
      ]),
    );
  }
}

/// Asks why the receipt is not accepted (required: the customer sees it). Pops with the reason, or null when dismissed.
class _RejectSheet extends StatefulWidget {
  const _RejectSheet();

  @override
  State<_RejectSheet> createState() => _RejectSheetState();
}

class _RejectSheetState extends State<_RejectSheet> {
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Padding(
      padding: EdgeInsets.fromLTRB(Gap.s5, Gap.s1, Gap.s5, MediaQuery.of(context).viewInsets.bottom + Gap.s5),
      child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text('Rechazar comprobante', style: theme.textTheme.titleLarge),
        const SizedBox(height: Gap.s2),
        Text(
          'El cliente verá el motivo y podrá adjuntar otro comprobante.',
          style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
        ),
        const SizedBox(height: Gap.s4),
        TextField(
          controller: _reason,
          autofocus: true,
          maxLength: 500,
          maxLines: 3,
          textCapitalization: TextCapitalization.sentences,
          onChanged: (_) => setState(() {}),
          decoration: const InputDecoration(labelText: 'Motivo'),
        ),
        const SizedBox(height: Gap.s4),
        Row(children: [
          Expanded(child: OutlinedButton(onPressed: () => Navigator.pop(context), child: const Text('Volver'))),
          const SizedBox(width: Gap.s3),
          Expanded(
            child: FilledButton(
              style: FilledButton.styleFrom(backgroundColor: scheme.error, foregroundColor: Colors.white),
              onPressed: _reason.text.trim().isEmpty ? null : () => Navigator.pop(context, _reason.text.trim()),
              child: const Text('Rechazar'),
            ),
          ),
        ]),
      ]),
    );
  }
}
