import 'package:flutter/material.dart';

/// Order lifecycle states exactly as the API names them (see OrderStatus.cs).
class OrderStatus {
  static const pending = 'Pending';
  static const paymentPending = 'PaymentPending';
  static const paid = 'Paid';
  static const confirmed = 'Confirmed';
  static const processing = 'Processing';
  static const shipped = 'Shipped';
  static const delivered = 'Delivered';
  static const paymentFailed = 'PaymentFailed';
  static const cancelled = 'Cancelled';
  static const refundRequested = 'RefundRequested';
  static const refunded = 'Refunded';

  /// Filters offered in the list (the ones an operator acts on first).
  static const filters = [paid, confirmed, processing, shipped, delivered, cancelled];

  static String label(String s) => switch (s) {
        pending => 'Pendiente',
        paymentPending => 'Pago pendiente',
        paid => 'Pagado',
        confirmed => 'Confirmado',
        processing => 'En proceso',
        shipped => 'Enviado',
        delivered => 'Entregado',
        paymentFailed => 'Pago fallido',
        cancelled => 'Cancelado',
        refundRequested => 'Reembolso solicitado',
        refunded => 'Reembolsado',
        _ => s,
      };

  /// Soft background/foreground pair for a status chip (same pairs as the web badges, all AA contrast).
  /// Light blue marks "Paid": the queue an operator needs to act on.
  static StatusTone tone(String s) => switch (s) {
        paid || confirmed => const StatusTone(Color(0xFFDBE6FF), Color(0xFF00246B)),
        processing => const StatusTone(Color(0xFFFEF3C7), Color(0xFF78350F)),
        shipped => const StatusTone(Color(0xFFC7D8FF), Color(0xFF00246B)),
        delivered => const StatusTone(Color(0xFFDCFCE7), Color(0xFF14532D)),
        cancelled || paymentFailed => const StatusTone(Color(0xFFFEE2E2), Color(0xFF7F1D1D)),
        _ => const StatusTone(Color(0xFFECECEF), Color(0xFF3F3F46)),
      };
}

class StatusTone {
  const StatusTone(this.background, this.foreground);
  final Color background;
  final Color foreground;
}

/// The single next fulfilment step the API allows from a state (state machine in OrderStatus.cs).
enum OrderAction {
  process('process', 'Iniciar proceso', Icons.play_arrow),
  ship('ship', 'Marcar enviado', Icons.local_shipping),
  deliver('deliver', 'Marcar entregado', Icons.check_circle),
  cancel('cancel', 'Cancelar pedido', Icons.cancel);

  const OrderAction(this.path, this.label, this.icon);
  final String path;
  final String label;
  final IconData icon;

  static OrderAction? nextFor(String status) => switch (status) {
        OrderStatus.paid || OrderStatus.confirmed => process,
        OrderStatus.processing => ship,
        OrderStatus.shipped => deliver,
        _ => null,
      };

  /// Cancel is allowed before shipping (Pending, PaymentPending, PaymentFailed, Paid, Confirmed, Processing).
  static bool canCancel(String status) => const {
        OrderStatus.pending,
        OrderStatus.paymentPending,
        OrderStatus.paymentFailed,
        OrderStatus.paid,
        OrderStatus.confirmed,
        OrderStatus.processing,
      }.contains(status);
}

class OrderSummary {
  OrderSummary({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.currency,
    required this.grandTotal,
    required this.itemCount,
    required this.createdAtUtc,
  });

  final String id;
  final String orderNumber;
  final String status;
  final String currency;
  final double grandTotal;
  final int itemCount;
  final DateTime createdAtUtc;

  factory OrderSummary.fromJson(Map<String, dynamic> j) => OrderSummary(
        id: j['id'] as String,
        orderNumber: j['orderNumber'] as String,
        status: j['status'] as String,
        currency: j['currency'] as String,
        grandTotal: (j['grandTotal'] as num).toDouble(),
        itemCount: (j['itemCount'] as num).toInt(),
        createdAtUtc: DateTime.parse(j['createdAtUtc'] as String).toUtc(),
      );
}

class OrderItem {
  OrderItem(this.productName, this.sku, this.quantity, this.unitPrice, this.lineTotal);
  final String productName;
  final String sku;
  final int quantity;
  final double unitPrice;
  final double lineTotal;

  factory OrderItem.fromJson(Map<String, dynamic> j) => OrderItem(
        j['productName'] as String,
        j['sku'] as String,
        (j['quantity'] as num).toInt(),
        (j['unitPrice'] as num).toDouble(),
        (j['lineTotal'] as num).toDouble(),
      );
}

class HistoryEntry {
  HistoryEntry(this.status, this.note, this.at);
  final String status;
  final String? note;
  final DateTime at;

