import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:commercehub_admin/core/api_client.dart';
import 'package:commercehub_admin/core/token_store.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

class MemoryTokenStore extends TokenStore {
  Tokens? tokens;
  @override
  Future<Tokens?> read() async => tokens;
  @override
  Future<void> save(Tokens t) async => tokens = t;
  @override
  Future<void> clear() async => tokens = null;
}

typedef Handler = FutureOr<ResponseBody> Function(RequestOptions o);

class FakeAdapter implements HttpClientAdapter {
  FakeAdapter(this.handler);
  final Handler handler;
  @override
  void close({bool force = false}) {}
  @override
  Future<ResponseBody> fetch(RequestOptions o, Stream<Uint8List>? body, Future<void>? cancel) async => handler(o);
}

ResponseBody json(int status, Object body) => ResponseBody.fromString(jsonEncode(body), status,
    headers: {Headers.contentTypeHeader: ['application/json']});

ApiClient makeClient(MemoryTokenStore store, Handler handler,
    {bool canRefresh = true, void Function()? onExpired}) {
  Dio mk() => Dio(BaseOptions(baseUrl: 'http://test', validateStatus: (s) => s != null && s < 400))
    ..httpClientAdapter = FakeAdapter(handler);
  return ApiClient(store, canRefresh: canRefresh, onSessionExpired: onExpired, dio: mk(), bare: mk());
}

void main() {
  late MemoryTokenStore store;
  setUp(() => store = MemoryTokenStore()..tokens = const Tokens(access: 'old', refresh: 'r0'));

  ResponseBody orders(RequestOptions o) =>
      o.headers['Authorization'] == 'Bearer new' ? json(200, {'ok': true}) : json(401, {});

  test('a transient refresh failure (503 / offline) keeps the session', () async {
    var expired = false;
    final client = makeClient(store, (o) {
      if (o.path.endsWith('/refresh')) return json(503, {});
      return json(401, {});
    }, onExpired: () => expired = true);

    await expectLater(client.get('/api/v1/orders'), throwsA(isA<ApiException>()));
    expect(store.tokens?.refresh, 'r0');
    expect(expired, isFalse);
  });

  test('a 429 on refresh keeps the session', () async {
    final client = makeClient(store, (o) => o.path.endsWith('/refresh') ? json(429, {}) : json(401, {}));
    await expectLater(client.get('/api/v1/orders'), throwsA(isA<ApiException>()));
    expect(store.tokens, isNotNull);
  });

  test('a rejected refresh token ends the session', () async {
    var expired = false;
    final client = makeClient(store, (o) => json(401, {}), onExpired: () => expired = true);
    await expectLater(client.get('/api/v1/orders'), throwsA(isA<ApiException>()));
    expect(store.tokens, isNull);
    expect(expired, isTrue);
  });

  test('parallel 401s refresh exactly once and all succeed', () async {
    var refreshes = 0;
    final client = makeClient(store, (o) async {
      if (o.path.endsWith('/refresh')) {
        refreshes++;
        await Future<void>.delayed(const Duration(milliseconds: 20));
        return json(200, {'accessToken': 'new', 'refreshToken': 'r1'});
      }
      return orders(o);
    });

    final results = await Future.wait([client.get('/a'), client.get('/b'), client.get('/c')]);
    expect(results.length, 3);
    expect(refreshes, 1);
    expect(store.tokens?.refresh, 'r1');
  });

  test('a request that raced a refresh by another client just retries with the stored token', () async {
    var refreshes = 0;
    final client = makeClient(store, (o) {
      if (o.path.endsWith('/refresh')) {
        refreshes++;
        return json(200, {'accessToken': 'x', 'refreshToken': 'x'});
      }
      if (o.headers['Authorization'] == 'Bearer old') {
        store.tokens = const Tokens(access: 'new', refresh: 'r1'); // another isolate renewed it meanwhile
        return json(401, {});
      }
      return orders(o);
    });

    await client.get('/a');
    expect(refreshes, 0);
  });

  test('the background client never spends the refresh token', () async {
    var refreshes = 0;
    final client = makeClient(store, (o) {
      if (o.path.endsWith('/refresh')) refreshes++;
      return json(401, {});
    }, canRefresh: false);

    await expectLater(client.get('/api/v1/orders'), throwsA(isA<ApiException>()));
    expect(refreshes, 0);
    expect(store.tokens?.refresh, 'r0');
  });
}
