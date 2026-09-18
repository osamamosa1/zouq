import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:zouq/core/widgets/error_view.dart';
import 'package:zouq/core/widgets/loading_view.dart';
import 'package:zouq/features/auth/presentation/auth_cubit.dart';
import 'package:zouq/features/profile/presentation/profile_cubit.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  @override
  void initState() {
    super.initState();
    context.read<ProfileCubit>().load();
  }

  @override
  Widget build(BuildContext context) {
    final currency = NumberFormat.simpleCurrency(name: 'USD');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Profile'),
        actions: [
          IconButton(
            onPressed: () async {
              await context.read<AuthCubit>().logout();
              if (context.mounted) context.go('/login');
            },
            icon: const Icon(Icons.logout),
            tooltip: 'Sign out',
          ),
        ],
      ),
      body: BlocBuilder<ProfileCubit, ProfileState>(
        builder: (context, state) {
          if (state is ProfileLoading || state is ProfileInitial) {
            return const LoadingView(message: 'Loading profile…');
          }
          if (state is ProfileError) {
            return ErrorView(
              message: state.message,
              onRetry: () => context.read<ProfileCubit>().load(),
            );
          }
          if (state is ProfileLoaded) {
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(state.user.name, style: Theme.of(context).textTheme.headlineSmall),
                Text(state.user.email),
                const SizedBox(height: 16),
                Card(
                  child: ListTile(
                    title: const Text('Wallet balance'),
                    trailing: Text(
                      currency.format(state.balance),
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                  ),
                ),
                const SizedBox(height: 8),
                ListTile(
                  leading: const Icon(Icons.palette_outlined),
                  title: const Text('My Designs'),
                  subtitle: const Text('Drafts · Ready to publish · Published'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.push('/my-designs'),
                ),
                ListTile(
                  leading: const Icon(Icons.receipt_long_outlined),
                  title: const Text('My Orders'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => context.go('/orders'),
                ),
                const SizedBox(height: 16),
                Text('Recent ledger', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                if (state.ledger.isEmpty)
                  const Text('No ledger entries')
                else
                  ...state.ledger.take(10).map(
                        (e) => ListTile(
                          dense: true,
                          title: Text('${e['entry_type']} · ${e['reason'] ?? ''}'),
                          subtitle: Text('${e['created_at_utc']}'),
                          trailing: Text(currency.format((e['amount'] as num).toDouble())),
                        ),
                      ),
              ],
            );
          }
          return const SizedBox.shrink();
        },
      ),
    );
  }
}
