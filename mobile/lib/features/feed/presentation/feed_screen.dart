import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:zouq/config/api_config.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/auth/presentation/auth_cubit.dart';
import 'package:zouq/features/design_editor/data/design_repository.dart';
import 'package:zouq/features/feed/data/feed_dtos.dart';
import 'package:zouq/features/feed/presentation/feed_cubit.dart';
import 'package:zouq/injection_container.dart';

class FeedScreen extends StatefulWidget {
  const FeedScreen({super.key});

  @override
  State<FeedScreen> createState() => _FeedScreenState();
}

class _FeedScreenState extends State<FeedScreen> {
  final _scroll = ScrollController();

  @override
  void initState() {
    super.initState();
    context.read<FeedCubit>().load();
    _scroll.addListener(() {
      if (_scroll.position.pixels > _scroll.position.maxScrollExtent - 240) {
        context.read<FeedCubit>().loadMore();
      }
    });
  }

  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  String _url(String? path) {
    if (path == null || path.isEmpty) return '';
    if (path.startsWith('http')) return path;
    return '${ApiConfig.baseUrl}$path';
  }

  Future<void> _useDesign(FeedItemDto item) async {
    final auth = context.read<AuthCubit>().state;
    if (auth is! AuthAuthenticated) {
      if (mounted) context.go('/login');
      return;
    }

    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (_) => const Center(child: CircularProgressIndicator()),
    );

    try {
      final draft = await sl<DesignRepository>().derive(item.designId);
      if (!mounted) return;
      Navigator.of(context).pop();
      context.push('/products/${draft.productId}/design?draftId=${draft.id}');
    } catch (e) {
      if (!mounted) return;
      Navigator.of(context).pop();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.toString())),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('For You')),
      body: BlocBuilder<FeedCubit, FeedState>(
        builder: (context, state) {
          if (state is FeedLoading || state is FeedInitial) {
            return const LoadingView(message: 'Loading feed…');
          }
          if (state is FeedError) {
            return ErrorView(
              message: state.message,
              onRetry: () => context.read<FeedCubit>().load(),
            );
          }
          if (state is FeedLoaded) {
            final feedAds = state.ads.where((a) => a.placement == 'feed').toList()
              ..sort((a, b) => b.displayPriority.compareTo(a.displayPriority));

            return RefreshIndicator(
              onRefresh: () => context.read<FeedCubit>().load(),
              child: ListView(
                controller: _scroll,
                padding: const EdgeInsets.all(12),
                children: [
                  for (final ad in feedAds) _AdCard(ad: ad, resolveUrl: _url),
                  if (state.items.isEmpty && feedAds.isEmpty)
                    const Padding(
                      padding: EdgeInsets.all(24),
                      child: Center(
                        child: Text(
                          'No published reusable designs yet.\nDesigns appear here only after Delivered + Publish.',
                          textAlign: TextAlign.center,
                        ),
                      ),
                    ),
                  for (final item in state.items)
                    _FeedCard(item: item, resolveUrl: _url, onUse: () => _useDesign(item)),
                  if (state.loadingMore)
                    const Padding(
                      padding: EdgeInsets.all(16),
                      child: Center(child: CircularProgressIndicator()),
                    ),
                  if (state.loadMoreError != null)
                    Padding(
                      padding: const EdgeInsets.all(8),
                      child: Text(state.loadMoreError!, textAlign: TextAlign.center),
                    ),
                ],
              ),
            );
          }
          return const SizedBox.shrink();
        },
      ),
    );
  }
}

class _FeedCard extends StatelessWidget {
  const _FeedCard({
    required this.item,
    required this.resolveUrl,
    required this.onUse,
  });

  final FeedItemDto item;
  final String Function(String?) resolveUrl;
  final VoidCallback onUse;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (item.previewImageUrl != null)
            AspectRatio(
              aspectRatio: 16 / 9,
              child: Image.network(
                resolveUrl(item.previewImageUrl),
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => ColoredBox(
                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                  child: const Center(child: Icon(Icons.image_outlined)),
                ),
              ),
            ),
          ListTile(
            title: Text(item.title),
            subtitle: Text('by ${item.ownerName}'),
            trailing: item.isFeatured ? const Icon(Icons.star, color: Colors.amber) : null,
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
            child: SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: onUse,
                child: const Text('Use Design'),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _AdCard extends StatelessWidget {
  const _AdCard({required this.ad, required this.resolveUrl});

  final AdDto ad;
  final String Function(String?) resolveUrl;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      color: Theme.of(context).colorScheme.primaryContainer,
      child: ListTile(
        leading: ad.imageUrl != null
            ? Image.network(
                resolveUrl(ad.imageUrl),
                width: 48,
                height: 48,
                fit: BoxFit.cover,
              )
            : const Icon(Icons.campaign_outlined),
        title: Text(ad.title),
        subtitle: Text(ad.description ?? 'Sponsored'),
        trailing: const Text('Ad'),
      ),
    );
  }
}
