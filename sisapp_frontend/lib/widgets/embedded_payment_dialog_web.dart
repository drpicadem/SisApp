import 'dart:async';
import 'dart:convert';
import 'dart:html' as html;
import 'dart:ui_web' as ui_web;

import 'package:flutter/material.dart';

Future<bool> showEmbeddedPaymentDialog(
  BuildContext context, {
  required String paymentFormUrl,
}) async {
  final completer = Completer<bool>();
  final viewType = 'embedded-stripe-${DateTime.now().toUtc().microsecondsSinceEpoch}';
  late final html.IFrameElement iframe;
  StreamSubscription<html.MessageEvent>? sub;

  iframe = html.IFrameElement()
    ..src = paymentFormUrl
    ..style.border = '0'
    ..style.width = '100%'
    ..style.height = '560px';

  ui_web.platformViewRegistry.registerViewFactory(viewType, (int _) => iframe);

  sub = html.window.onMessage.listen((event) {
    final data = event.data;
    if (data is! String) return;

    try {
      final decoded = jsonDecode(data);
      if (decoded is! Map<String, dynamic>) return;
      if (decoded['type'] != 'sisapp-embedded-payment') return;

      final status = decoded['status']?.toString();
      if (status == 'success') {
        if (!completer.isCompleted) completer.complete(true);
      } else if (status == 'cancel') {
        if (!completer.isCompleted) completer.complete(false);
      }
    } catch (_) {}
  });

  final dialogFuture = showDialog(
    context: context,
    barrierDismissible: false,
    builder: (ctx) => AlertDialog(
      title: const Text('Plaćanje karticom'),
      content: SizedBox(
        width: 760,
        height: 600,
        child: HtmlElementView(viewType: viewType),
      ),
      actions: [
        TextButton(
          onPressed: () {
            if (!completer.isCompleted) completer.complete(false);
            Navigator.of(ctx).pop();
          },
          child: const Text('Zatvori'),
        ),
      ],
    ),
  );

  dialogFuture.then((_) {
    if (!completer.isCompleted) completer.complete(false);
  });

  final result = await completer.future;
  await sub.cancel();
  
  return result;
}
