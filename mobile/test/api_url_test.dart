import 'package:catrashop_admin/core/api_url.dart';
import 'package:catrashop_admin/core/config.dart';
import 'package:catrashop_admin/core/providers.dart';
import 'package:catrashop_admin/core/theme.dart';
import 'package:catrashop_admin/core/token_store.dart';
import 'package:catrashop_admin/features/settings/settings_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'api_client_session_test.dart' show MemoryTokenStore;

class MemoryApiUrlStore extends ApiUrlStore {
  String? saved;
  @override
  Future<String?> read() async => saved;
  @override
  Future<void> write(String url) async => saved = url;
  @override
  Future<void> clear() async => saved = null;
}

class _ThrowingApiUrlStore extends ApiUrlStore {
  @override
  Future<String?> read() => throw StateError('no keystore');
}

const _custom = 'http://192.168.1.10:5227';

ProviderContainer container(MemoryApiUrlStore urls, MemoryTokenStore tokens, {String? initial}) {
  final c = ProviderContainer(overrides: [
    apiUrlStoreProvider.overrideWithValue(urls),
    tokenStoreProvider.overrideWithValue(tokens),
    initialApiUrlProvider.overrideWithValue(initial ?? apiBaseUrl),
  ]);
  addTearDown(c.dispose);
  return c;
}

