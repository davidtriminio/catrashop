import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'config.dart';

/// Cleans a typed API address: it must be an absolute http(s) URL with a host, and loses any trailing slash.
/// Returns null when it is not usable (so the caller can say so instead of saving garbage).
String? normalizeApiUrl(String input) {
  final text = input.trim();
  final uri = Uri.tryParse(text);
  if (uri == null || !(uri.scheme == 'http' || uri.scheme == 'https') || uri.host.isEmpty) return null;
  if (uri.hasQuery || uri.fragment.isNotEmpty || uri.userInfo.isNotEmpty) return null;
  return text.replaceAll(RegExp(r'/+$'), '');
}

/// Remembers the API address the admin chose on this device (Keychain/Keystore, like the session), so it survives
/// closing the app. Without a saved value the app uses the address it was built with ([apiBaseUrl]).
class ApiUrlStore {
  ApiUrlStore([FlutterSecureStorage? storage]) : _s = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _s;
  static const _key = 'api_base_url';

  Future<String?> read() => _s.read(key: _key);
  Future<void> write(String url) => _s.write(key: _key, value: url);
  Future<void> clear() => _s.delete(key: _key);
}

/// The address to talk to: the saved one when it is still valid, otherwise the one the app was built with.
/// Also used by the background isolate, which has no Riverpod.
Future<String> resolveApiUrl([ApiUrlStore? store]) async {
  try {
    final saved = await (store ?? ApiUrlStore()).read();
    return (saved == null ? null : normalizeApiUrl(saved)) ?? apiBaseUrl;
  } catch (_) {
    return apiBaseUrl; // storage unavailable: never fail to start because of it
  }
}

/// Asks the server at [url] whether it is alive (`/health/live`). Any failure is reported as `false`.
Future<bool> checkApiReachable(String url) async {
  try {
    final dio = Dio(BaseOptions(
      baseUrl: url,
      connectTimeout: const Duration(seconds: 6),
      receiveTimeout: const Duration(seconds: 6),
      validateStatus: (_) => true,
    ));
    final r = await dio.get<dynamic>('/health/live');
    return r.statusCode == 200;
  } catch (_) {
    return false;
  }
}
