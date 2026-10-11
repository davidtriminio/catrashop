import 'dart:async';

import 'package:dio/dio.dart';

import 'config.dart';
import 'token_store.dart';

class ApiException implements Exception {
  ApiException(this.message, {this.statusCode});
  final String message;
  final int? statusCode;
  @override
  String toString() => message;
}

/// Dio wrapper that attaches the bearer token and transparently performs one rotating-refresh on 401.
/// Has no Flutter/Riverpod dependency so the background isolate (WorkManager) can use it too.
class ApiClient {
  ApiClient(this._store, {this.onSessionExpired, String? baseUrl})
      : _dio = Dio(BaseOptions(
          baseUrl: baseUrl ?? apiBaseUrl,
          connectTimeout: const Duration(seconds: 15),
          receiveTimeout: const Duration(seconds: 20),
          headers: {'Accept': 'application/json'},
        )),
        _bare = Dio(BaseOptions(
          baseUrl: baseUrl ?? apiBaseUrl,
          connectTimeout: const Duration(seconds: 15),
          receiveTimeout: const Duration(seconds: 20),
        )) {
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (o, h) async {
        final t = await _store.read();
        if (t != null) o.headers['Authorization'] = 'Bearer ${t.access}';
        h.next(o);
      },
      onError: (e, h) async {
        final req = e.requestOptions;
        if (e.response?.statusCode == 401 && req.extra['retried'] != true) {
          if (await _refresh()) {
            req.extra['retried'] = true;
            final t = await _store.read();
            req.headers['Authorization'] = 'Bearer ${t!.access}';
            try {
              return h.resolve(await _dio.fetch(req));
            } on DioException catch (e2) {
              return h.next(e2);
            }
          }
          await _store.clear();
          onSessionExpired?.call();
        }
        h.next(e);
      },
    ));
  }

  final TokenStore _store;
  final void Function()? onSessionExpired;
  final Dio _dio;
  final Dio _bare; // no interceptors: used for login/refresh
  Future<bool>? _refreshing;

  /// Single-flight refresh so parallel 401s don't burn the single-use refresh token twice.
  Future<bool> _refresh() => _refreshing ??= _doRefresh().whenComplete(() => _refreshing = null);

  Future<bool> _doRefresh() async {
    final t = await _store.read();
    if (t == null) return false;
    try {
      final r = await _bare.post('/api/v1/identity/refresh', data: {'refreshToken': t.refresh});
      final d = r.data as Map<String, dynamic>;
      await _store.save(Tokens(access: d['accessToken'] as String, refresh: d['refreshToken'] as String));
      return true;
    } on DioException {
      return false;
    }
  }

  Future<Map<String, dynamic>> login(String email, String password) async {
    try {
      final r = await _bare.post('/api/v1/identity/login', data: {'email': email, 'password': password});
      return r.data as Map<String, dynamic>;
    } on DioException catch (e) {
      throw _toException(e);
    }
  }

  Future<void> logout(String refreshToken) async {
    try {
      await _bare.post('/api/v1/identity/logout', data: {'refreshToken': refreshToken});
    } on DioException {
      // best effort: the local session is cleared regardless
    }
  }

  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    try {
      return (await _dio.get(path, queryParameters: query)).data;
    } on DioException catch (e) {
      throw _toException(e);
    }
  }

  Future<dynamic> post(String path, {Object? body}) async {
    try {
      return (await _dio.post(path, data: body)).data;
    } on DioException catch (e) {
      throw _toException(e);
    }
  }

  ApiException _toException(DioException e) {
    final code = e.response?.statusCode;
    final data = e.response?.data;
    if (data is Map) {
      final detail = data['detail'] ?? data['title'];
      if (detail is String && detail.isNotEmpty) return ApiException(detail, statusCode: code);
      final errors = data['errors'];
      if (errors is Map && errors.isNotEmpty) {
        final first = errors.values.first;
        return ApiException(first is List && first.isNotEmpty ? '${first.first}' : '$first', statusCode: code);
      }
    }
    return switch (code) {
      401 => ApiException('Credenciales inválidas o sesión expirada.', statusCode: code),
      403 => ApiException('No tienes permiso para esta acción.', statusCode: code),
      429 => ApiException('Demasiados intentos. Espera un momento.', statusCode: code),
      null => ApiException('Sin conexión con el servidor.'),
      _ => ApiException('Error del servidor ($code).', statusCode: code),
    };
  }
}
