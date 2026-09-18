import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:zouq/core/api/api_client.dart';
import 'package:zouq/core/error/exceptions.dart';
import 'package:zouq/features/auth/data/auth_dtos.dart';
import 'package:zouq/features/auth/domain/user_entity.dart';

class AuthRepository {
  AuthRepository(this._api, this._storage);

  final ApiClient _api;
  final FlutterSecureStorage _storage;

  Future<bool> hasSession() async {
    final t = await _storage.read(key: 'access_token');
    return t != null && t.isNotEmpty;
  }

  bool get isLoggedIn => false; // sync check not available with secure storage

  Future<bool> isLoggedInAsync() => hasSession();

  Future<UserEntity> login({required String email, required String password}) async {
    try {
      final res = await _api.dio.post('/api/auth/login', data: {
        'email': email,
        'password': password,
      });
      final token = await _persistToken(res);
      return token.user.toEntity();
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Login failed', statusCode: e.response?.statusCode);
    }
  }

  Future<UserEntity> register({
    required String name,
    required String email,
    required String password,
    String? phone,
  }) async {
    try {
      final res = await _api.dio.post('/api/auth/register', data: {
        'name': name,
        'email': email,
        'password': password,
        if (phone != null && phone.isNotEmpty) 'phone': phone,
      });
      final token = await _persistToken(res);
      return token.user.toEntity();
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Registration failed', statusCode: e.response?.statusCode);
    }
  }

  Future<AuthTokenDto> _persistToken(Response<dynamic> res) async {
    final data = await _api.unwrap(res, (d) => Map<String, dynamic>.from(d as Map));
    final token = AuthTokenDto.fromJson(data);
    await _storage.write(key: 'access_token', value: token.accessToken);
    await _storage.write(key: 'refresh_token', value: token.refreshToken);
    await _storage.write(key: 'user_name', value: token.user.name);
    return token;
  }

  Future<void> logout() async {
    await _storage.deleteAll();
  }
}
