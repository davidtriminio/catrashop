import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

import '../../core/providers.dart';
import 'widgets.dart';

/// Shows the receipt a customer attached to a bank transfer. Images are shown in the app (pinch to zoom); a PDF is
/// saved to a temporary file and handed to the device's PDF viewer.
class ReceiptPage extends ConsumerStatefulWidget {
  const ReceiptPage({super.key, required this.paymentId, required this.orderNumber});
  final String paymentId;
  final String orderNumber;

  @override
  ConsumerState<ReceiptPage> createState() => _ReceiptPageState();
}

class _ReceiptPageState extends ConsumerState<ReceiptPage> {
  late Future<({List<int> bytes, String contentType})> _file = _load();

  Future<({List<int> bytes, String contentType})> _load() => ref.read(ordersRepositoryProvider).receipt(widget.paymentId);

  Future<void> _openPdf(List<int> bytes) async {
    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/comprobante-${widget.paymentId}.pdf');
    await file.writeAsBytes(bytes, flush: true);
    final result = await OpenFilex.open(file.path, type: 'application/pdf');
    if (result.type != ResultType.done && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('No hay una app para abrir PDF en este dispositivo.')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Comprobante')),
      body: FutureBuilder(
        future: _file,
        builder: (context, snap) {
          if (snap.connectionState != ConnectionState.done) return const Center(child: CircularProgressIndicator());
          if (snap.hasError) {
            return LoadError(
              title: 'No se pudo abrir el comprobante',
              error: snap.error!,
              onRetry: () => setState(() => _file = _load()),
            );
          }
          final f = snap.data!;
          if (f.contentType.contains('pdf')) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(mainAxisSize: MainAxisSize.min, children: [
                  Icon(Icons.picture_as_pdf_outlined, size: 56, color: theme.colorScheme.primary),
                  const SizedBox(height: 12),
                  Text('Comprobante en PDF · pedido ${widget.orderNumber}', textAlign: TextAlign.center, style: theme.textTheme.titleMedium),
                  const SizedBox(height: 20),
                  FilledButton.icon(onPressed: () => _openPdf(f.bytes), icon: const Icon(Icons.open_in_new), label: const Text('Abrir PDF')),
                ]),
              ),
            );
          }
          return InteractiveViewer(
            minScale: 0.8,
            maxScale: 6,
            child: Center(
              child: Image.memory(
                Uint8List.fromList(f.bytes),
                fit: BoxFit.contain,
                errorBuilder: (_, _, _) => const Padding(padding: EdgeInsets.all(24), child: Text('El archivo no es una imagen válida.')),
              ),
            ),
          );
        },
      ),
    );
  }
}
