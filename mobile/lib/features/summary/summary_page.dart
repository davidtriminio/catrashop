import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../../core/theme.dart';
import '../orders/order_models.dart';
import '../orders/widgets.dart';

class SummaryPage extends ConsumerWidget {
  const SummaryPage({super.key});

  void _open(WidgetRef ref, String? status) {
    ref.read(statusFilterProvider.notifier).select(status);
    ref.read(tabIndexProvider.notifier).select(0);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summary = ref.watch(summaryProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Resumen')),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(summaryProvider);
          await ref.read(summaryProvider.future);
        },
        child: summary.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          // While a retry is under way Riverpod still reports the previous error: show the spinner instead.
          error: (e, _) => summary.isLoading ? const Center(child: CircularProgressIndicator()) : ListView(children: [
            SizedBox(
              height: MediaQuery.sizeOf(context).height * 0.6,
              child: LoadError(
                title: 'No se pudo cargar el resumen',
                error: e,
                onRetry: () => ref.invalidate(summaryProvider),
              ),
            ),
          ]),
          data: (s) => ListView(
            padding: const EdgeInsets.fromLTRB(Gap.s4, Gap.s2, Gap.s4, Gap.s5),
            children: [
              _AttentionCard(count: s.of(OrderStatus.paid), onTap: () => _open(ref, OrderStatus.paid)),
              const SizedBox(height: Gap.s3),
              Row(children: [
                Expanded(child: _StatCard(label: 'En proceso', status: OrderStatus.processing, count: s.of(OrderStatus.processing), onTap: () => _open(ref, OrderStatus.processing))),
                const SizedBox(width: Gap.s3),
                Expanded(child: _StatCard(label: 'Enviados', status: OrderStatus.shipped, count: s.of(OrderStatus.shipped), onTap: () => _open(ref, OrderStatus.shipped))),
              ]),
              const SizedBox(height: Gap.s3),
              Row(children: [
                Expanded(child: _StatCard(label: 'Entregados', status: OrderStatus.delivered, count: s.of(OrderStatus.delivered), onTap: () => _open(ref, OrderStatus.delivered))),
                const SizedBox(width: Gap.s3),
                Expanded(child: _StatCard(label: 'Cancelados', status: OrderStatus.cancelled, count: s.of(OrderStatus.cancelled), onTap: () => _open(ref, OrderStatus.cancelled))),
              ]),
              const SizedBox(height: Gap.s3),
              Card(
                clipBehavior: Clip.antiAlias,
                child: ListTile(
                  onTap: () => _open(ref, null),
                  leading: const Icon(Icons.receipt_long_outlined),
                  title: const Text('Todos los pedidos'),
                  trailing: Row(mainAxisSize: MainAxisSize.min, children: [
                    Text('${s.total}', style: Theme.of(context).textTheme.titleMedium),
                    const SizedBox(width: Gap.s1),
                    const Icon(Icons.chevron_right),
                  ]),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Hero card: how many paid orders are waiting for someone to start processing them.
class _AttentionCard extends StatelessWidget {
  const _AttentionCard({required this.count, required this.onTap});
  final int count;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      decoration: BoxDecoration(
        color: AppColors.ink,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.darkBorder),
      ),
      padding: const EdgeInsets.all(Gap.s5),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
          decoration: BoxDecoration(color: AppColors.lime, borderRadius: BorderRadius.circular(999)),
          child: const Text('Por atender', style: TextStyle(color: AppColors.limeInk, fontSize: 12, fontWeight: FontWeight.w700)),
        ),
        const SizedBox(height: Gap.s4),
        Text('$count', style: theme.textTheme.displayMedium?.copyWith(color: Colors.white, fontWeight: FontWeight.w700, letterSpacing: -2, height: 1)),
        const SizedBox(height: Gap.s2),
        Text(
          count == 0
              ? 'No hay pedidos pagados esperando.'
              : count == 1
                  ? 'pedido pagado esperando que lo inicies'
                  : 'pedidos pagados esperando que los inicies',
          style: theme.textTheme.bodyMedium?.copyWith(color: AppColors.darkMuted),
        ),
        const SizedBox(height: Gap.s4),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: AppColors.lime, foregroundColor: AppColors.limeInk),
          onPressed: onTap,
          child: const Text('Ver pedidos pagados'),
        ),
      ]),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({required this.label, required this.status, required this.count, required this.onTap});
  final String label;
  final String status;
  final int count;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final tone = OrderStatus.tone(status);
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(Gap.s4),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Container(
              width: 36,
              height: 36,
              decoration: BoxDecoration(color: tone.background, shape: BoxShape.circle),
              child: Icon(statusIcon(status), size: 20, color: tone.foreground),
            ),
            const SizedBox(height: Gap.s4),
            Text('$count', style: theme.textTheme.headlineSmall?.copyWith(fontSize: 28)),
            const SizedBox(height: 2),
            Text(label, style: theme.textTheme.bodyMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
          ]),
        ),
      ),
    );
  }
}
