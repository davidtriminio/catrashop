import '../../core/api_client.dart';
import '../../core/token_store.dart';
import 'order_models.dart';

class OrdersRepository {
  OrdersRepository(this._api);
  final ApiClient _api;

  static const pageSize = 20;

  Future<OrderPage> list({String? status, int page = 1}) async {
    final d = await _api.get('/api/v1/orders/admin', query: {
      'status': ?status,
      'page': page,
      'pageSize': pageSize,
    }) as Map<String, dynamic>;
    final items = (d['items'] as List).map((e) => OrderSummary.fromJson(e as Map<String, dynamic>)).toList();
    final total = (d['totalCount'] as num).toInt();
    return OrderPage(items, page, page * pageSize < total);
  }

  Future<OrderDetail> detail(String id) async =>
      OrderDetail.fromJson(await _api.get('/api/v1/orders/admin/$id') as Map<String, dynamic>);

  Future<void> perform(String id, OrderAction action, {String? reason}) async {
    await _api.post('/api/v1/orders/admin/$id/${action.path}',
        body: action == OrderAction.cancel ? {'reason': reason} : null);
  }
}

/// Detects orders created since the last check (newest-first page 1 is enough for a 60s/15min cadence).
/// Shared by the foreground timer and the background isolate.
class NewOrderWatcher {
  NewOrderWatcher(this._repo, this._store);
  final OrdersRepository _repo;
  final TokenStore _store;

  /// Returns the new orders (empty on the very first run, which only sets the baseline).
  Future<List<OrderSummary>> check() async {
    final latest = (await _repo.list()).items;
    if (latest.isEmpty) return const [];
    final newest = latest.map((o) => o.createdAtUtc).reduce((a, b) => a.isAfter(b) ? a : b);
    final lastSeen = await _store.readLastSeen();
    await _store.writeLastSeen(newest);
    if (lastSeen == null) return const [];
    return latest.where((o) => o.createdAtUtc.isAfter(lastSeen)).toList();
  }
}
