import 'package:dio/dio.dart';
import 'package:zouq/core/api/api_client.dart';
import 'package:zouq/core/error/exceptions.dart';
import 'package:zouq/features/auth/data/auth_dtos.dart';

class BalanceInfo {
  BalanceInfo({required this.balance, required this.ledger});

  final double balance;
  final List<Map<String, dynamic>> ledger;
}

class ProfileRepository {
  ProfileRepository(this._api);

  final ApiClient _api;

  Future<UserDto> profile() async {
    try {
      final res = await _api.dio.get('/api/me');
      return _api.unwrap(
        res,
        (d) => UserDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }

  Future<BalanceInfo> balance() async {
    try {
      final res = await _api.dio.get('/api/me/balance');
      final data = await _api.unwrap(res, (d) => Map<String, dynamic>.from(d as Map));
      final ledger = (data['ledger'] as List<dynamic>?)
              ?.map((e) => Map<String, dynamic>.from(e as Map))
              .toList() ??
          const [];
      return BalanceInfo(
        balance: (data['balance'] as num).toDouble(),
        ledger: ledger,
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }
}
