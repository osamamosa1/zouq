import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:zouq/config/api_config.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/design_editor/data/design_dtos.dart';
import 'package:zouq/features/my_designs/presentation/my_designs_cubit.dart';

class MyDesignsScreen extends StatefulWidget {
  const MyDesignsScreen({super.key});

  @override
  State<MyDesignsScreen> createState() => _MyDesignsScreenState();
}

class _MyDesignsScreenState extends State<MyDesignsScreen> {
  @override
  void initState() {
    super.initState();
    context.read<MyDesignsCubit>().load();
  }

  String _url(String? path) {
    if (path == null || path.isEmpty) return '';
    if (path.startsWith('http')) return path;
    return '${ApiConfig.baseUrl}$path';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Designs')),
      body: BlocConsumer<MyDesignsCubit, MyDesignsState>(
        listenWhen: (p, c) => c is MyDesignsLoaded && c.actionError != null,
        listener: (context, state) {
          if (state is MyDesignsLoaded && state.actionError != null) {
            ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(state.actionError!)));
          }
        },
        builder: (context, state) {
          if (state is MyDesignsLoading || state is MyDesignsInitial) {
            return const LoadingView(message: 'Loading your designs…');
          }
          if (state is MyDesignsError) {
            return ErrorView(
              message: state.message,
              onRetry: () => context.read<MyDesignsCubit>().load(),
            );
          }
          if (state is! MyDesignsLoaded) return const SizedBox.shrink();

          final cubit = context.read<MyDesignsCubit>();
          final items = state.filtered;

          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(12, 8, 12, 0),
                child: Text(
                  'Design → Order → Delivered → Publish → For You',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ),
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                child: Row(
                  children: [
                    for (final f in MyDesignsFilter.values)
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: Text(_filterLabel(f)),
                          selected: state.filter == f,
                          onSelected: (_) => cubit.setFilter(f),
                        ),
                      ),
                  ],
                ),
              ),
              Expanded(
                child: RefreshIndicator(
                  onRefresh: cubit.load,
                  child: items.isEmpty
                      ? ListView(
                          children: const [
                            SizedBox(height: 80),
                            Center(child: Text('No designs in this filter.')),
                          ],
                        )
                      : ListView.separated(
                          padding: const EdgeInsets.all(12),
                          itemCount: items.length,
                          separatorBuilder: (_, __) => const SizedBox(height: 8),
                          itemBuilder: (context, i) => _DesignCard(
                            design: items[i],
                            resolveUrl: _url,
                            busy: state.busyId == items[i].id,
                            onEdit: items[i].isDraft
                                ? () => context.push(
                                      '/products/${items[i].productId}/design?draftId=${items[i].id}',
                                    )
                                : null,
                            onContinueOrder: items[i].isDraft
                                ? () => context.push(
                                      '/products/${items[i].productId}/design?draftId=${items[i].id}',
                                    )
                                : null,
                            onPublish: items[i].isReadyToPublish
                                ? () => cubit.publish(items[i].id)
                                : null,
                            onUnpublish: items[i].isPublished
                                ? () => cubit.unpublish(items[i].id)
                                : null,
                            onView: items[i].isPublished
                                ? () => context.push(
                                      '/products/${items[i].productId}/design?draftId=${items[i].id}',
                                    )
                                : null,
                          ),
                        ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  String _filterLabel(MyDesignsFilter f) => switch (f) {
        MyDesignsFilter.all => 'All',
        MyDesignsFilter.drafts => 'Drafts',
        MyDesignsFilter.readyToPublish => 'Ready to Publish',
        MyDesignsFilter.published => 'Published',
      };
}

class _DesignCard extends StatelessWidget {
  const _DesignCard({
    required this.design,
    required this.resolveUrl,
    required this.busy,
    this.onEdit,
    this.onContinueOrder,
    this.onPublish,
    this.onUnpublish,
    this.onView,
  });

  final DesignDto design;
  final String Function(String?) resolveUrl;
  final bool busy;
  final VoidCallback? onEdit;
  final VoidCallback? onContinueOrder;
  final VoidCallback? onPublish;
  final VoidCallback? onUnpublish;
  final VoidCallback? onView;

  @override
  Widget build(BuildContext context) {
    final date = design.updatedAtUtc;
    final dateLabel = date == null ? '' : DateFormat.yMMMd().add_jm().format(date.toLocal());

    return Card(
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (design.previewImageUrl != null)
            AspectRatio(
              aspectRatio: 16 / 9,
              child: Image.network(
                resolveUrl(design.previewImageUrl),
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => ColoredBox(
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                  child: const Center(child: Icon(Icons.image_outlined)),
                ),
              ),
            )
          else
            ColoredBox(
              color: Theme.of(context).colorScheme.surfaceContainerHighest,
              child: const SizedBox(
                height: 88,
                child: Center(child: Icon(Icons.palette_outlined)),
              ),
            ),
          ListTile(
            title: Text(design.title),
            subtitle: Text(
              [
                design.status,
                if (design.isDerived) 'Derived',
                if (design.isPublished) 'Reusable',
                if (dateLabel.isNotEmpty) dateLabel,
              ].join(' · '),
            ),
          ),
          if (busy)
            const LinearProgressIndicator()
          else
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
              child: Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  if (onEdit != null)
                    OutlinedButton(onPressed: onEdit, child: const Text('Edit')),
                  if (onContinueOrder != null)
                    FilledButton(onPressed: onContinueOrder, child: const Text('Continue Order')),
                  if (onPublish != null)
                    FilledButton(onPressed: onPublish, child: const Text('Publish')),
                  if (onView != null)
                    OutlinedButton(onPressed: onView, child: const Text('View')),
                  if (onUnpublish != null)
                    OutlinedButton(onPressed: onUnpublish, child: const Text('Unpublish')),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