void main() {
  group('normalizeApiUrl', () {
    test('accepts http and https addresses and drops trailing slashes and spaces', () {
      expect(normalizeApiUrl(' http://10.0.2.2:5227/ '), 'http://10.0.2.2:5227');
      expect(normalizeApiUrl('https://api.example.com//'), 'https://api.example.com');
      expect(normalizeApiUrl('https://example.com/api'), 'https://example.com/api');
    });

    test('rejects what is not a usable address', () {
      for (final bad in ['', '   ', '10.0.2.2:5227', 'ftp://example.com', 'http://', 'not a url', 'https://u:p@example.com', 'https://example.com?x=1']) {
        expect(normalizeApiUrl(bad), isNull, reason: bad);
      }
    });
  });

  group('resolveApiUrl', () {
    test('uses the saved address, else the built-in one', () async {
      final store = MemoryApiUrlStore();
      expect(await resolveApiUrl(store), apiBaseUrl);
      store.saved = _custom;
      expect(await resolveApiUrl(store), _custom);
    });

    test('ignores a corrupt saved value and never fails when storage is unavailable', () async {
      final store = MemoryApiUrlStore()..saved = 'garbage';
      expect(await resolveApiUrl(store), apiBaseUrl);
      expect(await resolveApiUrl(_ThrowingApiUrlStore()), apiBaseUrl);
    });
  });

  group('changing the API address', () {
    test('saves it, switches the client and signs the admin out', () async {
      final urls = MemoryApiUrlStore();
      final tokens = MemoryTokenStore()..tokens = const Tokens(access: 'a', refresh: 'r');
      final c = container(urls, tokens);

      expect(c.read(apiUrlProvider), apiBaseUrl);
      expect(await c.read(apiUrlProvider.notifier).change('$_custom/'), isTrue);

      expect(c.read(apiUrlProvider), _custom);
      expect(urls.saved, _custom);
      expect(tokens.tokens, isNull, reason: 'the old session belongs to the old server');
      expect(c.read(sessionProvider), SessionStatus.signedOut);
      expect(c.read(apiClientProvider), isNotNull);
    });

    test('an invalid address changes nothing', () async {
      final urls = MemoryApiUrlStore();
      final tokens = MemoryTokenStore()..tokens = const Tokens(access: 'a', refresh: 'r');
      final c = container(urls, tokens);

      expect(await c.read(apiUrlProvider.notifier).change('mi servidor'), isFalse);
      expect(c.read(apiUrlProvider), apiBaseUrl);
      expect(urls.saved, isNull);
      expect(tokens.tokens, isNotNull);
    });

    test('resetting goes back to the built-in address and forgets the saved one', () async {
      final urls = MemoryApiUrlStore()..saved = _custom;
      final tokens = MemoryTokenStore()..tokens = const Tokens(access: 'a', refresh: 'r');
      final c = container(urls, tokens, initial: _custom);

      await c.read(apiUrlProvider.notifier).reset();

      expect(c.read(apiUrlProvider), apiBaseUrl);
      expect(urls.saved, isNull);
      expect(tokens.tokens, isNull);
    });
  });

  group('Ajustes', () {
    Future<(MemoryApiUrlStore, MemoryTokenStore, ProviderContainer)> open(
      WidgetTester tester, {String? saved, Future<bool> Function(String)? reachable}) async {
      final urls = MemoryApiUrlStore()..saved = saved;
      final tokens = MemoryTokenStore()..tokens = const Tokens(access: 'a', refresh: 'r');
      tester.view.physicalSize = const Size(390 * 2, 844 * 2);
      tester.view.devicePixelRatio = 2;
      addTearDown(tester.view.reset);
      final c = ProviderContainer(overrides: [
        apiUrlStoreProvider.overrideWithValue(urls),
        tokenStoreProvider.overrideWithValue(tokens),
        initialApiUrlProvider.overrideWithValue(saved ?? apiBaseUrl),
        profileProvider.overrideWith((ref) async => const ProfileInfo(email: 'admin@catrashop.local', roles: {'Administrator'})),
        if (reachable != null) apiReachableProvider.overrideWithValue(reachable),
      ]);
      addTearDown(c.dispose);
      await tester.pumpWidget(UncontrolledProviderScope(
        container: c,
        child: MaterialApp(theme: AppTheme.light(), home: const SettingsPage()),
      ));
      await tester.pumpAndSettle();
      return (urls, tokens, c);
    }

    testWidgets('shows the current address and lets the admin change it', (tester) async {
      final (urls, tokens, c) = await open(tester);
      expect(find.textContaining(apiBaseUrl), findsOneWidget);
      expect(find.textContaining('Predeterminada de esta versión'), findsOneWidget);

      await tester.tap(find.text('Cambiar'));
      await tester.pumpAndSettle();
      expect(find.text('Servidor de la API'), findsOneWidget);
      expect(find.text('Restablecer'), findsNothing, reason: 'nothing to reset while the built-in address is used');

      await tester.enterText(find.byType(TextField), _custom);
      await tester.tap(find.text('Guardar'));
      await tester.pumpAndSettle();

      expect(c.read(apiUrlProvider), _custom);
      expect(urls.saved, _custom);
      expect(tokens.tokens, isNull);
      expect(find.text('Servidor cambiado. Inicia sesión de nuevo.'), findsOneWidget);
      expect(find.textContaining('Cambiada en este dispositivo'), findsOneWidget);
    });

    testWidgets('an invalid address shows an error and keeps the dialog open', (tester) async {
      final (urls, _, c) = await open(tester);
      await tester.tap(find.text('Cambiar'));
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextField), '192.168.1.10');
      await tester.tap(find.text('Guardar'));
      await tester.pumpAndSettle();

      expect(find.textContaining('Escribe una dirección completa'), findsOneWidget);
      expect(find.text('Servidor de la API'), findsOneWidget);
      expect(c.read(apiUrlProvider), apiBaseUrl);
      expect(urls.saved, isNull);
    });

    testWidgets('"Probar" reports whether the server answers without saving anything', (tester) async {
      final asked = <String>[];
      final (urls, _, _) = await open(tester, reachable: (url) async {
        asked.add(url);
        return url.contains('ok');
      });
      await tester.tap(find.text('Cambiar'));
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextField), 'http://down.local:5227/');
      await tester.tap(find.text('Probar'));
      await tester.pumpAndSettle();
      expect(find.text('No se pudo conectar con ese servidor.'), findsOneWidget);

      await tester.enterText(find.byType(TextField), 'http://ok.local:5227');
      await tester.tap(find.text('Probar'));
      await tester.pumpAndSettle();
      expect(find.text('El servidor responde.'), findsOneWidget);

      expect(asked, ['http://down.local:5227', 'http://ok.local:5227']);
      expect(urls.saved, isNull);
    });

    testWidgets('a changed address can be reset to the built-in one', (tester) async {
      final (urls, tokens, c) = await open(tester, saved: _custom);
      expect(find.textContaining('Cambiada en este dispositivo'), findsOneWidget);

      await tester.tap(find.text('Cambiar'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Restablecer'));
      await tester.pumpAndSettle();

      expect(c.read(apiUrlProvider), apiBaseUrl);
      expect(urls.saved, isNull);
      expect(tokens.tokens, isNull);
      expect(find.textContaining('Predeterminada de esta versión'), findsOneWidget);
    });
  });
}
