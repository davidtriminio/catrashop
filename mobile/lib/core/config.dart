/// Base URL of the Catrashop API. Override per build:
/// `flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5227`
const String apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'https://api.davidtriminio.dev',
);

/// Permissions that make an account eligible to use this app (granted to OrderManager/Administrator).
const String permOrdersRead = 'orders.read_all';
const String permOrdersManage = 'orders.manage';

/// How often the app polls for new orders while it is open.
const Duration foregroundPollInterval = Duration(seconds: 60);

/// Android's minimum for periodic background work.
const Duration backgroundPollInterval = Duration(minutes: 15);

/// Re-lock (biometric) after the app spent this long in the background.
const Duration relockAfter = Duration(seconds: 30);
