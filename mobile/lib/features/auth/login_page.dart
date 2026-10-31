import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers.dart';
import '../../core/theme.dart';
import 'brand.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;
  bool _obscure = true;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(sessionProvider.notifier).login(_email.text, _password.text);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(Gap.s5),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                const Center(child: BrandMark(size: 34)),
                const SizedBox(height: Gap.s2),
                Text('Panel de pedidos', textAlign: TextAlign.center, style: theme.textTheme.bodyLarge?.copyWith(color: scheme.onSurfaceVariant)),
                const SizedBox(height: Gap.s6),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(Gap.s5),
                    child: Form(
                      key: _form,
                      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
                        Text('Inicia sesión', style: theme.textTheme.titleLarge),
                        const SizedBox(height: Gap.s1),
                        Text('Solo cuentas de administración.', style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant)),
                        const SizedBox(height: Gap.s5),
                        TextFormField(
                          controller: _email,
                          keyboardType: TextInputType.emailAddress,
                          autofillHints: const [AutofillHints.username],
                          textInputAction: TextInputAction.next,
                          decoration: const InputDecoration(labelText: 'Correo'),
                          validator: (v) => (v == null || !v.contains('@')) ? 'Correo inválido' : null,
                        ),
                        const SizedBox(height: Gap.s4),
                        TextFormField(
                          controller: _password,
                          obscureText: _obscure,
                          autofillHints: const [AutofillHints.password],
                          onFieldSubmitted: (_) => _submit(),
                          decoration: InputDecoration(
                            labelText: 'Contraseña',
                            suffixIcon: IconButton(
                              tooltip: _obscure ? 'Mostrar contraseña' : 'Ocultar contraseña',
                              icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                              onPressed: () => setState(() => _obscure = !_obscure),
                            ),
                          ),
                          validator: (v) => (v == null || v.isEmpty) ? 'Ingresa tu contraseña' : null,
                        ),
                        if (_error != null) ...[
                          const SizedBox(height: Gap.s4),
                          Container(
                            padding: const EdgeInsets.all(Gap.s3),
                            decoration: BoxDecoration(color: scheme.error.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(12)),
                            child: Row(children: [
                              Icon(Icons.error_outline, size: 18, color: scheme.error),
                              const SizedBox(width: Gap.s2),
                              Expanded(child: Text(_error!, style: TextStyle(color: scheme.error))),
                            ]),
                          ),
                        ],
                        const SizedBox(height: Gap.s5),
                        FilledButton(
                          onPressed: _busy ? null : _submit,
                          child: _busy
                              ? SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: scheme.onPrimary))
                              : const Text('Iniciar sesión'),
                        ),
                      ]),
                    ),
                  ),
                ),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}
