part of 'feed_cubit.dart';

sealed class FeedState extends Equatable {
  const FeedState();

  @override
  List<Object?> get props => [];
}

final class FeedInitial extends FeedState {
  const FeedInitial();
}

final class FeedLoading extends FeedState {
  const FeedLoading();
}

final class FeedLoaded extends FeedState {
  const FeedLoaded({
    required this.items,
    required this.ads,
    this.hasMore = false,
    this.loadingMore = false,
    this.loadMoreError,
  });

  final List<FeedItemDto> items;
  final List<AdDto> ads;
  final bool hasMore;
  final bool loadingMore;
  final String? loadMoreError;

  FeedLoaded copyWith({
    List<FeedItemDto>? items,
    List<AdDto>? ads,
    bool? hasMore,
    bool? loadingMore,
    String? loadMoreError,
  }) {
    return FeedLoaded(
      items: items ?? this.items,
      ads: ads ?? this.ads,
      hasMore: hasMore ?? this.hasMore,
      loadingMore: loadingMore ?? this.loadingMore,
      loadMoreError: loadMoreError,
    );
  }

  @override
  List<Object?> get props => [items, ads, hasMore, loadingMore, loadMoreError];
}

final class FeedError extends FeedState {
  const FeedError(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}
