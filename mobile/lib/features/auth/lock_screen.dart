import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';

/// Shown on cold start and after the app spent time in the background. Prompts biometrics (PIN as fallback).
class LockScreen extends ConsumerStatefulWidget {
  const LockScreen({super.key});

  @override
  ConsumerState<LockScreen> createState() => _LockScreenState();
}

class _LockScreenState extends ConsumerState<LockScreen> {
  bool _busy = false;
  BiometricResult? _result;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _unlock());
  }

  Future<void> _unlock() async {
    if (_busy) return;
    setState(() => _busy = true);
    final r = await ref.read(biometricProvider).authenticate();
    if (!mounted) return;
    if (r == BiometricResult.success) {
      ref.read(lockProvider.notifier).unlock();
    } else {
      setState(() {
        _busy = false;
        _result = r;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final unsupported = _result == BiometricResult.unsupported;
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(32),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.lock, size: 64, color: scheme.primary),
                const SizedBox(height: 16),
                Text('CommerceHub Admin', style: Theme.of(context).textTheme.headlineSmall),
                const SizedBox(height: 8),
                Text(
                  unsupported
                      ? 'Este dispositivo no tiene bloqueo de pantalla ni biometría. Configúralo en Ajustes para usar la app.'
                      : 'Verifica tu identidad para continuar.',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 24),
                FilledButton.icon(
                  onPressed: _busy ? null : _unlock,
                  icon: const Icon(Icons.fingerprint),
                  label: const Text('Desbloquear'),
                ),
                TextButton(
                  onPressed: () => ref.read(sessionProvider.notifier).logout(),
                  child: const Text('Cerrar sesión'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
