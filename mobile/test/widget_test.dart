import 'package:flutter_test/flutter_test.dart';
import 'package:zouq/config/app_theme.dart';

void main() {
  test('AppTheme builds Material 3 theme', () {
    final theme = AppTheme.light();
    expect(theme.useMaterial3, isTrue);
  });
}
