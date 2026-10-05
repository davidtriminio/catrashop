import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/notifications.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initBackground();
  runApp(const ProviderScope(child: CommerceHubAdminApp()));
}
