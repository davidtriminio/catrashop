import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/api_url.dart';
import 'core/providers.dart';
import 'core/format.dart';
import 'core/notifications.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initFormats();
  await initBackground();
  final apiUrl = await resolveApiUrl();
  runApp(ProviderScope(
    overrides: [initialApiUrlProvider.overrideWithValue(apiUrl)],
    child: const CommerceHubAdminApp(),
  ));
}
