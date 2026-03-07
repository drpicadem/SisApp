import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'dart:io' show Platform;
import 'package:url_launcher/url_launcher.dart';
import 'package:flutter_stripe/flutter_stripe.dart';
import '../services/api_service.dart';
import '../models/appointment.dart';
import 'auth_provider.dart';

class PaymentProvider extends ChangeNotifier {
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  PaymentProvider(this._authProvider);

  bool get isLoading => _isLoading;

  bool get _isMobilePlatform {
    if (kIsWeb) return false;
    return Platform.isAndroid || Platform.isIOS;
  }

  Future<bool> initiatePayment(Appointment appointment, String serviceName, double amount, String paymentMethod) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    try {
      if (_isMobilePlatform) {
        return await _payWithPaymentSheet(appointment, serviceName, amount);
      } else {
        return await _payWithBrowser(appointment, serviceName, amount, paymentMethod);
      }
    } catch (e) {
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> _payWithPaymentSheet(Appointment appointment, String serviceName, double amount) async {
    try {
      final intentData = await _apiService.createPaymentIntent(
        _authProvider!.tokenResponse!.token,
        appointment.id!,
        serviceName,
        amount,
        _authProvider!.userId,
        _authProvider!.email ?? 'noreply@sisapp.com',
      );

      if (intentData == null) {
        _isLoading = false;
        notifyListeners();
        return false;
      }

      final publishableKey = intentData['publishableKey'];
      if (publishableKey != null && publishableKey.toString().startsWith('pk_')) {
        Stripe.publishableKey = publishableKey;
      }

      await Stripe.instance.initPaymentSheet(
        paymentSheetParameters: SetupPaymentSheetParameters(
          paymentIntentClientSecret: intentData['clientSecret'],
          merchantDisplayName: 'ŠišApp',
          style: ThemeMode.system,
        ),
      );

      await Stripe.instance.presentPaymentSheet();

      final status = await _apiService.confirmPayment(
        appointment.id!,
        _authProvider!.tokenResponse!.token,
      );

      _isLoading = false;
      notifyListeners();
      return status == 'Paid';
    } on StripeException {
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> _payWithBrowser(Appointment appointment, String serviceName, double amount, String paymentMethod) async {
    try {
      final url = await _apiService.createCheckoutSession(
        _authProvider!.tokenResponse!.token,
        appointment.id!,
        serviceName,
        amount,
        _authProvider!.userId,
        _authProvider!.email ?? 'noreply@sisapp.com',
        "${ApiService.baseUrl}/Payment/success",
        "${ApiService.baseUrl}/Payment/cancel",
        paymentMethod,
      );

      if (url != null) {
        final uri = Uri.parse(url);
        if (await canLaunchUrl(uri)) {
          await launchUrl(uri, mode: LaunchMode.externalApplication);
        }
      }

      _isLoading = false;
      notifyListeners();

      return await monitorPaymentStatus(appointment.id!);
    } catch (e) {
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> monitorPaymentStatus(int appointmentId) async {
    if (_authProvider?.tokenResponse == null) return false;

    await Future.delayed(Duration(seconds: 5));

    for (int i = 0; i < 90; i++) {
      await Future.delayed(Duration(seconds: 2));
      try {
        final status = await _apiService.checkPaymentStatus(
          appointmentId,
          _authProvider!.tokenResponse!.token,
        );
        if (status == "Paid") {
          return true;
        }
      } catch (e) {
        // continue polling
      }
    }
    return false;
  }
}
