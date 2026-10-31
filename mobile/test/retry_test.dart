import 'package:catrashop_admin/core/api_client.dart';
import 'package:catrashop_admin/core/config.dart';
import 'package:catrashop_admin/core/format.dart';
import 'package:catrashop_admin/core/providers.dart';
import 'package:catrashop_admin/core/theme.dart';
import 'package:catrashop_admin/features/orders/order_models.dart';
import 'package:catrashop_admin/features/orders/orders_page.dart';
import 'package:catrashop_admin/features/orders/widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'fakes.dart';

/// A backend that answers instantly with a 500 (the real symptom: it fails so fast that "Reintentar" looked inert),
/// and can be switched to answer normally.
class FlakyOrdersRepository extends FakeOrdersRepository {
  bool failing = true;
  int calls = 0;

  @override
  Future<OrderPage> list({String? status, int page = 1}) {
    calls++;
    if (failing) throw ApiException('An error occurred while processing your request.', statusCode: 500);
    return super.list(status: status, page: page);
  }
}

Future<void> openOrders(WidgetTester tester, FlakyOrdersRepository repo) async {
  tester.view.physicalSize = const Size(390 * 2, 844 * 2);
  tester.view.devicePixelRatio = 2;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    retry: (retryCount, error) => null, // same as main.dart
    overrides: [
      ordersRepositoryProvider.overrideWithValue(repo),
      newOrderPollerProvider.overrideWith((ref) {}),
      profileProvider.overrideWith((ref) async => const ProfileInfo(email: 'admin@catrashop.local', roles: {'Administrator'})),
    ],
    child: MaterialApp(theme: AppTheme.light(), home: const OrdersPage()),
  ));
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(initFormats);

  testWidgets('a failed load says what failed and which server was asked', (tester) async {
    await openOrders(tester, FlakyOrdersRepository());

    expect(find.text('No se pudieron cargar los pedidos'), findsOneWidget);
    expect(find.textContaining('An error occurred while processing your request.'), findsOneWidget);
    expect(find.textContaining('error del servidor'), findsOneWidget, reason: '5xx explains it is the server, not the app');
    expect(find.textContaining('Servidor: $apiBaseUrl'), findsOneWidget);
    expect(find.textContaining('Intento'), findsNothing, reason: 'no retry yet');
  });

  testWidgets('Reintentar visibly reloads and numbers each attempt', (tester) async {
    final repo = FlakyOrdersRepository();
    await openOrders(tester, repo);
    expect(repo.calls, 1);

    await tester.tap(find.text('Reintentar'));
    await tester.pump(); // the press itself
    await tester.pump(const Duration(milliseconds: 100));
    expect(find.byType(OrderCardSkeleton), findsWidgets, reason: 'a loading state shows even though the server fails at once');
    expect(find.text('No se pudieron cargar los pedidos'), findsNothing);

    await tester.pump(const Duration(milliseconds: 700)); // the loading state is held for a moment on purpose
    await tester.pumpAndSettle();
    expect(repo.calls, 2);
    expect(find.textContaining('Intento 1 ·'), findsOneWidget);

    await tester.tap(find.text('Reintentar'));
    await tester.pump(const Duration(milliseconds: 700));
    await tester.pumpAndSettle();
    expect(repo.calls, 3);
    expect(find.textContaining('Intento 2 ·'), findsOneWidget);
    expect(find.textContaining('Intento 1 ·'), findsNothing);
  });

  testWidgets('when the server recovers the retry shows the orders and forgets the attempts', (tester) async {
    final repo = FlakyOrdersRepository();
    await openOrders(tester, repo);

    repo.failing = false;
    await tester.tap(find.text('Reintentar'));
    await tester.pump(const Duration(milliseconds: 700));
    await tester.pumpAndSettle();

    expect(find.text('1001'), findsOneWidget);
    expect(find.textContaining('Intento'), findsNothing);
    final container = ProviderScope.containerOf(tester.element(find.byType(OrdersPage)));
    expect(container.read(retryInfoProvider), isNull);
  });
}
