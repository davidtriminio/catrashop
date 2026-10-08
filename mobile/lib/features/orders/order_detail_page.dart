import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/providers.dart';
import 'order_models.dart';

final _money = NumberFormat.currency(symbol: '', decimalDigits: 2);
final _dateFmt = DateFormat('dd MMM yyyy, HH:mm');

class OrderDetailPage extends ConsumerStatefulWidget {
  const OrderDetailPage({super.key, required this.orderId});
  final String orderId;

  @override
  ConsumerState<OrderDetailPage> createState() => _OrderDetailPageState();
}

class _OrderDetailPageState extends ConsumerState<OrderDetailPage> {
  bool _busy = false;

  Future<void> _run(OrderDetail o, OrderAction action) async {
    String? reason;
    if (action == OrderAction.cancel) {
      reason = await _askReason();
      if (reason == null) return;
    } else {
      final ok = await showDialog<bool>(
        context: context,
        builder: (c) => AlertDialog(
          title: Text(action.label),
          content: Text('Pedido ${o.orderNumber}: ${OrderStatus.label(o.status)} → siguiente estado.'),
          actions: [
            TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Volver')),
            FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Confirmar')),
          ],
        ),
      );
      if (ok != true) return;
    }

    setState(() => _busy = true);
    try {
      await ref.read(ordersRepositoryProvider).perform(o.id, action, reason: reason);
      ref.invalidate(orderDetailProvider(o.id));
      ref.invalidate(ordersProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Estado actualizado.')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<String?> _askReason() async {
    final ctrl = TextEditingController();
    final r = await showDialog<String>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Cancelar pedido'),
        content: TextField(
          controller: ctrl,
          autofocus: true,
          decoration: const InputDecoration(labelText: 'Motivo', border: OutlineInputBorder()),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c), child: const Text('Volver')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Theme.of(c).colorScheme.error),
            onPressed: () => Navigator.pop(c, ctrl.text.trim().isEmpty ? 'Cancelado por administración' : ctrl.text.trim()),
            child: const Text('Cancelar pedido'),
          ),
        ],
      ),
    );
    ctrl.dispose();
    return r;
  }

  @override
  Widget build(BuildContext context) {
    final detail = ref.watch(orderDetailProvider(widget.orderId));
    return Scaffold(
      appBar: AppBar(title: Text(detail.value?.orderNumber ?? 'Pedido')),
      body: detail.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(mainAxisSize: MainAxisSize.min, children: [
            Text(e.toString()),
            const SizedBox(height: 12),
            OutlinedButton(
              onPressed: () => ref.invalidate(orderDetailProvider(widget.orderId)),
              child: const Text('Reintentar'),
            ),
          ]),
        ),
        data: (o) => _Body(order: o, busy: _busy, onAction: (a) => _run(o, a)),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.order, required this.busy, required this.onAction});
  final OrderDetail order;
  final bool busy;
  final void Function(OrderAction) onAction;

  @override
  Widget build(BuildContext context) {
    final o = order;
    final next = OrderAction.nextFor(o.status);
    final color = OrderStatus.color(o.status);
    final text = Theme.of(context).textTheme;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Row(children: [
          Chip(
            label: Text(OrderStatus.label(o.status)),
            backgroundColor: color.withValues(alpha: 0.15),
            labelStyle: TextStyle(color: color, fontWeight: FontWeight.w600),
            side: BorderSide.none,
          ),
          const Spacer(),
          Text(_dateFmt.format(o.createdAtUtc.toLocal()), style: text.bodySmall),
        ]),
        const SizedBox(height: 16),
        if (next != null || OrderAction.canCancel(o.status)) ...[
          Row(children: [
            if (next != null)
              Expanded(
                child: FilledButton.icon(
                  onPressed: busy ? null : () => onAction(next),
                  icon: Icon(next.icon),
                  label: Text(next.label),
                ),
              ),
            if (next != null && OrderAction.canCancel(o.status)) const SizedBox(width: 12),
            if (OrderAction.canCancel(o.status))
              Expanded(
                child: OutlinedButton.icon(
                  style: OutlinedButton.styleFrom(foregroundColor: Theme.of(context).colorScheme.error),
                  onPressed: busy ? null : () => onAction(OrderAction.cancel),
                  icon: const Icon(Icons.cancel_outlined),
                  label: const Text('Cancelar'),
                ),
              ),
          ]),
          if (busy) const Padding(padding: EdgeInsets.only(top: 12), child: LinearProgressIndicator()),
          const SizedBox(height: 16),
        ],
        _section(context, 'Envío'),
        Text(o.recipient, style: text.bodyLarge),
        for (final l in o.addressLines) Text(l),
        const SizedBox(height: 16),
        _section(context, 'Artículos'),
        for (final i in o.items)
          ListTile(
            contentPadding: EdgeInsets.zero,
            dense: true,
            title: Text(i.productName),
            subtitle: Text('${i.sku} · ${i.quantity} × ${_money.format(i.unitPrice)}'),
            trailing: Text(_money.format(i.lineTotal)),
          ),
        const Divider(),
        _totalRow('Subtotal', o.subtotal, o.currency),
        if (o.discountTotal > 0) _totalRow('Descuento', -o.discountTotal, o.currency),
        _totalRow('Impuestos', o.taxTotal, o.currency),
        _totalRow('Total', o.grandTotal, o.currency, bold: true),
        const SizedBox(height: 16),
        _section(context, 'Historial'),
        for (final h in o.history.reversed)
          ListTile(
            contentPadding: EdgeInsets.zero,
            dense: true,
            leading: Icon(Icons.circle, size: 12, color: OrderStatus.color(h.status)),
            title: Text(OrderStatus.label(h.status)),
            subtitle: Text([_dateFmt.format(h.at.toLocal()), if (h.note != null) h.note!].join(' · ')),
          ),
      ],
    );
  }

  Widget _section(BuildContext context, String t) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Text(t, style: Theme.of(context).textTheme.titleSmall?.copyWith(color: Theme.of(context).colorScheme.primary)),
      );

  Widget _totalRow(String label, double v, String cur, {bool bold = false}) {
    final style = TextStyle(fontWeight: bold ? FontWeight.w700 : FontWeight.normal, fontSize: bold ? 16 : 14);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(children: [
        Text(label, style: style),
        const Spacer(),
        Text('$cur ${_money.format(v)}', style: style),
      ]),
    );
  }
}
