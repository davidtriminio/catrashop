import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/config.dart';
import '../../core/notifications.dart';
import '../../core/providers.dart';
import '../../core/theme.dart';

class SettingsPage extends ConsumerWidget {
  const SettingsPage({super.key});

  Future<void> _confirmLogout(BuildContext context, WidgetRef ref) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Cerrar sesión'),
        content: const Text('Tendrás que volver a iniciar sesión para ver los pedidos.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Volver')),
          FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Cerrar sesión')),
        ],
      ),
    );
    if (ok == true) await ref.read(sessionProvider.notifier).logout();
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final profile = ref.watch(profileProvider).value;
    final email = profile?.email ?? '';

    Widget section(String title) => Padding(
          padding: const EdgeInsets.fromLTRB(Gap.s1, Gap.s5, Gap.s1, Gap.s2),
          child: Text(title.toUpperCase(),
              style: theme.textTheme.labelSmall?.copyWith(color: scheme.onSurfaceVariant, letterSpacing: 1, fontWeight: FontWeight.w700)),
        );

    return Scaffold(
      appBar: AppBar(title: const Text('Ajustes')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(Gap.s4, Gap.s2, Gap.s4, Gap.s5),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(Gap.s4),
              child: Row(children: [
                CircleAvatar(
                  radius: 24,
                  backgroundColor: scheme.primary,
                  child: Text(email.isEmpty ? '?' : email[0].toUpperCase(), style: TextStyle(color: scheme.onPrimary, fontWeight: FontWeight.w700, fontSize: 18)),
                ),
                const SizedBox(width: Gap.s3),
                Expanded(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(email.isEmpty ? 'Cuenta' : email, style: theme.textTheme.titleMedium, overflow: TextOverflow.ellipsis),
                    const SizedBox(height: 2),
                    Text(
                      (profile?.roles.isEmpty ?? true) ? 'Administración de pedidos' : profile!.roles.join(' · '),
                      style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
                    ),
                  ]),
                ),
              ]),
            ),
          ),

          section('Seguridad'),
          Card(
            child: Column(children: [
              ListTile(
                leading: const Icon(Icons.fingerprint),
                title: const Text('Bloqueo biométrico'),
                subtitle: Text('Se pide al abrir la app y tras ${relockAfter.inSeconds} s en segundo plano.'),
              ),
              const Divider(),
              ListTile(
                leading: const Icon(Icons.lock_outline),
                title: const Text('Bloquear ahora'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => ref.read(lockProvider.notifier).lock(),
              ),
            ]),
          ),

          section('Notificaciones'),
          Card(
            child: Column(children: [
              ListTile(
                leading: const Icon(Icons.notifications_none),
                title: const Text('Avisos de pedidos nuevos'),
                subtitle: Text(
                  'Cada ${foregroundPollInterval.inSeconds} s con la app abierta y cada ~${backgroundPollInterval.inMinutes} min en segundo plano.',
                ),
              ),
              const Divider(),
              ListTile(
                leading: const Icon(Icons.notifications_active_outlined),
                title: const Text('Permitir notificaciones'),
                trailing: const Icon(Icons.chevron_right),
                onTap: () => requestNotificationPermission(),
              ),
            ]),
          ),

          section('Servidor'),
          Card(
            child: ListTile(
              leading: const Icon(Icons.cloud_outlined),
              title: const Text('API'),
              subtitle: Text(apiBaseUrl),
            ),
          ),

          const SizedBox(height: Gap.s5),
          OutlinedButton.icon(
            style: OutlinedButton.styleFrom(foregroundColor: scheme.error, side: BorderSide(color: scheme.error.withValues(alpha: 0.5))),
            onPressed: () => _confirmLogout(context, ref),
            icon: const Icon(Icons.logout),
            label: const Text('Cerrar sesión'),
          ),
        ],
      ),
    );
  }
}
