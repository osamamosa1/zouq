import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:zouq/features/auth/presentation/login_screen.dart';
import 'package:zouq/features/auth/presentation/register_screen.dart';
import 'package:zouq/features/catalog/presentation/catalog_cubit.dart';
import 'package:zouq/features/catalog/presentation/product_config_screen.dart';
import 'package:zouq/features/catalog/presentation/product_list_screen.dart';
import 'package:zouq/features/design_editor/presentation/design_editor_cubit.dart';
import 'package:zouq/features/design_editor/presentation/design_editor_screen.dart';
import 'package:zouq/features/feed/presentation/feed_screen.dart';
import 'package:zouq/features/my_designs/presentation/my_designs_cubit.dart';
import 'package:zouq/features/my_designs/presentation/my_designs_screen.dart';
import 'package:zouq/features/orders/presentation/order_detail_screen.dart';
import 'package:zouq/features/orders/presentation/orders_screen.dart';
import 'package:zouq/features/profile/presentation/profile_screen.dart';
import 'package:zouq/features/tabbar/main_shell.dart';
import 'package:zouq/injection_container.dart';

class AppRoutes {
  AppRoutes._();

  static final _rootKey = GlobalKey<NavigatorState>();

  static GoRouter router = GoRouter(
    navigatorKey: _rootKey,
    initialLocation: '/',
    routes: [
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterScreen(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            MainShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/',
                builder: (context, state) => const FeedScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/catalog',
                builder: (context, state) => const ProductListScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/orders',
                builder: (context, state) => const OrdersScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/profile',
                builder: (context, state) => const ProfileScreen(),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: '/my-designs',
        parentNavigatorKey: _rootKey,
        builder: (context, state) => BlocProvider(
          create: (_) => sl<MyDesignsCubit>(),
          child: const MyDesignsScreen(),
        ),
      ),
      GoRoute(
        path: '/orders/:orderId',
        parentNavigatorKey: _rootKey,
        builder: (context, state) => OrderDetailScreen(
          orderId: state.pathParameters['orderId']!,
        ),
      ),
      GoRoute(
        path: '/products/:productId/config',
        parentNavigatorKey: _rootKey,
        builder: (context, state) => BlocProvider(
          create: (_) => sl<ProductConfigCubit>(),
          child: ProductConfigScreen(productId: state.pathParameters['productId']!),
        ),
      ),
      GoRoute(
        path: '/products/:productId/design',
        parentNavigatorKey: _rootKey,
        builder: (context, state) => BlocProvider(
          create: (_) => sl<DesignEditorCubit>(),
          child: DesignEditorScreen(
            productId: state.pathParameters['productId']!,
            draftDesignId: state.uri.queryParameters['draftId'],
          ),
        ),
      ),
    ],
  );
}