  factory HistoryEntry.fromJson(Map<String, dynamic> j) => HistoryEntry(
        j['status'] as String,
        j['note'] as String?,
        DateTime.parse(j['occurredAtUtc'] as String).toUtc(),
      );
}

class OrderDetail {
  OrderDetail({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.currency,
    required this.subtotal,
    required this.discountTotal,
    required this.taxTotal,
    this.shippingTotal = 0,
    required this.grandTotal,
    this.pickupStoreName,
    required this.recipient,
    required this.addressLines,
    required this.items,
    required this.history,
    required this.createdAtUtc,
  });

  final String id;
  final String orderNumber;
  final String status;
  final String currency;
  final double subtotal;
  final double discountTotal;
  final double taxTotal;

  /// Delivery cost charged for the destination zone (0 for pickup and for orders from before national shipping).
  final double shippingTotal;
  final double grandTotal;

  /// Set when the customer collects the order at a store instead of receiving it.
  final String? pickupStoreName;
  final String recipient;
  final List<String> addressLines;
  final List<OrderItem> items;
  final List<HistoryEntry> history;
  final DateTime createdAtUtc;

  factory OrderDetail.fromJson(Map<String, dynamic> j) {
    final a = j['shippingAddress'] as Map<String, dynamic>;
    String? s(String k) {
      final v = a[k] as String?;
      return (v == null || v.trim().isEmpty) ? null : v;
    }

    return OrderDetail(
      id: j['id'] as String,
      orderNumber: j['orderNumber'] as String,
      status: j['status'] as String,
      currency: j['currency'] as String,
      subtotal: (j['subtotal'] as num).toDouble(),
      discountTotal: (j['discountTotal'] as num).toDouble(),
      taxTotal: (j['taxTotal'] as num).toDouble(),
      shippingTotal: ((j['shippingTotal'] as num?) ?? 0).toDouble(),
      grandTotal: (j['grandTotal'] as num).toDouble(),
      pickupStoreName: j['deliveryMode'] == 'Pickup' ? j['pickupStoreName'] as String? : null,
      recipient: a['recipientName'] as String,
      addressLines: [
        s('line1'),
        s('line2'),
        [s('postalCode'), s('city')].whereType<String>().join(' '),
        [s('region'), s('country')].whereType<String>().join(', '),
      ].whereType<String>().where((e) => e.isNotEmpty).toList(),
      items: (j['items'] as List).map((e) => OrderItem.fromJson(e as Map<String, dynamic>)).toList(),
      history: (j['history'] as List).map((e) => HistoryEntry.fromJson(e as Map<String, dynamic>)).toList(),
      createdAtUtc: DateTime.parse(j['createdAtUtc'] as String).toUtc(),
    );
  }
}

class OrderPage {
  OrderPage(this.items, this.page, this.hasNext, [this.totalCount = 0]);
  final List<OrderSummary> items;
  final int page;
  final bool hasNext;
  final int totalCount;
}

/// The payment attached to an order (what the operator needs to verify or collect it).
class OrderPayment {
  OrderPayment({
    required this.id,
    required this.status,
    required this.method,
    required this.hasReceipt,
    this.bankAccountCode,
    this.reference,
    this.reviewNote,
  });

  final String id;
  final String status;
  final String? method;
  final bool hasReceipt;
  final String? bankAccountCode;
  final String? reference;
  final String? reviewNote;

  factory OrderPayment.fromJson(Map<String, dynamic> j) => OrderPayment(
        id: j['id'] as String,
        status: j['status'] as String,
        method: j['method'] as String?,
        hasReceipt: j['hasReceipt'] as bool? ?? false,
        bankAccountCode: j['transferBankAccountCode'] as String?,
        reference: j['transferReference'] as String?,
        reviewNote: j['reviewNote'] as String?,
      );

  /// Cash on delivery and pay-at-store: no receipt, the money is collected later and registered by an operator.
  bool get collectedLater => method == 'CONTRA_ENTREGA' || method == 'PAGO_EN_TIENDA';

  bool get isPending => status == 'Pending';

  String get methodLabel => switch (method) {
        'TRANSFERENCIA' => 'Transferencia bancaria',
        'CONTRA_ENTREGA' => 'Contra entrega',
        'PAGO_EN_TIENDA' => 'Pago en tienda',
        null => 'Pasarela de pago',
        final m => m,
      };

  String get statusLabel => switch (status) {
        'Pending' => 'Pendiente',
        'Authorized' => 'Autorizado',
        'Captured' => 'Cobrado',
        'Failed' => 'Fallido',
        'Refunded' => 'Reembolsado',
        'UnderReview' => 'Comprobante por verificar',
        final s => s,
      };
}
