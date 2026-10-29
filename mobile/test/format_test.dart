import 'package:commercehub_admin/core/format.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  setUpAll(initFormats);

  group('timeAgo', () {
    final now = DateTime(2026, 10, 28, 15, 0);

    test('very recent is "ahora"', () {
      expect(timeAgo(now.subtract(const Duration(seconds: 20)), now: now), 'ahora');
      expect(timeAgo(now.add(const Duration(minutes: 5)), now: now), 'ahora'); // clock skew never shows the future
    });

    test('minutes and hours today', () {
      expect(timeAgo(now.subtract(const Duration(minutes: 12)), now: now), 'hace 12 min');
      expect(timeAgo(now.subtract(const Duration(hours: 3)), now: now), 'hace 3 h');
    });

    test('yesterday, days and older dates', () {
      expect(timeAgo(DateTime(2026, 10, 27, 23, 0), now: now), 'ayer');
      expect(timeAgo(now.subtract(const Duration(days: 3)), now: now), 'hace 3 d');
      expect(timeAgo(DateTime(2026, 10, 1), now: now), '1 oct');
    });
  });

  test('formatMoney keeps two decimals and trims a missing currency', () {
    expect(formatMoney(87.8, 'USD'), 'USD 87.80');
    expect(formatMoney(5, ''), '5.00');
  });

  test('formatMoney writes lempiras as LPS. with thousands separators', () {
    expect(formatMoney(1234.5, 'HNL'), 'LPS. 1,234.50');
    expect(formatMoney(320, 'hnl'), 'LPS. 320.00');
    expect(formatMoney(-45, 'HNL'), '-LPS. 45.00');
  });
}
