import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:zouq/config/api_config.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/catalog/presentation/catalog_cubit.dart';

class ProductListScreen extends StatefulWidget {
  const ProductListScreen({super.key});

  @override
  State<ProductListScreen> createState() => _ProductListScreenState();
}

class _ProductListScreenState extends State<ProductListScreen> {
  @override
  void initState() {
    super.initState();
    context.read<CatalogCubit>().loadProducts();
  }

  String _resolveUrl(String? path) {
    if (path == null || path.isEmpty) return '';
    if (path.startsWith('http')) return path;
    return '${ApiConfig.baseUrl}$path';
  }

  @override
  Widget build(BuildContext context) {
    final currency = NumberFormat.simpleCurrency(name: 'USD');

    return Scaffold(
      appBar: AppBar(title: const Text('Catalog')),
      body: BlocBuilder<CatalogCubit, CatalogState>(
        builder: (context, state) {
          if (state is CatalogLoading || state is CatalogInitial) {
            return const LoadingView(message: 'Loading products…');
          }
          if (state is CatalogError) {
            return ErrorView(
              message: state.message,
              onRetry: () => context.read<CatalogCubit>().loadProducts(),
            );
          }
          if (state is CatalogLoaded) {
            if (state.products.isEmpty) {
              return const Center(child: Text('No products yet.'));
            }
            return ListView.separated(
              padding: const EdgeInsets.all(12),
              itemCount: state.products.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (context, index) {
                final p = state.products[index];
                return Card(
                  child: ListTile(
                    leading: p.thumbnailUrl != null
                        ? ClipRRect(
                            borderRadius: BorderRadius.circular(8),
                            child: Image.network(
                              _resolveUrl(p.thumbnailUrl),
                              width: 56,
                              height: 56,
                              fit: BoxFit.cover,
                              errorBuilder: (_, __, ___) => const Icon(Icons.inventory_2_outlined),
                            ),
                          )
                        : const Icon(Icons.inventory_2_outlined),
                    title: Text(p.name),
                    subtitle: Text('${p.productTypeName} · from ${currency.format(p.basePrice)}'),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: () => context.push('/products/${p.id}/config'),
                  ),
                );
              },
            );
          }
          return const SizedBox.shrink();
        },
      ),
    );
  }
}
