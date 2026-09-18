class UserEntity {
  const UserEntity({
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
}
