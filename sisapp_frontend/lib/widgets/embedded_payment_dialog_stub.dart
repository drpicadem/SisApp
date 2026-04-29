import 'package:flutter/material.dart';
import 'package:webview_flutter/webview_flutter.dart';

Future<bool> showEmbeddedPaymentDialog(
  BuildContext context, {
  required String paymentFormUrl,
}) async {
  final result = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (ctx) => _EmbeddedPaymentDialog(paymentFormUrl: paymentFormUrl),
  );

  return result ?? false;
}

class _EmbeddedPaymentDialog extends StatefulWidget {
  final String paymentFormUrl;

  const _EmbeddedPaymentDialog({required this.paymentFormUrl});

  @override
  State<_EmbeddedPaymentDialog> createState() => _EmbeddedPaymentDialogState();
}

class _EmbeddedPaymentDialogState extends State<_EmbeddedPaymentDialog> {
  late final WebViewController _controller;
  bool _isLoading = true;

  static const String _callbackPrefix = 'sisapp-payment://result';

  @override
  void initState() {
    super.initState();
    _controller = WebViewController()
      ..setJavaScriptMode(JavaScriptMode.unrestricted)
      ..setNavigationDelegate(
        NavigationDelegate(
          onPageFinished: (_) {
            if (mounted) {
              setState(() => _isLoading = false);
            }
          },
          onNavigationRequest: (request) {
            if (request.url.startsWith(_callbackPrefix)) {
              final uri = Uri.parse(request.url);
              final status = uri.queryParameters['status'];
              Navigator.of(context).pop(status == 'success');
              return NavigationDecision.prevent;
            }
            return NavigationDecision.navigate;
          },
        ),
      )
      ..loadRequest(Uri.parse(widget.paymentFormUrl));
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      insetPadding: const EdgeInsets.all(12),
      child: SizedBox(
        width: 720,
        height: 620,
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              child: Row(
                children: [
                  const Expanded(
                    child: Text(
                      'Plaćanje karticom',
                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
                    ),
                  ),
                  IconButton(
                    onPressed: () => Navigator.of(context).pop(false),
                    icon: const Icon(Icons.close),
                  )
                ],
              ),
            ),
            const Divider(height: 1),
            Expanded(
              child: Stack(
                children: [
                  WebViewWidget(controller: _controller),
                  if (_isLoading)
                    const Center(child: CircularProgressIndicator()),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
