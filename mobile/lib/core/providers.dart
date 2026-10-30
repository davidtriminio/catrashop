import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:local_auth/local_auth.dart';

import '../features/orders/order_models.dart';
import '../features/orders/orders_repository.dart';
import 'api_client.dart';
import 'api_url.dart';
import 'config.dart';
import 'notifications.dart';
import 'token_store.dart';

final tokenStoreProvider = Provider<TokenStore>((ref) => TokenStore());

/// The address saved on this device (or the built-in one), read once before the app starts.
final initialApiUrlProvider = Provider<String>((ref) => apiBaseUrl);

final apiUrlStoreProvider = Provider<ApiUrlStore>((ref) => ApiUrlStore());

/// Lets tests (and nothing else) replace the real network check made by the "Probar conexión" button.
final apiReachableProvider = Provider<Future<bool> Function(String)>((ref) => checkApiReachable);

/// The API address in use. Changing it signs the admin out (the session belongs to the other server), remembers the
/// new address on the device and makes every request go there.
class ApiUrlNotifier extends Notifier<String> {
  @override
  String build() => ref.watch(initialApiUrlProvider);

  /// True when the app is using the address it was built with (nothing saved).
  bool get isBuiltIn => state == apiBaseUrl;

  /// Saves [input] as the API address. Returns false, changing nothing, when it is not a valid http(s) URL.
  Future<bool> change(String input) async {
    final url = normalizeApiUrl(input);
    if (url == null) return false;
    if (url == state) return true;
    await _switchTo(url, save: true);
    return true;
  }

  /// Goes back to the address the app was built with and forgets the saved one.
  Future<void> reset() async {
    if (isBuiltIn) {
      await ref.read(apiUrlStoreProvider).clear();
      return;
    }
    await _switchTo(apiBaseUrl, save: false);
  }

  Future<void> _switchTo(String url, {required bool save}) async {
    await ref.read(sessionProvider.notifier).switchServer(); // while the old address is still the one in use
    final store = ref.read(apiUrlStoreProvider);
    if (save) {
      await store.write(url);
    } else {
      await store.clear();
    }
    state = url;
    ref.invalidate(ordersProvider);
    ref.invalidate(summaryProvider);
    ref.invalidate(profileProvider);
  }
}

final apiUrlProvider = NotifierProvider<ApiUrlNotifier, String>(ApiUrlNotifier.new);

