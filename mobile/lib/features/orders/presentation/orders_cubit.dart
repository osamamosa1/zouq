import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:zouq/features/orders/data/order_dtos.dart';
import 'package:zouq/features/orders/data/orders_repository.dart';

part 'orders_state.dart';

class OrdersCubit extends Cubit<OrdersState> {
  OrdersCubit(this._repository) : super(const OrdersInitial());

  final OrdersRepository _repository;

  Future<void> load() async {
    emit(const OrdersLoading());
    try {
      final orders = await _repository.myOrders();
      emit(OrdersLoaded(orders: orders));
    } catch (e) {
      emit(OrdersError(e.toString()));
    }
  }
}
