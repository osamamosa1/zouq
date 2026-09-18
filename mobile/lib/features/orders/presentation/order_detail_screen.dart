import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/orders/data/order_dtos.dart';
import 'package:zouq/features/orders/data/orders_repository.dart';
import 'package:zouq/injection_container.dart';

class OrderDetailScreen extends StatefulWidget {
  const OrderDetailScreen({super.key, required this.orderId});

  final String orderId;

  @override
  State<OrderDetailScreen> createState() => _OrderDetailScreenState();
}

class _OrderDetailScreenState extends State<OrderDetailScreen> {
  OrderDetailDto? _order;
  String? _error;
  bool _loading = true;

  static const _timeline = [
    'Pending',
    'Processing',
    'InProduction',
    'Shipped',
    'Delivered',
  ];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final order = await sl<OrdersRepository>().getOrder(widget.orderId);
      if (!mounted) return;
      setState(() {
        _order = order;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  Map<String, dynamic> _safeJson(String raw) {
    try {
      final decoded = jsonDecode(raw);
      if (decoded is Map<String, dynamic>) return decoded;
      if (decoded is Map) return Map<String, dynamic>.from(decoded);
    } catch (_) {}
    return {};
  }

  @override
  Widget build(BuildContext context) {
    final currency = NumberFormat.simpleCurrency(name: 'USD');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Order details'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
      ),
      body: _loading
          ? const LoadingView(message: 'Loading order snapshot…')
          : _error != null
              ? ErrorView(message: _error!, onRetry: _load)
              : _order == null
                  ? const Center(child: Text('Order not found'))
                  : RefreshIndicator(
                      onRefresh: _load,
                      child: ListView(
                        padding: const EdgeInsets.all(16),
                        children: [
                          Text(_order!.orderNumber, style: Theme.of(context).textTheme.headlineSmall),
                          Text('${_order!.status} · ${_order!.createdAtUtc.toLocal()}'),
                          const SizedBox(height: 12),
                          Card(
                            child: ListTile(
                              title: const Text('Total'),
                              subtitle: Text('Subtotal ${currency.format(_order!.subtotal)}'),
                              trailing: Text(
                                currency.format(_order!.total),
                                style: Theme.of(context).textTheme.titleMedium,
                              ),
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text('Status timeline', style: Theme.of(context).textTheme.titleMedium),
                          const SizedBox(height: 8),
                          _Timeline(current: _order!.status, steps: _timeline),
                          const SizedBox(height: 16),
                          for (final item in _order!.items) ...[
                            Text('Item snapshot', style: Theme.of(context).textTheme.titleMedium),
                            const SizedBox(height: 8),
                            _SnapshotSection(
                              title: 'Product',
                              data: _safeJson(item.productSnapshotJson),
                            ),
                            _SnapshotSection(
                              title: 'Pricing',
                              data: _safeJson(item.pricingBreakdownJson),
                            ),
                            _SnapshotSection(
                              title: 'Design',
                              data: _safeJson(item.designSnapshotJson),
                            ),
                            if (item.designerId != null)
                              ListTile(
                                contentPadding: EdgeInsets.zero,
                                title: const Text('Creator attribution'),
                                subtitle: Text(item.designerId!),
                              ),
                            ListTile(
                              contentPadding: EdgeInsets.zero,
                              title: Text('Line total'),
                              trailing: Text(currency.format(item.lineTotal)),
                            ),
                            const Divider(),
                          ],
                          ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: const Text('Commission snapshot'),
                            trailing: Text('${_order!.commissionPercentSnapshot}%'),
                          ),
                          if (_order!.creatorRewardAmount != null)
                            ListTile(
                              contentPadding: EdgeInsets.zero,
                              title: const Text('Creator reward (after Delivered)'),
                              trailing: Text(currency.format(_order!.creatorRewardAmount!)),
                            ),
                        ],
                      ),
                    ),
    );
  }
}

class _Timeline extends StatelessWidget {
  const _Timeline({required this.current, required this.steps});

  final String current;
  final List<String> steps;

  @override
  Widget build(BuildContext context) {
    final idx = steps.indexWhere((s) => s.toLowerCase() == current.toLowerCase());
    return Column(
      children: [
        for (var i = 0; i < steps.length; i++)
          ListTile(
            dense: true,
            leading: Icon(
              i <= idx && idx >= 0 ? Icons.check_circle : Icons.radio_button_unchecked,
              color: i <= idx && idx >= 0
                  ? Theme.of(context).colorScheme.primary
                  : Theme.of(context).colorScheme.outline,
            ),
            title: Text(steps[i]),
          ),
      ],
    );
  }
}

class _SnapshotSection extends StatelessWidget {
  const _SnapshotSection({required this.title, required this.data});

  final String title;
  final Map<String, dynamic> data;

  @override
  Widget build(BuildContext context) {
    if (data.isEmpty) {
      return Card(
        child: ListTile(title: Text(title), subtitle: const Text('No snapshot data')),
      );
    }

    final entries = data.entries.take(24).toList();
    return Card(
      child: ExpansionTile(
        title: Text(title),
        children: [
          for (final e in entries)
            ListTile(
              dense: true,
              title: Text(e.key),
              subtitle: Text(_formatValue(e.value)),
            ),
        ],
      ),
    );
  }

  String _formatValue(dynamic value) {
    if (value is List) {
      return value.map(_formatValue).join(', ');
    }
    if (value is Map) {
      return value.entries.map((e) => '${e.key}: ${_formatValue(e.value)}').join(' · ');
    }
    return value?.toString() ?? '—';
  }
}
