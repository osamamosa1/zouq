import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:zouq/features/auth/data/auth_dtos.dart';
import 'package:zouq/features/profile/data/profile_repository.dart';

part 'profile_state.dart';

class ProfileCubit extends Cubit<ProfileState> {
  ProfileCubit(this._repository) : super(const ProfileInitial());

  final ProfileRepository _repository;

  Future<void> load() async {
    emit(const ProfileLoading());
    try {
      final user = await _repository.profile();
      final balance = await _repository.balance();
      emit(ProfileLoaded(user: user, balance: balance.balance, ledger: balance.ledger));
    } catch (e) {
      emit(ProfileError(e.toString()));
    }
  }
}
