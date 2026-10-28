import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../../core/theme.dart';
import 'brand.dart';

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
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final unsupported = _result == BiometricResult.unsupported;
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(Gap.s6),
            child: Column(mainAxisSize: MainAxisSize.min, children: [
              Container(
                width: 88,
                height: 88,
                decoration: const BoxDecoration(color: AppColors.lime, shape: BoxShape.circle),
                child: const Icon(Icons.lock_outline, size: 40, color: AppColors.limeInk),
              ),
              const SizedBox(height: Gap.s5),
              const BrandMark(size: 28),
              const SizedBox(height: Gap.s3),
              Text(
                unsupported
                    ? 'Este dispositivo no tiene bloqueo de pantalla ni biometría. Configúralo en Ajustes para usar la app.'
                    : 'Verifica tu identidad para continuar.',
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyLarge?.copyWith(color: scheme.onSurfaceVariant),
              ),
              const SizedBox(height: Gap.s5),
              FilledButton.icon(
                onPressed: _busy ? null : _unlock,
                icon: const Icon(Icons.fingerprint),
                label: const Text('Desbloquear'),
              ),
              const SizedBox(height: Gap.s2),
              TextButton(
                onPressed: () => ref.read(sessionProvider.notifier).logout(),
                child: const Text('Cerrar sesión'),
              ),
            ]),
          ),
        ),
      ),
    );
  }
}
