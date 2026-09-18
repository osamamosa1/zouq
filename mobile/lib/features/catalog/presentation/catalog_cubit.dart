import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:zouq/features/catalog/data/catalog_dtos.dart';
import 'package:zouq/features/catalog/data/catalog_repository.dart';

part 'catalog_state.dart';

class CatalogCubit extends Cubit<CatalogState> {
  CatalogCubit(this._repository) : super(const CatalogInitial());

  final CatalogRepository _repository;

  Future<void> loadProducts() async {
    emit(const CatalogLoading());
    try {
      final products = await _repository.listProducts();
      emit(CatalogLoaded(products: products));
    } catch (e) {
      emit(CatalogError(e.toString()));
    }
  }
}

class ProductConfigCubit extends Cubit<ProductConfigState> {
  ProductConfigCubit(this._repository) : super(const ProductConfigInitial());

  final CatalogRepository _repository;

  Future<void> load(String productId) async {
    emit(const ProductConfigLoading());
    try {
      final config = await _repository.getProductConfig(productId);
      emit(ProductConfigLoaded(config: config));
    } catch (e) {
      emit(ProductConfigError(e.toString()));
    }
  }
}
