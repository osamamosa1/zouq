class ApiConfig {
  ApiConfig._();

  /// Android emulator → host machine localhost.
  static const String baseUrl = String.fromEnvironment(
    'ZOUQ_API',
    defaultValue: 'http://10.0.2.2:5280',
  );

  static const Duration connectTimeout = Duration(seconds: 15);
  static const Duration receiveTimeout = Duration(seconds: 30);
}
