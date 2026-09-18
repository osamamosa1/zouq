import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:zouq/features/auth/data/auth_repository.dart';
import 'package:zouq/features/auth/domain/user_entity.dart';

part 'auth_state.dart';

class AuthCubit extends Cubit<AuthState> {
  AuthCubit(this._repository) : super(AuthInitial());

  final AuthRepository _repository;

  Future<void> checkSession() async {
    final loggedIn = await _repository.hasSession();
    emit(loggedIn ? AuthAuthenticated() : AuthUnauthenticated());
  }

  Future<void> login(String email, String password) async {
    emit(AuthLoading());
    try {
      final user = await _repository.login(email: email, password: password);
      emit(AuthAuthenticated(user: user));
    } catch (e) {
      emit(AuthFailure(e.toString()));
      emit(AuthUnauthenticated());
    }
  }

  Future<void> register({
    required String name,
    required String email,
    required String password,
    String? phone,
  }) async {
    emit(AuthLoading());
    try {
      final user = await _repository.register(
        name: name,
        email: email,
        password: password,
        phone: phone,
      );
      emit(AuthAuthenticated(user: user));
    } catch (e) {
      emit(AuthFailure(e.toString()));
      emit(AuthUnauthenticated());
    }
  }

  Future<void> logout() async {
    await _repository.logout();
    emit(AuthUnauthenticated());
  }
}
