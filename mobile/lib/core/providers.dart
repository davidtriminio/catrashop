import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:local_auth/local_auth.dart';

import '../features/orders/order_models.dart';
import '../features/orders/orders_repository.dart';
import 'api_client.dart';
import 'config.dart';
import 'notifications.dart';
import 'token_store.dart';

final tokenStoreProvider = Provider<TokenStore>((ref) => TokenStore());

final apiClientProvider = Provider<ApiClient>((ref) {
  return ApiClient(
    ref.watch(tokenStoreProvider),
    onSessionExpired: () => ref.read(sessionProvider.notifier).expired(),
  );
});

final ordersRepositoryProvider = Provider<OrdersRepository>((ref) => OrdersRepository(ref.watch(apiClientProvider)));

// --- Session ---------------------------------------------------------------------------------

enum SessionStatus { loading, signedOut, signedIn }

class NotAdminException implements Exception {
  @override
  String toString() => 'Esta cuenta no tiene permisos para gestionar pedidos.';
}

bool _canManageOrders(String accessToken) {
  final perms = claimValues(decodeJwtPayload(accessToken), 'permission');
  return perms.contains(permOrdersRead) && perms.contains(permOrdersManage);
}

class SessionNotifier extends Notifier<SessionStatus> {
  @override
  SessionStatus build() {
    _restore();
    return SessionStatus.loading;
  }

  TokenStore get _store => ref.read(tokenStoreProvider);

  Future<void> _restore() async {
    final t = await _store.read();
    if (t != null && _canManageOrders(t.access)) {
      state = SessionStatus.signedIn;
      unawaited(startBackgroundPolling());
    } else {
      if (t != null) await _store.clear();
      state = SessionStatus.signedOut;
    }
  }

  Future<void> login(String email, String password) async {
    final api = ref.read(apiClientProvider);
    final d = await api.login(email.trim(), password);
    final access = d['accessToken'] as String;
    final refresh = d['refreshToken'] as String;
    if (!_canManageOrders(access)) {
      await api.logout(refresh); // don't leave a customer session alive on the server
      throw NotAdminException();
    }
    await _store.save(Tokens(access: access, refresh: refresh));
    ref.read(lockProvider.notifier).unlock();
    state = SessionStatus.signedIn;
    unawaited(requestNotificationPermission());
    unawaited(startBackgroundPolling());
  }

  Future<void> logout() async {
    final t = await _store.read();
    if (t != null) await ref.read(apiClientProvider).logout(t.refresh);
    await _store.clear();
    await stopBackgroundPolling();
    state = SessionStatus.signedOut;
  }

  void expired() {
    if (state == SessionStatus.signedIn) state = SessionStatus.signedOut;
  }
}

final sessionProvider = NotifierProvider<SessionNotifier, SessionStatus>(SessionNotifier.new);

// --- Biometric lock --------------------------------------------------------------------------

class LockNotifier extends Notifier<bool> {
  @override
  bool build() => true; // locked on every cold start

  void lock() => state = true;
  void unlock() => state = false;
}

final lockProvider = NotifierProvider<LockNotifier, bool>(LockNotifier.new);

enum BiometricResult { success, failed, unsupported }

class BiometricService {
  final _auth = LocalAuthentication();

  /// Biometrics first; the device PIN/pattern is accepted as fallback. Devices with no screen lock at all are refused.
  Future<BiometricResult> authenticate() async {
    try {
      if (!await _auth.isDeviceSupported()) return BiometricResult.unsupported;
      final ok = await _auth.authenticate(localizedReason: 'Desbloquea CommerceHub Admin');
      return ok ? BiometricResult.success : BiometricResult.failed;
    } catch (_) {
      return BiometricResult.failed;
    }
  }
}

final biometricProvider = Provider<BiometricService>((ref) => BiometricService());

// --- Orders ----------------------------------------------------------------------------------

class StatusFilter extends Notifier<String?> {
  @override
  String? build() => null; // all

  void select(String? status) => state = status;
}

final statusFilterProvider = NotifierProvider<StatusFilter, String?>(StatusFilter.new);

class OrdersState {
  const OrdersState(this.items, this.page, this.hasNext, {this.loadingMore = false});
  final List<OrderSummary> items;
  final int page;
  final bool hasNext;
  final bool loadingMore;
}

class OrdersNotifier extends AsyncNotifier<OrdersState> {
  @override
  Future<OrdersState> build() async {
    final status = ref.watch(statusFilterProvider);
    final p = await ref.read(ordersRepositoryProvider).list(status: status);
    return OrdersState(p.items, p.page, p.hasNext);
  }

  Future<void> refresh() async {
    ref.invalidateSelf();
    await future;
  }

  Future<void> loadMore() async {
    final cur = state.value;
    if (cur == null || !cur.hasNext || cur.loadingMore) return;
    state = AsyncData(OrdersState(cur.items, cur.page, cur.hasNext, loadingMore: true));
    try {
      final p = await ref.read(ordersRepositoryProvider).list(status: ref.read(statusFilterProvider), page: cur.page + 1);
      state = AsyncData(OrdersState([...cur.items, ...p.items], p.page, p.hasNext));
    } catch (_) {
      state = AsyncData(OrdersState(cur.items, cur.page, cur.hasNext));
    }
  }
}

final ordersProvider = AsyncNotifierProvider<OrdersNotifier, OrdersState>(OrdersNotifier.new);

final orderDetailProvider = FutureProvider.autoDispose.family<OrderDetail, String>(
  (ref, id) => ref.watch(ordersRepositoryProvider).detail(id),
);

/// Foreground poller: while the app is open and signed in, checks every minute for new orders.
final newOrderPollerProvider = Provider<void>((ref) {
  final watcher = NewOrderWatcher(ref.watch(ordersRepositoryProvider), ref.watch(tokenStoreProvider));

  Future<void> tick() async {
    try {
      final fresh = await watcher.check();
      if (fresh.isNotEmpty) {
        await showNewOrders(fresh);
        ref.invalidate(ordersProvider);
      }
    } catch (_) {
      // offline / transient: next tick
    }
  }

  unawaited(tick()); // sets the baseline on first run
  final timer = Timer.periodic(foregroundPollInterval, (_) => tick());
  ref.onDispose(timer.cancel);
});
