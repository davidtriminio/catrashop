import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api_url.dart';
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

  Future<void> _changeApiUrl(BuildContext context, WidgetRef ref) async {
    final done = await showDialog<bool>(context: context, builder: (_) => const ApiUrlDialog());
    if (done == true && context.mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Servidor cambiado. Inicia sesión de nuevo.')),
      );
    }
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
              subtitle: Text(
                ref.watch(apiUrlProvider) == apiBaseUrl
                    ? '${ref.watch(apiUrlProvider)}\nPredeterminada de esta versión'
                    : '${ref.watch(apiUrlProvider)}\nCambiada en este dispositivo',
              ),
              isThreeLine: true,
              trailing: TextButton(
                onPressed: () => _changeApiUrl(context, ref),
                child: const Text('Cambiar'),
              ),
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

/// Edits the address of the API this device talks to. Saving (or going back to the built-in address) signs the admin
/// out, because the session belongs to the previous server.
class ApiUrlDialog extends ConsumerStatefulWidget {
  const ApiUrlDialog({super.key});

  @override
  ConsumerState<ApiUrlDialog> createState() => _ApiUrlDialogState();
}

class _ApiUrlDialogState extends ConsumerState<ApiUrlDialog> {
  late final TextEditingController _controller;
  String? _error;
  String? _result; // outcome of "Probar conexión"
  bool _resultOk = false;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: ref.read(apiUrlProvider));
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  String? _validUrl() {
    final url = normalizeApiUrl(_controller.text);
    if (url == null) {
      setState(() {
        _error = 'Escribe una dirección completa, por ejemplo http://192.168.1.10:5227';
        _result = null;
      });
    } else {
      setState(() => _error = null);
    }
    return url;
  }

  Future<void> _test() async {
    final url = _validUrl();
    if (url == null) return;
    setState(() { _busy = true; _result = null; });
    final ok = await ref.read(apiReachableProvider)(url);
    if (!mounted) return;
    setState(() {
      _busy = false;
      _resultOk = ok;
      _result = ok ? 'El servidor responde.' : 'No se pudo conectar con ese servidor.';
    });
  }

  Future<void> _save() async {
    final url = _validUrl();
    if (url == null) return;
    setState(() => _busy = true);
    await ref.read(apiUrlProvider.notifier).change(url);
    if (mounted) Navigator.pop(context, true);
  }

  Future<void> _reset() async {
    setState(() => _busy = true);
    await ref.read(apiUrlProvider.notifier).reset();
    if (mounted) Navigator.pop(context, true);
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final builtIn = ref.watch(apiUrlProvider) == apiBaseUrl;
    return AlertDialog(
      title: const Text('Servidor de la API'),
      content: SingleChildScrollView(
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          TextField(
            controller: _controller,
            keyboardType: TextInputType.url,
            autocorrect: false,
            enableSuggestions: false,
            decoration: InputDecoration(labelText: 'Dirección', errorText: _error, errorMaxLines: 3),
            onChanged: (_) => setState(() { _error = null; _result = null; }),
          ),
          if (_result != null) ...[
            const SizedBox(height: Gap.s2),
            Text(_result!, style: TextStyle(color: _resultOk ? scheme.primary : scheme.error)),
          ],
          const SizedBox(height: Gap.s3),
          Text(
            'En el emulador de Android usa http://10.0.2.2:5227 (el HTTP sin cifrar solo funciona en builds de depuración). Al guardar se cierra la sesión y hay que iniciarla de nuevo.',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
          ),
          Text(
            'Predeterminada: $apiBaseUrl',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
          ),
        ]),
      ),
      actions: [
        if (!builtIn) TextButton(onPressed: _busy ? null : _reset, child: const Text('Restablecer')),
        TextButton(onPressed: _busy ? null : _test, child: const Text('Probar')),
        TextButton(onPressed: _busy ? null : () => Navigator.pop(context, false), child: const Text('Cancelar')),
        FilledButton(onPressed: _busy ? null : _save, child: const Text('Guardar')),
      ],
    );
  }
}
