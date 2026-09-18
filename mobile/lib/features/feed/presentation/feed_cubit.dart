import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:zouq/features/feed/data/feed_dtos.dart';
import 'package:zouq/features/feed/data/feed_repository.dart';

part 'feed_state.dart';

class FeedCubit extends Cubit<FeedState> {
  FeedCubit(this._repository) : super(const FeedInitial());

  final FeedRepository _repository;
  static const _pageSize = 20;

  Future<void> load({bool refresh = true}) async {
    if (refresh) emit(const FeedLoading());
    try {
      final items = await _repository.forYou(take: _pageSize, skip: 0);
      final ads = await _repository.ads();
      emit(FeedLoaded(
        items: items,
        ads: ads,
        hasMore: items.length >= _pageSize,
        loadingMore: false,
      ));
    } catch (e) {
      emit(FeedError(e.toString()));
    }
  }

  Future<void> loadMore() async {
    final current = state;
    if (current is! FeedLoaded || current.loadingMore || !current.hasMore) return;
    emit(current.copyWith(loadingMore: true));
    try {
      final next = await _repository.forYou(take: _pageSize, skip: current.items.length);
      final seen = current.items.map((e) => e.designId).toSet();
      final merged = [
        ...current.items,
        ...next.where((e) => !seen.contains(e.designId)),
      ];
      emit(current.copyWith(
        items: merged,
        hasMore: next.length >= _pageSize,
        loadingMore: false,
      ));
    } catch (e) {
      emit(current.copyWith(loadingMore: false, loadMoreError: e.toString()));
    }
  }
}
