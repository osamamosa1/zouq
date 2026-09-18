import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:zouq/features/design_editor/data/design_dtos.dart';
import 'package:zouq/features/design_editor/data/design_repository.dart';

part 'my_designs_state.dart';

enum MyDesignsFilter { all, drafts, readyToPublish, published }

class MyDesignsCubit extends Cubit<MyDesignsState> {
  MyDesignsCubit(this._repository) : super(const MyDesignsInitial());

  final DesignRepository _repository;

  Future<void> load() async {
    emit(const MyDesignsLoading());
    try {
      final designs = await _repository.myDesigns();
      emit(MyDesignsLoaded(designs: designs));
    } catch (e) {
      emit(MyDesignsError(e.toString()));
    }
  }

  void setFilter(MyDesignsFilter filter) {
    final current = state;
    if (current is MyDesignsLoaded) {
      emit(current.copyWith(filter: filter));
    }
  }

  Future<void> publish(String designId) async {
    final current = state;
    if (current is! MyDesignsLoaded) return;
    emit(current.copyWith(busyId: designId, clearActionError: true));
    try {
      await _repository.publish(designId);
      await load();
    } catch (e) {
      final ready = state;
      if (ready is MyDesignsLoaded) {
        emit(ready.copyWith(clearBusy: true, actionError: e.toString()));
      }
    }
  }

  Future<void> unpublish(String designId) async {
    final current = state;
    if (current is! MyDesignsLoaded) return;
    emit(current.copyWith(busyId: designId, clearActionError: true));
    try {
      await _repository.unpublish(designId);
      await load();
    } catch (e) {
      final ready = state;
      if (ready is MyDesignsLoaded) {
        emit(ready.copyWith(clearBusy: true, actionError: e.toString()));
      }
    }
  }
}
