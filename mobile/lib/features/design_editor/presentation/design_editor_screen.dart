import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';
import 'package:zouq/config/api_config.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/design_editor/data/design_dtos.dart';
import 'package:zouq/features/design_editor/data/design_repository.dart';
import 'package:zouq/features/design_editor/domain/production_method.dart';
import 'package:zouq/features/design_editor/presentation/design_editor_cubit.dart';
import 'package:zouq/features/design_editor/presentation/widgets/design_canvas.dart';
import 'package:zouq/injection_container.dart';

class DesignEditorScreen extends StatefulWidget {
  const DesignEditorScreen({super.key, required this.productId, this.draftDesignId});

  final String productId;
  final String? draftDesignId;

  @override
  State<DesignEditorScreen> createState() => _DesignEditorScreenState();
}

class _DesignEditorScreenState extends State<DesignEditorScreen> {
  @override
  void initState() {
    super.initState();
    context.read<DesignEditorCubit>().load(widget.productId, draftDesignId: widget.draftDesignId);
  }

  @override
  Widget build(BuildContext context) {
    final currency = NumberFormat.simpleCurrency(name: 'USD');

    return Scaffold(
      appBar: AppBar(title: const Text('Design editor')),
      body: BlocConsumer<DesignEditorCubit, DesignEditorState>(
        listenWhen: (p, c) =>
            c is DesignEditorReady &&
            (c.actionError != null || c.savedDesignId != null || c.createdOrderId != null),
        listener: (context, state) {
          if (state is! DesignEditorReady) return;
          if (state.actionError != null) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.actionError!)),
            );
          }
          if (state.createdOrderId != null) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Order created: ${state.createdOrderId}')),
            );
          } else if (state.savedDesignId != null) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Design saved (${state.savedDesignId})')),
            );
          }
        },
        builder: (context, state) {
          if (state is DesignEditorLoading || state is DesignEditorInitial) {
            return const LoadingView(message: 'Loading product surfaces…');
          }
          if (state is DesignEditorError) {
            return ErrorView(
              message: state.message,
              onRetry: () => context.read<DesignEditorCubit>().load(
                widget.productId,
                draftDesignId: widget.draftDesignId,
              ),
            );
          }
          if (state is! DesignEditorReady) return const SizedBox.shrink();

          final cubit = context.read<DesignEditorCubit>();
          final surfaces = cubit.availableSurfaces(state);
          final active = state.activeSurface;

          return Column(
            children: [
              Expanded(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(state.config.name, style: Theme.of(context).textTheme.titleLarge),
                      const SizedBox(height: 8),
                      if (state.config.printingOptions.isNotEmpty)
                        DropdownButtonFormField<String>(
                          initialValue: state.selectedPrintingId,
                          decoration: const InputDecoration(labelText: 'Printing option'),
                          items: state.config.printingOptions
                              .map(
                                (p) => DropdownMenuItem(
                                  value: p.id,
                                  child: Text('${p.name} (${p.includedSurfaceCodes.join(', ')})'),
                                ),
                              )
                              .toList(),
                          onChanged: cubit.selectPrinting,
                        ),
                      const SizedBox(height: 8),
                      if (surfaces.length > 1)
                        SegmentedButton<String>(
                          segments: surfaces
                              .map((s) => ButtonSegment(value: s.code, label: Text(s.name)))
                              .toList(),
                          selected: {if (state.activeSurfaceCode != null) state.activeSurfaceCode!},
                          emptySelectionAllowed: true,
                          onSelectionChanged: (set) {
                            if (set.isNotEmpty) cubit.setActiveSurface(set.first);
                          },
                        ),
                      const SizedBox(height: 12),
                      if (active != null)
                        DesignCanvas(
                          surface: active,
                          elements: state.elements,
                          selectedElementId: state.selectedElementId,
                          onSelect: cubit.selectElement,
                          onMoveElement: (id, x, y) {
                            cubit.updateElement(id, (e) => e.copyWith(normX: x, normY: y));
                          },
                          onResizeElement: (id, w, h) {
                            cubit.updateElement(id, (e) => e.copyWith(normWidth: w, normHeight: h));
                          },
                          onRotateElement: (id, deg) {
                            cubit.updateElement(id, (e) => e.copyWith(rotationDegrees: deg));
                          },
                        ),
                      if (state.selectedElement != null) ...[
                        const SizedBox(height: 8),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: cubit.removeSelectedElement,
                                icon: const Icon(Icons.delete_outline),
                                label: const Text('Delete element'),
                              ),
                            ),
                            const SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                'Rotation ${state.selectedElement!.rotationDegrees.toStringAsFixed(0)}°',
                                textAlign: TextAlign.center,
                              ),
                            ),
                          ],
                        ),
                      ],
                      const SizedBox(height: 12),
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          if (state.config.fabrics.isNotEmpty)
                            _OptionDropdown(
                              label: 'Fabric',
                              value: state.selectedFabricId,
                              items: state.config.fabrics.map((f) => (f.id, f.name)).toList(),
                              onChanged: cubit.selectFabric,
                            ),
                          if (state.config.cuts.isNotEmpty)
                            _OptionDropdown(
                              label: 'Cut',
                              value: state.selectedCutId,
                              items: state.config.cuts.map((c) => (c.id, c.name)).toList(),
                              onChanged: cubit.selectCut,
                            ),
                          if (state.config.sizes.isNotEmpty)
                            _OptionDropdown(
                              label: 'Size',
                              value: state.selectedSizeId,
                              items: state.config.sizes.map((s) => (s.id, s.name)).toList(),
                              onChanged: cubit.selectSize,
                            ),
                        ],
                      ),
                      if (state.selectedElement != null) ...[
                        const SizedBox(height: 12),
                        DropdownButtonFormField<ProductionMethod>(
                          initialValue: state.selectedElement!.productionMethod,
                          decoration: const InputDecoration(labelText: 'Production method'),
                          items: ProductionMethod.values
                              .map(
                                (m) => DropdownMenuItem(value: m, child: Text(m.apiValue)),
                              )
                              .toList(),
                          onChanged: (m) {
                            if (m == null) return;
                            cubit.setProductionMethod(state.selectedElement!.id, m);
                          },
                        ),
                      ],
                      if (state.quote != null) ...[
                        const SizedBox(height: 12),
                        Text('Server quote breakdown', style: Theme.of(context).textTheme.titleSmall),
                        ...state.quote!.lines.map(
                          (l) => ListTile(
                            dense: true,
                            contentPadding: EdgeInsets.zero,
                            title: Text(l.label),
                            trailing: Text(currency.format(l.amount)),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              Material(
                elevation: 4,
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      if (state.quote != null)
                        Text(
                          'Quote: ${currency.format(state.quote!.total)}',
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              onPressed: cubit.addElement,
                              icon: const Icon(Icons.add),
                              label: const Text('Add'),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: OutlinedButton.icon(
                              onPressed: () => _openAssetPicker(context, cubit, state),
                              icon: const Icon(Icons.collections),
                              label: const Text('Assets'),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: OutlinedButton.icon(
                              onPressed: state.saving ? null : cubit.pickAndUploadImage,
                              icon: const Icon(Icons.upload),
                              label: const Text('Upload'),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton(
                              onPressed: state.quoting ? null : cubit.fetchQuote,
                              child: state.quoting
                                  ? const SizedBox(
                                      height: 18,
                                      width: 18,
                                      child: CircularProgressIndicator(strokeWidth: 2),
                                    )
                                  : const Text('Get quote'),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Expanded(
                            child: OutlinedButton(
                              onPressed: state.saving ? null : () => cubit.saveDesign(),
                              child: const Text('Save draft'),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      FilledButton(
                        onPressed: state.saving ? null : () => cubit.createOrder(),
                        child: state.saving
                            ? const SizedBox(
                                height: 18,
                                width: 18,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Text('Create order'),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Future<void> _openAssetPicker(
    BuildContext context,
    DesignEditorCubit cubit,
    DesignEditorReady state,
  ) async {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => _AssetPickerSheet(
        productId: state.config.id,
        surfaceCode: state.activeSurfaceCode,
        onPick: (asset) {
          cubit.addAssetElement(assetId: asset.id, label: asset.name);
          Navigator.of(ctx).pop();
        },
      ),
    );
  }
}

class _AssetPickerSheet extends StatefulWidget {
  const _AssetPickerSheet({
    required this.productId,
    required this.surfaceCode,
    required this.onPick,
  });

  final String productId;
  final String? surfaceCode;
  final void Function(DesignAssetDto asset) onPick;

  @override
  State<_AssetPickerSheet> createState() => _AssetPickerSheetState();
}

class _AssetPickerSheetState extends State<_AssetPickerSheet> {
  late Future<List<DesignAssetDto>> _future;

  @override
  void initState() {
    super.initState();
    _future = sl<DesignRepository>().listAssets(
      productId: widget.productId,
      surface: widget.surfaceCode,
    );
  }

  String _url(String path) {
    if (path.startsWith('http')) return path;
    return '${ApiConfig.baseUrl}$path';
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: SizedBox(
        height: MediaQuery.of(context).size.height * 0.7,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: Text('Ready-made assets', style: Theme.of(context).textTheme.titleMedium),
            ),
            Expanded(
              child: FutureBuilder<List<DesignAssetDto>>(
                future: _future,
                builder: (context, snap) {
                  if (snap.connectionState != ConnectionState.done) {
                    return const Center(child: CircularProgressIndicator());
                  }
                  if (snap.hasError) {
                    return ErrorView(
                      message: snap.error.toString(),
                      onRetry: () => setState(() {
                        _future = sl<DesignRepository>().listAssets(
                          productId: widget.productId,
                          surface: widget.surfaceCode,
                        );
                      }),
                    );
                  }
                  final assets = snap.data ?? [];
                  if (assets.isEmpty) {
                    return const Center(child: Text('No assets available.'));
                  }
                  return GridView.builder(
                    padding: const EdgeInsets.all(12),
                    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      mainAxisSpacing: 8,
                      crossAxisSpacing: 8,
                      childAspectRatio: 0.9,
                    ),
                    itemCount: assets.length,
                    itemBuilder: (context, i) {
                      final a = assets[i];
                      return InkWell(
                        onTap: () => widget.onPick(a),
                        child: Card(
                          clipBehavior: Clip.antiAlias,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              Expanded(
                                child: Image.network(
                                  _url(a.thumbnailUrl ?? a.fileUrl),
                                  fit: BoxFit.cover,
                                  errorBuilder: (_, __, ___) =>
                                      const Center(child: Icon(Icons.image_outlined)),
                                ),
                              ),
                              Padding(
                                padding: const EdgeInsets.all(8),
                                child: Text(a.name, maxLines: 2, overflow: TextOverflow.ellipsis),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _OptionDropdown extends StatelessWidget {
  const _OptionDropdown({
    required this.label,
    required this.value,
    required this.items,
    required this.onChanged,
  });

  final String label;
  final String? value;
  final List<(String, String)> items;
  final ValueChanged<String?> onChanged;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 160,
      child: DropdownButtonFormField<String>(
        initialValue: value,
        decoration: InputDecoration(labelText: label),
        items: items
            .map((e) => DropdownMenuItem(value: e.$1, child: Text(e.$2, overflow: TextOverflow.ellipsis)))
            .toList(),
        onChanged: onChanged,
      ),
    );
  }
}
