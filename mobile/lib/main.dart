import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/format.dart';
import 'core/notifications.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initFormats();
  await initBackground();
  runApp(const ProviderScope(child: CommerceHubAdminApp()));
}
