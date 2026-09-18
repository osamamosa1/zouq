part of 'my_designs_cubit.dart';

sealed class MyDesignsState extends Equatable {
  const MyDesignsState();

  @override
  List<Object?> get props => [];
}

final class MyDesignsInitial extends MyDesignsState {
  const MyDesignsInitial();
}

final class MyDesignsLoading extends MyDesignsState {
  const MyDesignsLoading();
}

final class MyDesignsError extends MyDesignsState {
  const MyDesignsError(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}

final class MyDesignsLoaded extends MyDesignsState {
  const MyDesignsLoaded({
    required this.designs,
    this.filter = MyDesignsFilter.all,
    this.busyId,
    this.actionError,
  });

  final List<DesignDto> designs;
  final MyDesignsFilter filter;
  final String? busyId;
  final String? actionError;

  List<DesignDto> get filtered {
    switch (filter) {
      case MyDesignsFilter.all:
        return designs;
      case MyDesignsFilter.drafts:
        return designs.where((d) => d.isDraft || d.status.toLowerCase() == 'ordered').toList();
      case MyDesignsFilter.readyToPublish:
        return designs.where((d) => d.isReadyToPublish).toList();
      case MyDesignsFilter.published:
        return designs.where((d) => d.isPublished).toList();
    }
  }

  MyDesignsLoaded copyWith({
    List<DesignDto>? designs,
    MyDesignsFilter? filter,
    String? busyId,
    bool clearBusy = false,
    String? actionError,
    bool clearActionError = false,
  }) {
    return MyDesignsLoaded(
      designs: designs ?? this.designs,
      filter: filter ?? this.filter,
      busyId: clearBusy ? null : (busyId ?? this.busyId),
      actionError: clearActionError ? null : (actionError ?? this.actionError),
    );
  }

  @override
  List<Object?> get props => [designs, filter, busyId, actionError];
}
