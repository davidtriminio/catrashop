import 'package:catrashop_admin/core/format.dart';
import 'package:catrashop_admin/core/providers.dart';
import 'package:catrashop_admin/core/theme.dart';
import 'package:catrashop_admin/features/orders/order_detail_page.dart';
import 'package:catrashop_admin/features/orders/order_models.dart';
import 'package:catrashop_admin/features/orders/orders_page.dart';
import 'package:catrashop_admin/features/orders/widgets.dart';
import 'package:catrashop_admin/shell/home_shell.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fakes.dart';

Future<FakeOrdersRepository> pump(WidgetTester tester, Widget home, {FakeOrdersRepository? repo}) async {
  final fake = repo ?? FakeOrdersRepository();
  tester.view.physicalSize = const Size(390 * 2, 844 * 2);
  tester.view.devicePixelRatio = 2;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      ordersRepositoryProvider.overrideWithValue(fake),
      newOrderPollerProvider.overrideWith((ref) {}),
      profileProvider.overrideWith((ref) async => const ProfileInfo(email: 'admin@catrashop.local', roles: {'Administrator'})),
    ],
    child: MaterialApp(theme: AppTheme.light(), home: home),
  ));
  await tester.pumpAndSettle();
  return fake;
}

void main() {
  setUpAll(initFormats);

  testWidgets('orders list shows cards, count and filter chips', (tester) async {
    await pump(tester, const OrdersPage());

    expect(find.text('ORD-1001'), findsOneWidget);
    expect(find.text('5 pedidos'), findsOneWidget);
    expect(find.text('hace 12 min · 3 artículos'), findsOneWidget);
    // The chip row scrolls horizontally: the first ones are visible, the rest are reachable by scrolling.
    for (final label in ['Todos', 'Pagado', 'En proceso']) {
      expect(find.widgetWithText(ChoiceChip, label), findsOneWidget);
    }
    for (final label in ['Enviado', 'Entregado', 'Cancelado']) {
      await tester.ensureVisible(find.widgetWithText(ChoiceChip, label));
      await tester.pumpAndSettle();
      expect(find.widgetWithText(ChoiceChip, label), findsOneWidget);
    }
  });

  testWidgets('selecting a filter narrows the list and an empty filter offers to clear it', (tester) async {
    await pump(tester, const OrdersPage());

    await tester.ensureVisible(find.widgetWithText(ChoiceChip, 'Enviado'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ChoiceChip, 'Enviado'));
    await tester.pumpAndSettle();
    expect(find.text('ORD-1004'), findsOneWidget);
    expect(find.text('ORD-1001'), findsNothing);
    expect(find.text('1 pedido'), findsOneWidget);

    await tester.ensureVisible(find.widgetWithText(ChoiceChip, 'Cancelado'));
    await tester.pumpAndSettle();
    await tester.tap(find.widgetWithText(ChoiceChip, 'Cancelado'));
    await tester.pumpAndSettle();
    expect(find.text('No hay pedidos'), findsOneWidget);
    await tester.tap(find.text('Quitar filtro'));
    await tester.pumpAndSettle();
    expect(find.text('ORD-1001'), findsOneWidget);
  });

  testWidgets('summary highlights paid orders and opens the filtered list', (tester) async {
    await pump(tester, const HomeShell());

    await tester.tap(find.text('Resumen'));
    await tester.pumpAndSettle();
    expect(find.text('Por atender'), findsOneWidget);
    expect(find.text('2'), findsWidgets); // two paid orders

    await tester.tap(find.text('Ver pedidos pagados'));
    await tester.pumpAndSettle();
    expect(find.text('ORD-1001'), findsOneWidget);
    expect(find.text('ORD-1003'), findsNothing); // processing, filtered out
  });

  testWidgets('detail offers the next action and confirms it in a sheet', (tester) async {
    final repo = await pump(tester, const OrderDetailPage(orderId: '1'));

    expect(find.text('Iniciar proceso'), findsOneWidget); // Paid -> process
    expect(find.text('Cancelar'), findsOneWidget);

    await tester.tap(find.text('Iniciar proceso'));
    await tester.pumpAndSettle();
    expect(find.textContaining('cambiará de estado'), findsOneWidget);
    await tester.tap(find.text('Confirmar'));
    await tester.pumpAndSettle();

    expect(repo.performed, ['1:process']);
  });

  testWidgets('cancelling asks for a reason and sends it', (tester) async {
    final repo = await pump(tester, const OrderDetailPage(orderId: '1'));

    await tester.tap(find.text('Cancelar'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'Sin stock');
    await tester.tap(find.widgetWithText(FilledButton, 'Cancelar pedido'));
    await tester.pumpAndSettle();

    expect(repo.performed, ['1:cancel:Sin stock']);
  });

  testWidgets('a delivered order has no action bar', (tester) async {
    await pump(tester, const OrderDetailPage(orderId: '5'));

    expect(find.text('Iniciar proceso'), findsNothing);
    expect(find.text('Marcar entregado'), findsNothing);
    expect(find.text('Cancelar'), findsNothing);
  });

  testWidgets('every status has a readable chip', (tester) async {
    await tester.pumpWidget(MaterialApp(
      theme: AppTheme.light(),
      home: Scaffold(body: Column(children: [for (final s in [...OrderStatus.filters, OrderStatus.pending, OrderStatus.paymentFailed]) StatusChip(status: s)])),
    ));
    expect(find.byType(StatusChip), findsNWidgets(7));
    expect(find.text('Pagado'), findsOneWidget);
  });
}
