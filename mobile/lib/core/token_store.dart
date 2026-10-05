import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class Tokens {
  const Tokens({required this.access, required this.refresh});
  final String access;
  final String refresh;
}

/// Keychain/Keystore-backed storage for the session and the "last seen order" cursor.
class TokenStore {
  TokenStore([FlutterSecureStorage? storage])
      : _s = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _s;

  static const _kAccess = 'access_token';
  static const _kRefresh = 'refresh_token';
  static const _kLastSeen = 'last_seen_order_utc';

  Future<Tokens?> read() async {
    final a = await _s.read(key: _kAccess);
    final r = await _s.read(key: _kRefresh);
    return (a == null || r == null) ? null : Tokens(access: a, refresh: r);
  }

  Future<void> save(Tokens t) async {
    await _s.write(key: _kAccess, value: t.access);
    await _s.write(key: _kRefresh, value: t.refresh);
  }

  Future<void> clear() async {
    await _s.delete(key: _kAccess);
    await _s.delete(key: _kRefresh);
    await _s.delete(key: _kLastSeen);
  }

  Future<DateTime?> readLastSeen() async {
    final v = await _s.read(key: _kLastSeen);
    return v == null ? null : DateTime.tryParse(v);
  }

  Future<void> writeLastSeen(DateTime utc) =>
      _s.write(key: _kLastSeen, value: utc.toUtc().toIso8601String());
}

/// Decodes (without verifying) a JWT payload. The server verifies; this is only used to read claims for UI gating.
Map<String, dynamic> decodeJwtPayload(String jwt) {
  final parts = jwt.split('.');
  if (parts.length != 3) return const {};
  final json = utf8.decode(base64Url.decode(base64Url.normalize(parts[1])));
  return jsonDecode(json) as Map<String, dynamic>;
}

/// Reads a claim that may be a single string or an array of strings.
Set<String> claimValues(Map<String, dynamic> payload, String name) {
  final v = payload[name];
  if (v is String) return {v};
  if (v is List) return v.map((e) => e.toString()).toSet();
  return {};
}
