import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/orders/presentation/orders_cubit.dart';

class OrdersScreen extends StatefulWidget {
  const OrdersScreen({super.key});

  @override
  State<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends State<OrdersScreen> {
  @override
  void initState() {
    super.initState();
    context.read<OrdersCubit>().load();
  }

  @override
  Widget build(BuildContext context) {
    final currency = NumberFormat.simpleCurrency(name: 'USD');

    return Scaffold(
      appBar: AppBar(title: const Text('Orders')),
      body: BlocBuilder<OrdersCubit, OrdersState>(
        builder: (context, state) {
          if (state is OrdersLoading || state is OrdersInitial) {
            return const LoadingView(message: 'Loading orders…');
          }
          if (state is OrdersError) {
            return ErrorView(
              message: state.message,
              onRetry: () => context.read<OrdersCubit>().load(),
            );
          }
          if (state is OrdersLoaded) {
            if (state.orders.isEmpty) {
              return RefreshIndicator(
                onRefresh: () => context.read<OrdersCubit>().load(),
                child: ListView(
                  children: const [
                    SizedBox(height: 120),
                    Center(child: Text('No orders yet.')),
                  ],
                ),
              );
            }
            return RefreshIndicator(
              onRefresh: () => context.read<OrdersCubit>().load(),
              child: ListView.separated(
                padding: const EdgeInsets.all(12),
                itemCount: state.orders.length,
                separatorBuilder: (_, __) => const SizedBox(height: 8),
                itemBuilder: (context, index) {
                  final o = state.orders[index];
                  return Card(
                    child: ListTile(
                      title: Text(o.orderNumber),
                      subtitle: Text('${o.status} · ${o.createdAtUtc.toLocal()}'),
                      trailing: Text(currency.format(o.total)),
                      onTap: () => context.push('/orders/${o.id}'),
                    ),
                  );
                },
              ),
            );
          }
          return const SizedBox.shrink();
        },
      ),
    );
  }
}
