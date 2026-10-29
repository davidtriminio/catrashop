import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../../core/theme.dart';
import 'order_detail_page.dart';
import 'order_models.dart';
import 'widgets.dart';

class OrdersPage extends ConsumerWidget {
  const OrdersPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final orders = ref.watch(ordersProvider);
    final filter = ref.watch(statusFilterProvider);
    final theme = Theme.of(context);

    Widget skeleton() => ListView.separated(
          physics: const NeverScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(Gap.s4, 0, Gap.s4, Gap.s5),
          itemCount: 6,
          separatorBuilder: (_, _) => const SizedBox(height: Gap.s2),
          itemBuilder: (_, _) => const OrderCardSkeleton(),
        );

    return Scaffold(
      appBar: AppBar(title: const Text('Pedidos')),
      body: Column(children: [
        SizedBox(
          height: 52,
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: Gap.s4, vertical: Gap.s2),
            child: Row(children: [
              _chip(ref, 'Todos', null, filter),
              for (final s in OrderStatus.filters) _chip(ref, OrderStatus.label(s), s, filter),
            ]),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(Gap.s4, Gap.s2, Gap.s4, Gap.s2),
          child: Align(
            alignment: Alignment.centerLeft,
            child: Text(
              orders.value == null
                  ? ' '
                  : orders.value!.totalCount == 1
                      ? '1 pedido'
                      : '${orders.value!.totalCount} pedidos',
              style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
            ),
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => ref.read(ordersProvider.notifier).refresh(),
            child: orders.when(
              loading: skeleton,
              // While a retry is under way Riverpod still reports the previous error: show the loading state instead,
              // otherwise pressing "Reintentar" would change nothing on screen.
              error: (e, _) => orders.isLoading ? skeleton() : ListView(children: [
                SizedBox(
                  height: MediaQuery.sizeOf(context).height * 0.6,
                  child: LoadError(
                    title: 'No se pudieron cargar los pedidos',
                    error: e,
                    onRetry: () => ref.invalidate(ordersProvider),
                  ),
                ),
              ]),
              data: (s) => s.items.isEmpty
                  ? ListView(children: [
                      SizedBox(
                        height: 360,
                        child: StateMessage(
                          icon: Icons.inbox_outlined,
                          title: 'No hay pedidos',
                          message: filter == null ? 'Cuando entre un pedido aparecerá aquí.' : 'No hay pedidos con este estado.',
                          action: filter == null
                              ? null
                              : OutlinedButton(onPressed: () => ref.read(statusFilterProvider.notifier).select(null), child: const Text('Quitar filtro')),
                        ),
                      ),
                    ])
                  : NotificationListener<ScrollNotification>(
                      onNotification: (n) {
                        if (n.metrics.pixels > n.metrics.maxScrollExtent - 300) {
                          ref.read(ordersProvider.notifier).loadMore();
                        }
                        return false;
                      },
                      child: ListView.separated(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(Gap.s4, 0, Gap.s4, Gap.s5),
                        itemCount: s.items.length + (s.hasNext ? 1 : 0),
                        separatorBuilder: (_, _) => const SizedBox(height: Gap.s2),
                        itemBuilder: (context, i) {
                          if (i >= s.items.length) {
                            return const Padding(
                              padding: EdgeInsets.all(Gap.s4),
                              child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
                            );
                          }
                          final order = s.items[i];
                          return OrderCard(
                            order: order,
                            onTap: () => Navigator.of(context).push(
                              MaterialPageRoute<void>(builder: (_) => OrderDetailPage(orderId: order.id)),
                            ),
                          );
                        },
                      ),
                    ),
            ),
          ),
        ),
      ]),
    );
  }

  Widget _chip(WidgetRef ref, String label, String? value, String? current) => Padding(
        padding: const EdgeInsets.only(right: Gap.s2),
        child: ChoiceChip(
          label: Text(label),
          selected: current == value,
          onSelected: (_) => ref.read(statusFilterProvider.notifier).select(value),
        ),
      );
}
