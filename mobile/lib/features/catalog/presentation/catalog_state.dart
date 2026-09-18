part of 'catalog_cubit.dart';

sealed class CatalogState extends Equatable {
  const CatalogState();

  @override
  List<Object?> get props => [];
}

final class CatalogInitial extends CatalogState {
  const CatalogInitial();
}

final class CatalogLoading extends CatalogState {
  const CatalogLoading();
}

final class CatalogLoaded extends CatalogState {
  const CatalogLoaded({required this.products});
  final List<ProductListItemDto> products;

  @override
  List<Object?> get props => [products];
}

final class CatalogError extends CatalogState {
  const CatalogError(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}

sealed class ProductConfigState extends Equatable {
  const ProductConfigState();

  @override
  List<Object?> get props => [];
}

final class ProductConfigInitial extends ProductConfigState {
  const ProductConfigInitial();
}

final class ProductConfigLoading extends ProductConfigState {
  const ProductConfigLoading();
}

final class ProductConfigLoaded extends ProductConfigState {
  const ProductConfigLoaded({required this.config});
  final ProductConfigDto config;

  @override
  List<Object?> get props => [config];
}

final class ProductConfigError extends ProductConfigState {
  const ProductConfigError(this.message);
  final String message;

  @override
  List<Object?> get props => [message];
}
