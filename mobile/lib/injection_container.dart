import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:zouq/config/api_config.dart';
import 'package:zouq/core/api/api_client.dart';
import 'package:zouq/features/auth/data/auth_repository.dart';
import 'package:zouq/features/auth/presentation/auth_cubit.dart';
import 'package:zouq/features/catalog/data/catalog_repository.dart';
import 'package:zouq/features/catalog/presentation/catalog_cubit.dart';
import 'package:zouq/features/design_editor/data/design_repository.dart';
import 'package:zouq/features/design_editor/presentation/design_editor_cubit.dart';
import 'package:zouq/features/feed/data/feed_repository.dart';
import 'package:zouq/features/feed/presentation/feed_cubit.dart';
import 'package:zouq/features/my_designs/presentation/my_designs_cubit.dart';
import 'package:zouq/features/orders/data/orders_repository.dart';
import 'package:zouq/features/orders/presentation/orders_cubit.dart';
import 'package:zouq/features/profile/data/profile_repository.dart';
import 'package:zouq/features/profile/presentation/profile_cubit.dart';

final sl = GetIt.instance;

Future<void> initDependencies() async {
  const storage = FlutterSecureStorage();
  sl.registerSingleton<FlutterSecureStorage>(storage);

  sl.registerLazySingleton<Dio>(
    () => Dio(
      BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: ApiConfig.connectTimeout,
        receiveTimeout: ApiConfig.receiveTimeout,
        headers: {'Accept': 'application/json', 'Content-Type': 'application/json'},
      ),
    ),
  );

  sl.registerLazySingleton(() => ApiClient(sl(), sl()));
  sl.registerLazySingleton(() => AuthRepository(sl(), sl()));
  sl.registerLazySingleton(() => CatalogRepository(sl()));
  sl.registerLazySingleton(() => DesignRepository(sl()));
  sl.registerLazySingleton(() => FeedRepository(sl()));
  sl.registerLazySingleton(() => OrdersRepository(sl()));
  sl.registerLazySingleton(() => ProfileRepository(sl()));

  sl.registerLazySingleton(() => AuthCubit(sl()));
  sl.registerFactory(() => CatalogCubit(sl()));
  sl.registerFactory(() => ProductConfigCubit(sl()));
  sl.registerFactory(() => DesignEditorCubit(sl(), sl(), sl()));
  sl.registerFactory(() => FeedCubit(sl()));
  sl.registerFactory(() => OrdersCubit(sl()));
  sl.registerFactory(() => ProfileCubit(sl()));
  sl.registerFactory(() => MyDesignsCubit(sl()));
}
