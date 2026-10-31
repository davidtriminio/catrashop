import 'package:intl/date_symbol_data_local.dart';
import 'package:intl/intl.dart';

/// Call once at startup (and in tests) before formatting dates: loads the Spanish month names.
Future<void> initFormats() => initializeDateFormatting('es');

final _money = NumberFormat.currency(symbol: '', decimalDigits: 2);
final _dayMonth = DateFormat('d MMM', 'es');
final _full = DateFormat('d MMM yyyy, HH:mm', 'es');

/// Prefixes shown instead of the ISO code. The store sells in lempiras ("LPS. 1,234.50").
const _symbols = {'HNL': 'LPS.'};

String formatMoney(double value, String currency) {
  final prefix = _symbols[currency.toUpperCase()] ?? currency;
  final sign = value < 0 ? '-' : '';
  return '$sign$prefix ${_money.format(value.abs())}'.trim();
}

String formatDateTime(DateTime dt) => _full.format(dt.toLocal());

/// The order number without its constant "ORD-" prefix, so the meaningful part fits on a list row.
String shortOrderId(String orderNumber) => orderNumber.replaceFirst(RegExp(r'^ORD-', caseSensitive: false), '');

/// Short Spanish relative time: "ahora", "hace 5 min", "hace 3 h", "ayer", "12 oct".
/// [now] is injectable so it can be tested.
String timeAgo(DateTime when, {DateTime? now}) {
  final current = (now ?? DateTime.now()).toLocal();
  final t = when.toLocal();
  final diff = current.difference(t);
  if (diff.isNegative || diff.inSeconds < 45) return 'ahora';
  if (diff.inMinutes < 60) return 'hace ${diff.inMinutes} min';
  if (diff.inHours < 24 && current.day == t.day) return 'hace ${diff.inHours} h';
  final yesterday = DateTime(current.year, current.month, current.day).subtract(const Duration(days: 1));
  if (DateTime(t.year, t.month, t.day) == yesterday) return 'ayer';
  if (diff.inDays < 7) return 'hace ${diff.inDays} d';
  return _dayMonth.format(t);
}
