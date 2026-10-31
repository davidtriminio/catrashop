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
    // A failed load shows its error at once, with the Reintentar button. Riverpod 3 would otherwise keep retrying
    // behind a loading screen for a long time.
    retry: (retryCount, error) => null,
    overrides: [initialApiUrlProvider.overrideWithValue(apiUrl)],
    child: const CatrashopAdminApp(),
  ));
}
