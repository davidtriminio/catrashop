import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/providers.dart';
import 'order_detail_page.dart';
import 'order_models.dart';

final _money = NumberFormat.currency(symbol: '', decimalDigits: 2);
final _dateFmt = DateFormat('dd MMM, HH:mm');

class OrdersPage extends ConsumerWidget {
  const OrdersPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.watch(newOrderPollerProvider); // keeps the 60s poller alive while this screen is up
    final orders = ref.watch(ordersProvider);
    final filter = ref.watch(statusFilterProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Pedidos'),
        actions: [
          IconButton(
            tooltip: 'Cerrar sesión',
            icon: const Icon(Icons.logout),
            onPressed: () => ref.read(sessionProvider.notifier).logout(),
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(52),
          child: SizedBox(
            height: 52,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              children: [
                _chip(ref, 'Todos', null, filter),
                for (final s in OrderStatus.filters) _chip(ref, OrderStatus.label(s), s, filter),
              ],
            ),
          ),
        ),
      ),
      body: RefreshIndicator(
        onRefresh: () => ref.read(ordersProvider.notifier).refresh(),
        child: orders.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => _ErrorView(message: e.toString(), onRetry: () => ref.invalidate(ordersProvider)),
          data: (s) => s.items.isEmpty
              ? ListView(children: const [SizedBox(height: 160), Center(child: Text('No hay pedidos.'))])
              : NotificationListener<ScrollNotification>(
                  onNotification: (n) {
                    if (n.metrics.pixels > n.metrics.maxScrollExtent - 300) {
                      ref.read(ordersProvider.notifier).loadMore();
                    }
                    return false;
                  },
                  child: ListView.separated(
                    physics: const AlwaysScrollableScrollPhysics(),
                    itemCount: s.items.length + (s.hasNext ? 1 : 0),
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, i) {
                      if (i >= s.items.length) {
                        return const Padding(
                          padding: EdgeInsets.all(16),
                          child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
                        );
                      }
                      return _OrderTile(order: s.items[i]);
                    },
                  ),
                ),
        ),
      ),
    );
  }

  Widget _chip(WidgetRef ref, String label, String? value, String? current) => Padding(
        padding: const EdgeInsets.only(right: 8),
        child: ChoiceChip(
          label: Text(label),
          selected: current == value,
          onSelected: (_) => ref.read(statusFilterProvider.notifier).select(value),
        ),
      );
}

class _OrderTile extends StatelessWidget {
  const _OrderTile({required this.order});
  final OrderSummary order;

  @override
  Widget build(BuildContext context) {
    final color = OrderStatus.color(order.status);
    return ListTile(
      onTap: () => Navigator.of(context).push(
        MaterialPageRoute<void>(builder: (_) => OrderDetailPage(orderId: order.id)),
      ),
      title: Text(order.orderNumber, style: const TextStyle(fontWeight: FontWeight.w600)),
      subtitle: Text('${_dateFmt.format(order.createdAtUtc.toLocal())} · ${order.itemCount} artículo(s)'),
      trailing: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text('${order.currency} ${_money.format(order.grandTotal)}', style: const TextStyle(fontWeight: FontWeight.w600)),
          const SizedBox(height: 4),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(color: color.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(10)),
            child: Text(OrderStatus.label(order.status), style: TextStyle(color: color, fontSize: 12)),
          ),
        ],
      ),
    );
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => ListView(
        children: [
          const SizedBox(height: 160),
          Center(child: Text(message, textAlign: TextAlign.center)),
          const SizedBox(height: 12),
          Center(child: OutlinedButton(onPressed: onRetry, child: const Text('Reintentar'))),
        ],
      );
}
