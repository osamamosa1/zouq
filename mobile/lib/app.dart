import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:zouq/config/app_routes.dart';
import 'package:zouq/config/app_theme.dart';
import 'package:zouq/features/auth/presentation/auth_cubit.dart';
import 'package:zouq/features/catalog/presentation/catalog_cubit.dart';
import 'package:zouq/features/feed/presentation/feed_cubit.dart';
import 'package:zouq/features/orders/presentation/orders_cubit.dart';
import 'package:zouq/features/profile/presentation/profile_cubit.dart';
import 'package:zouq/injection_container.dart';

class ZouqApp extends StatelessWidget {
  const ZouqApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider.value(value: sl<AuthCubit>()..checkSession()),
        BlocProvider(create: (_) => sl<FeedCubit>()),
        BlocProvider(create: (_) => sl<CatalogCubit>()),
        BlocProvider(create: (_) => sl<OrdersCubit>()),
        BlocProvider(create: (_) => sl<ProfileCubit>()),
      ],
      child: MaterialApp.router(
        title: 'Zouq',
        theme: AppTheme.light(),
        routerConfig: AppRoutes.router,
        debugShowCheckedModeBanner: false,
      ),
    );
  }
}
