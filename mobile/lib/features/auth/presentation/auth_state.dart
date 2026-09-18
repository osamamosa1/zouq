part of 'auth_cubit.dart';

sealed class AuthState extends Equatable {
  @override
  List<Object?> get props => [];
}

final class AuthInitial extends AuthState {}

final class AuthLoading extends AuthState {}

final class AuthAuthenticated extends AuthState {
  AuthAuthenticated({this.user});
  final UserEntity? user;

  @override
  List<Object?> get props => [user];
}

final class AuthUnauthenticated extends AuthState {}

final class AuthFailure extends AuthState {
  AuthFailure(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}
