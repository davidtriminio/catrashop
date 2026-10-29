import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:workmanager/workmanager.dart';

import '../features/orders/order_models.dart';
import '../features/orders/orders_repository.dart';
import 'api_client.dart';
import 'api_url.dart';
import 'config.dart';
import 'format.dart';
import 'token_store.dart';

const _taskName = 'commercehub.orderCheck';
const _taskId = 'dev.davidtriminio.commercehub.orderCheck';

final _plugin = FlutterLocalNotificationsPlugin();
bool _pluginReady = false;

Future<void> _ensurePlugin() async {
  if (_pluginReady) return;
  await _plugin.initialize(
    settings: const InitializationSettings(
      android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      iOS: DarwinInitializationSettings(
        requestAlertPermission: false,
        requestBadgePermission: false,
        requestSoundPermission: false,
      ),
    ),
  );
  _pluginReady = true;
}

/// Asks the OS for notification permission (Android 13+/iOS). Safe to call repeatedly.
Future<void> requestNotificationPermission() async {
  await _ensurePlugin();
  await _plugin
      .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
      ?.requestNotificationsPermission();
  await _plugin
      .resolvePlatformSpecificImplementation<IOSFlutterLocalNotificationsPlugin>()
      ?.requestPermissions(alert: true, badge: true, sound: true);
}

Future<void> showNewOrders(List<OrderSummary> orders) async {
  if (orders.isEmpty) return;
  await _ensurePlugin();
  final first = orders.first;
  final title = orders.length == 1 ? 'Nuevo pedido ${first.orderNumber}' : '${orders.length} pedidos nuevos';
  final body = orders.length == 1
      ? '${formatMoney(first.grandTotal, first.currency)} · ${first.itemCount} artículo(s)'
      : orders.map((o) => o.orderNumber).take(4).join(', ');
  await _plugin.show(
    id: orders.length == 1 ? first.orderNumber.hashCode & 0x7fffffff : 1,
    title: title,
    body: body,
    notificationDetails: const NotificationDetails(
      android: AndroidNotificationDetails(
        'orders',
        'Pedidos nuevos',
        channelDescription: 'Aviso cuando entra un pedido nuevo',
        importance: Importance.high,
        priority: Priority.high,
      ),
      iOS: DarwinNotificationDetails(),
    ),
  );
}

/// Entry point of the background isolate (WorkManager on Android, BGTaskScheduler on iOS).
/// Runs without Riverpod/UI: reads the stored session, polls once, notifies.
@pragma('vm:entry-point')
void backgroundDispatcher() {
  Workmanager().executeTask((task, _) async {
    try {
      final store = TokenStore();
      if (await store.read() == null) return true; // signed out
      final watcher = NewOrderWatcher(OrdersRepository(ApiClient(store, baseUrl: await resolveApiUrl(), canRefresh: false)), store);
      await showNewOrders(await watcher.check());
      return true;
    } catch (_) {
      return true; // transient (offline): try again next cycle
    }
  });
}

Future<void> initBackground() => Workmanager().initialize(backgroundDispatcher);

Future<void> startBackgroundPolling() => Workmanager().registerPeriodicTask(
      _taskId,
      _taskName,
      frequency: backgroundPollInterval,
      constraints: Constraints(networkType: NetworkType.connected),
      existingWorkPolicy: ExistingPeriodicWorkPolicy.update,
    );

Future<void> stopBackgroundPolling() => Workmanager().cancelByUniqueName(_taskId);
