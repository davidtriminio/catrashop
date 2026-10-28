import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/providers.dart';
import '../features/orders/order_models.dart';
import '../features/orders/orders_page.dart';
import '../features/settings/settings_page.dart';
import '../features/summary/summary_page.dart';

/// Signed-in shell: bottom navigation between orders, summary and settings. Hosts the new-order poller so it keeps
/// running whichever tab is open.
class HomeShell extends ConsumerWidget {
  const HomeShell({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    ref.watch(newOrderPollerProvider);
    final index = ref.watch(tabIndexProvider);
    final waiting = ref.watch(summaryProvider).value?.of(OrderStatus.paid) ?? 0;

    return Scaffold(
      body: IndexedStack(
        index: index,
        children: const [OrdersPage(), SummaryPage(), SettingsPage()],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (i) => ref.read(tabIndexProvider.notifier).select(i),
        destinations: [
          NavigationDestination(
            icon: Badge(
              isLabelVisible: waiting > 0,
              label: Text('$waiting'),
              child: const Icon(Icons.receipt_long_outlined),
            ),
            selectedIcon: Badge(
              isLabelVisible: waiting > 0,
              label: Text('$waiting'),
              child: const Icon(Icons.receipt_long),
            ),
            label: 'Pedidos',
          ),
          const NavigationDestination(icon: Icon(Icons.insights_outlined), selectedIcon: Icon(Icons.insights), label: 'Resumen'),
          const NavigationDestination(icon: Icon(Icons.settings_outlined), selectedIcon: Icon(Icons.settings), label: 'Ajustes'),
        ],
      ),
    );
  }
}