final apiClientProvider = Provider<ApiClient>((ref) {
  return ApiClient(
    ref.watch(tokenStoreProvider),
    baseUrl: ref.watch(apiUrlProvider),
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

  /// Ends the session because the API address is changing: the tokens belong to the old server, so sign out there
  /// (best effort, it may be unreachable) and forget them here.
  Future<void> switchServer() async {
    try {
      final t = await _store.read();
      if (t != null) await ref.read(apiClientProvider).logout(t.refresh);
    } catch (_) {
      // the old server may be down or gone: leaving anyway
    }
    await _store.clear();
    try {
      await stopBackgroundPolling();
    } catch (_) {
      // no background scheduler (e.g. in tests)
    }
    state = SessionStatus.signedOut;
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
    // Debug-only escape hatch so the app can be exercised on an emulator with no screen lock:
    // flutter run --dart-define=SKIP_BIOMETRIC=true . Compiled out of release builds.
    if (kDebugMode && const bool.fromEnvironment('SKIP_BIOMETRIC')) return BiometricResult.success;
    try {
      if (!await _auth.isDeviceSupported()) return BiometricResult.unsupported;
      final ok = await _auth.authenticate(localizedReason: 'Desbloquea Catrashop Admin');
      return ok ? BiometricResult.success : BiometricResult.failed;
    } catch (_) {
      return BiometricResult.failed;
    }
  }
}

final biometricProvider = Provider<BiometricService>((ref) => BiometricService());

// --- Retrying a failed load ------------------------------------------------------------------

/// What the last "Reintentar" press was: how many times in a row and when, so a failure that comes back instantly
/// still looks like a new attempt instead of the same message.
class RetryInfo {
  const RetryInfo(this.attempt, this.at);
  final int attempt;
  final DateTime at;
}

class RetryInfoNotifier extends Notifier<RetryInfo?> {
  @override
  RetryInfo? build() => null;

  void mark() => state = RetryInfo((state?.attempt ?? 0) + 1, DateTime.now());
  void clear() => state = null;
}

final retryInfoProvider = NotifierProvider<RetryInfoNotifier, RetryInfo?>(RetryInfoNotifier.new);

/// After a retry press the loading state must be visible for a moment, even when the server answers (or fails) at once.
Future<void> _showRetryProgress(Ref ref) async {
  if (ref.read(retryInfoProvider) != null) await Future<void>.delayed(const Duration(milliseconds: 600));
}

void _loadSucceeded(Ref ref) {
  try {
    ref.read(retryInfoProvider.notifier).clear();
  } catch (_) {
    // the provider is being torn down
  }
}

// --- Orders ----------------------------------------------------------------------------------

class StatusFilter extends Notifier<String?> {
  @override
  String? build() => null; // all

  void select(String? status) => state = status;
}

final statusFilterProvider = NotifierProvider<StatusFilter, String?>(StatusFilter.new);

class OrdersState {
  const OrdersState(this.items, this.page, this.hasNext, {this.loadingMore = false, this.totalCount = 0});
  final List<OrderSummary> items;
  final int page;
  final bool hasNext;
  final bool loadingMore;
  final int totalCount;
}

class OrdersNotifier extends AsyncNotifier<OrdersState> {
  @override
  Future<OrdersState> build() async {
    final status = ref.watch(statusFilterProvider);
    await _showRetryProgress(ref);
    final p = await ref.read(ordersRepositoryProvider).list(status: status);
    _loadSucceeded(ref);
    return OrdersState(p.items, p.page, p.hasNext, totalCount: p.totalCount);
  }

  Future<void> refresh() async {
    ref.invalidateSelf();
    await future;
  }

  Future<void> loadMore() async {
    final cur = state.value;
    if (cur == null || !cur.hasNext || cur.loadingMore) return;
    state = AsyncData(OrdersState(cur.items, cur.page, cur.hasNext, loadingMore: true, totalCount: cur.totalCount));
    try {
      final p = await ref.read(ordersRepositoryProvider).list(status: ref.read(statusFilterProvider), page: cur.page + 1);
      state = AsyncData(OrdersState([...cur.items, ...p.items], p.page, p.hasNext, totalCount: p.totalCount));
    } catch (_) {
      state = AsyncData(OrdersState(cur.items, cur.page, cur.hasNext, totalCount: cur.totalCount));
    }
  }
}

final ordersProvider = AsyncNotifierProvider<OrdersNotifier, OrdersState>(OrdersNotifier.new);

final orderDetailProvider = FutureProvider.autoDispose.family<OrderDetail, String>((ref, id) async {
  await _showRetryProgress(ref);
  final detail = await ref.watch(ordersRepositoryProvider).detail(id);
  _loadSucceeded(ref);
  return detail;
});

/// Foreground poller: while the app is open and signed in, checks every minute for new orders.
final newOrderPollerProvider = Provider<void>((ref) {
  final watcher = NewOrderWatcher(ref.watch(ordersRepositoryProvider), ref.watch(tokenStoreProvider));

  Future<void> tick() async {
    try {
      final fresh = await watcher.check();
      if (fresh.isNotEmpty) {
        await showNewOrders(fresh);
        ref.invalidate(ordersProvider);
        ref.invalidate(summaryProvider);
      }
    } catch (_) {
      // offline / transient: next tick
    }
  }

  unawaited(tick()); // sets the baseline on first run
  final timer = Timer.periodic(foregroundPollInterval, (_) => tick());
  ref.onDispose(timer.cancel);
});

// --- Navigation + summary + profile ----------------------------------------------------------

/// Index of the selected bottom-navigation tab (0 orders, 1 summary, 2 settings).
class TabIndex extends Notifier<int> {
  @override
  int build() => 0;

  void select(int index) => state = index;
}

final tabIndexProvider = NotifierProvider<TabIndex, int>(TabIndex.new);

/// Order counts per status for the summary screen (counts only, no order payloads).
class OrderSummaryCounts {
  const OrderSummaryCounts({required this.total, required this.byStatus});
  final int total;
  final Map<String, int> byStatus;

  int of(String status) => byStatus[status] ?? 0;
}

final summaryProvider = FutureProvider.autoDispose<OrderSummaryCounts>((ref) async {
  final repo = ref.watch(ordersRepositoryProvider);
  final statuses = OrderStatus.filters;
  await _showRetryProgress(ref);
  final results = await Future.wait([
    repo.count(),
    for (final s in statuses) repo.count(status: s),
  ]);
  _loadSucceeded(ref);
  return OrderSummaryCounts(
    total: results.first,
    byStatus: {for (var i = 0; i < statuses.length; i++) statuses[i]: results[i + 1]},
  );
});

/// Who is signed in, read from the stored access token (display only; the server enforces access).
class ProfileInfo {
  const ProfileInfo({required this.email, required this.roles});
  final String email;
  final Set<String> roles;
}

final profileProvider = FutureProvider.autoDispose<ProfileInfo>((ref) async {
  final tokens = await ref.watch(tokenStoreProvider).read();
  if (tokens == null) return const ProfileInfo(email: '', roles: {});
  final payload = decodeJwtPayload(tokens.access);
  return ProfileInfo(email: (payload['email'] as String?) ?? '', roles: claimValues(payload, 'role'));
});
