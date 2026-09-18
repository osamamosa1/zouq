part of 'profile_cubit.dart';

sealed class ProfileState extends Equatable {
  const ProfileState();

  @override
  List<Object?> get props => [];
}

final class ProfileInitial extends ProfileState {
  const ProfileInitial();
}

final class ProfileLoading extends ProfileState {
  const ProfileLoading();
}

final class ProfileLoaded extends ProfileState {
  const ProfileLoaded({
    required this.user,
    required this.balance,
    required this.ledger,
  });

  final UserDto user;
  final double balance;
  final List<Map<String, dynamic>> ledger;

  @override
  List<Object?> get props => [user.id, balance, ledger.length];
}

final class ProfileError extends ProfileState {
  const ProfileError(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}
