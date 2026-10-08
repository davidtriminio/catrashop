import 'dart:convert';

import 'package:commercehub_admin/core/token_store.dart';
import 'package:commercehub_admin/features/orders/order_models.dart';
import 'package:flutter_test/flutter_test.dart';

String _jwt(Map<String, dynamic> claims) {
  String enc(Object o) => base64Url.encode(utf8.encode(jsonEncode(o))).replaceAll('=', '');
  return '${enc({'alg': 'HS256'})}.${enc(claims)}.sig';
}

void main() {
  group('state machine helpers', () {
    test('next action follows the API state machine', () {
      expect(OrderAction.nextFor(OrderStatus.paid), OrderAction.process);
      expect(OrderAction.nextFor(OrderStatus.processing), OrderAction.ship);
      expect(OrderAction.nextFor(OrderStatus.shipped), OrderAction.deliver);
      expect(OrderAction.nextFor(OrderStatus.delivered), isNull);
      expect(OrderAction.nextFor(OrderStatus.cancelled), isNull);
    });

    test('cancel is only offered before shipping', () {
      expect(OrderAction.canCancel(OrderStatus.paid), isTrue);
      expect(OrderAction.canCancel(OrderStatus.processing), isTrue);
      expect(OrderAction.canCancel(OrderStatus.shipped), isFalse);
      expect(OrderAction.canCancel(OrderStatus.delivered), isFalse);
    });
  });

  group('JSON mapping', () {
    test('OrderSummary parses the API payload', () {
      final o = OrderSummary.fromJson({
        'id': 'a1',
        'orderNumber': 'CH-1001',
        'status': 'Paid',
        'currency': 'USD',
        'grandTotal': 49.9,
        'itemCount': 2,
        'createdAtUtc': '2026-10-15T12:00:00+00:00',
      });
      expect(o.orderNumber, 'CH-1001');
      expect(o.grandTotal, 49.9);
      expect(o.createdAtUtc.isUtc, isTrue);
    });

    test('OrderDetail builds a readable address and skips blanks', () {
      final d = OrderDetail.fromJson({
        'id': 'a1',
        'orderNumber': 'CH-1001',
        'customerId': 'c1',
        'status': 'Processing',
        'currency': 'USD',
        'subtotal': 40,
        'discountTotal': 0,
        'taxTotal': 9.9,
        'grandTotal': 49.9,
        'shippingAddress': {
          'recipientName': 'Ana',
          'line1': 'Calle 1',
          'line2': null,
          'city': 'Tegucigalpa',
          'region': '',
          'postalCode': '11101',
          'country': 'HN',
        },
        'items': [
          {'id': 'i', 'productId': 'p', 'variantId': 'v', 'sku': 'S1', 'productName': 'Taza', 'unitPrice': 20, 'quantity': 2, 'lineTotal': 40},
        ],
        'history': [
          {'status': 'Paid', 'note': null, 'occurredAtUtc': '2026-10-15T12:00:00+00:00'},
        ],
        'createdAtUtc': '2026-10-15T11:59:00+00:00',
      });
      expect(d.addressLines, ['Calle 1', '11101 Tegucigalpa', 'HN']);
      expect(d.items.single.quantity, 2);
    });
  });

  group('JWT claims', () {
    test('reads single and array claims', () {
      final p = decodeJwtPayload(_jwt({'role': 'OrderManager', 'permission': ['orders.read_all', 'orders.manage']}));
      expect(claimValues(p, 'role'), {'OrderManager'});
      expect(claimValues(p, 'permission'), {'orders.read_all', 'orders.manage'});
      expect(claimValues(p, 'missing'), isEmpty);
    });

    test('garbage token yields no claims instead of throwing', () {
      expect(decodeJwtPayload('not-a-jwt'), isEmpty);
    });
  });
}
