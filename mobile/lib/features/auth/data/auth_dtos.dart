import 'package:zouq/features/auth/domain/user_entity.dart';

class AuthTokenDto {
  AuthTokenDto({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.user,
  });

  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final UserDto user;

  factory AuthTokenDto.fromJson(Map<String, dynamic> json) {
    return AuthTokenDto(
      accessToken: json['access_token'] as String,
      refreshToken: json['refresh_token'] as String,
      expiresIn: (json['expires_in'] as num).toInt(),
      user: UserDto.fromJson(json['user'] as Map<String, dynamic>),
    );
  }
}

class UserDto {
  UserDto({
    required this.id,
    required this.name,
    required this.email,
    this.phone,
    required this.role,
    this.avatarUrl,
    required this.balance,
  });

  final String id;
  final String name;
  final String email;
  final String? phone;
  final String role;
  final String? avatarUrl;
  final double balance;

  factory UserDto.fromJson(Map<String, dynamic> json) {
    return UserDto(
      id: json['id'].toString(),
      name: json['name'] as String,
      email: json['email'] as String,
      phone: json['phone'] as String?,
      role: json['role'] as String,
      avatarUrl: json['avatar_url'] as String?,
      balance: (json['balance'] as num).toDouble(),
    );
  }

  UserEntity toEntity() => UserEntity(
        id: id,
        name: name,
        email: email,
        phone: phone,
        role: role,
        avatarUrl: avatarUrl,
        balance: balance,
      );
}
