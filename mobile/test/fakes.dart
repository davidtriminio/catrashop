import 'package:commercehub_admin/core/api_client.dart';
import 'package:commercehub_admin/core/token_store.dart';
import 'package:commercehub_admin/features/orders/order_models.dart';
import 'package:commercehub_admin/features/orders/orders_repository.dart';

/// In-memory orders backend for widget tests (no network, no platform channels).
class FakeOrdersRepository extends OrdersRepository {
  FakeOrdersRepository({List<OrderSummary>? orders}) : orders = orders ?? sampleOrders(), super(ApiClient(TokenStore()));

  final List<OrderSummary> orders;
  final performed = <String>[];

  List<OrderSummary> _filtered(String? status) =>
      status == null ? orders : orders.where((o) => o.status == status).toList();

  @override
  Future<OrderPage> list({String? status, int page = 1}) async {
    final items = _filtered(status);
    return OrderPage(items, 1, false, items.length);
  }

  @override
  Future<int> count({String? status}) async => _filtered(status).length;

  @override
  Future<OrderDetail> detail(String id) async {
    final o = orders.firstWhere((e) => e.id == id);
    return sampleDetail(o);
  }

  @override
  Future<void> perform(String id, OrderAction action, {String? reason}) async {
    performed.add('$id:${action.path}${reason == null ? '' : ':$reason'}');
  }
}

OrderSummary order(String id, String number, String status, double total, int items, Duration ago) => OrderSummary(
      id: id,
      orderNumber: number,
      status: status,
      currency: 'USD',
      grandTotal: total,
      itemCount: items,
      createdAtUtc: DateTime.now().toUtc().subtract(ago),
    );

List<OrderSummary> sampleOrders() => [
      order('1', 'ORD-1001', OrderStatus.paid, 87.78, 3, const Duration(minutes: 12)),
      order('2', 'ORD-1002', OrderStatus.paid, 13.75, 1, const Duration(hours: 3)),
      order('3', 'ORD-1003', OrderStatus.processing, 59.40, 2, const Duration(days: 1, hours: 2)),
      order('4', 'ORD-1004', OrderStatus.shipped, 120.00, 4, const Duration(days: 3)),
      order('5', 'ORD-1005', OrderStatus.delivered, 42.50, 1, const Duration(days: 9)),
    ];

OrderDetail sampleDetail(OrderSummary o) => OrderDetail(
      id: o.id,
      orderNumber: o.orderNumber,
      status: o.status,
      currency: o.currency,
      subtotal: o.grandTotal - 5,
      discountTotal: 0,
      taxTotal: 5,
      grandTotal: o.grandTotal,
      recipient: 'Cliente Prueba',
      addressLines: const ['Col. Palmira, Calle 5', '11101 Tegucigalpa', 'FM, HN'],
      items: [
        OrderItem('Lampara Nebula', 'LAMP-001', 2, 39.90, 79.80),
        OrderItem('Taza Galactica', 'TAZA-001', 1, 12.50, 12.50),
      ],
      history: [
        HistoryEntry(OrderStatus.pending, null, o.createdAtUtc),
        HistoryEntry(OrderStatus.paid, 'Pago capturado', o.createdAtUtc.add(const Duration(minutes: 1))),
        if (o.status != OrderStatus.paid) HistoryEntry(o.status, null, o.createdAtUtc.add(const Duration(hours: 1))),
      ],
      createdAtUtc: o.createdAtUtc,
    );
