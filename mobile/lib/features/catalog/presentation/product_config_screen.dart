import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/catalog/data/catalog_dtos.dart';
import 'package:zouq/features/catalog/presentation/catalog_cubit.dart';

class ProductConfigScreen extends StatefulWidget {
  const ProductConfigScreen({super.key, required this.productId});

  final String productId;

  @override
  State<ProductConfigScreen> createState() => _ProductConfigScreenState();
}

class _ProductConfigScreenState extends State<ProductConfigScreen> {
  @override
  void initState() {
    super.initState();
    context.read<ProductConfigCubit>().load(widget.productId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Configure product')),
      body: BlocBuilder<ProductConfigCubit, ProductConfigState>(
        builder: (context, state) {
          if (state is ProductConfigLoading || state is ProductConfigInitial) {
            return const LoadingView(message: 'Loading options from API…');
          }
          if (state is ProductConfigError) {
            return ErrorView(
              message: state.message,
              onRetry: () => context.read<ProductConfigCubit>().load(widget.productId),
            );
          }
          if (state is ProductConfigLoaded) {
            return _ConfigBody(config: state.config);
          }
          return const SizedBox.shrink();
        },
      ),
    );
  }
}

class _ConfigBody extends StatelessWidget {
  const _ConfigBody({required this.config});

  final ProductConfigDto config;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(config.name, style: Theme.of(context).textTheme.headlineSmall),
        if (config.description != null) ...[
          const SizedBox(height: 8),
          Text(config.description!),
        ],
        const SizedBox(height: 16),
        _Section(
          title: 'Surfaces (${config.surfaces.length})',
          child: Wrap(
            spacing: 8,
            runSpacing: 8,
            children: config.surfaces
                .map((s) => Chip(label: Text('${s.name} (${s.code})')))
                .toList(),
          ),
        ),
        _Section(
          title: 'Fabrics',
          child: _OptionList(
            items: config.fabrics.map((f) => '${f.name} (+${f.priceAdjustment})').toList(),
          ),
        ),
        _Section(
          title: 'Cuts',
          child: _OptionList(
            items: config.cuts.map((c) => '${c.name} (+${c.priceAdjustment})').toList(),
          ),
        ),
        _Section(
          title: 'Sizes',
          child: _OptionList(
            items: config.sizes
                .map((s) => '${s.name} (${s.code}) · ${s.width}×${s.height} ${s.unit}')
                .toList(),
          ),
        ),
        _Section(
          title: 'Printing options',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: config.printingOptions
                .map(
                  (p) => Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Text(
                      '${p.name}: surfaces ${p.includedSurfaceCodes.join(', ')} · ${p.price}',
                    ),
                  ),
                )
                .toList(),
          ),
        ),
        const SizedBox(height: 24),
        FilledButton.icon(
          onPressed: () => context.push('/products/${config.id}/design'),
          icon: const Icon(Icons.brush_outlined),
          label: const Text('Open design editor'),
        ),
      ],
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          child,
        ],
      ),
    );
  }
}

class _OptionList extends StatelessWidget {
  const _OptionList({required this.items});

  final List<String> items;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const Text('None configured');
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: items.map((t) => Text('• $t')).toList(),
    );
  }
}
