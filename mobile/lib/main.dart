import 'package:flutter/material.dart';
import 'package:zouq/app.dart';
import 'package:zouq/injection_container.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initDependencies();
  runApp(const ZouqApp());
}
