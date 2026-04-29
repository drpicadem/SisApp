import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb, debugPrint;
import 'dart:async';
import 'dart:io' show Platform;
import 'package:flutter_paypal_native/flutter_paypal_native.dart';
import 'package:flutter_paypal_native/str_helper.dart';
import 'package:flutter_paypal_native/models/custom/environment.dart';
import 'package:flutter_paypal_native/models/custom/currency_code.dart';
import 'package:flutter_paypal_native/models/custom/user_action.dart';
import 'package:flutter_paypal_native/models/custom/order_callback.dart';
import 'package:flutter_paypal_native/models/custom/purchase_unit.dart';
import '../services/api_service.dart';
import '../models/appointment.dart';
import 'auth_provider.dart';
import '../widgets/embedded_payment_dialog.dart';

class PaymentProvider extends ChangeNotifier {
  bool _isLoading = false;
  String? _lastError;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;
  bool _paypalInitialized = false;

  PaymentProvider(this._authProvider);

  bool get isLoading => _isLoading;
  String? get lastError => _lastError;

  bool get _isMobilePlatform {
    if (kIsWeb) return false;
    return Platform.isAndroid || Platform.isIOS;
  }

  Future<bool> initiatePayment(
    BuildContext context,
    Appointment appointment,
    String serviceName,
    double amount,
    String paymentMethod,
  ) async {
    if (_authProvider?.tokenResponse == null) return false;

    _lastError = null;
    _isLoading = true;
    notifyListeners();

    try {
      final method = paymentMethod.toLowerCase();

      if (method == 'paypal') {
        if (!_isMobilePlatform) {
          return _failWith("PayPal je podržan samo u mobilnoj aplikaciji.");
        }
        return await _payWithPayPalNative(appointment, amount);
      }

      if (method == 'card') {
        return await _payWithEmbeddedWebForm(context, appointment, amount);
      }

      return _failWith("Nepoznata metoda plaćanja: $paymentMethod");
    } catch (_) {
      return _failWith("Plaćanje trenutno nije dostupno. Pokušajte ponovo.");
    }
  }

  bool _failWith(String message) {
    _lastError = message;
    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<bool> _payWithEmbeddedWebForm(
    BuildContext context,
    Appointment appointment,
    double amount,
  ) async {
    try {
      final token = _authProvider!.tokenResponse!.token;
      final formUrl = _apiService.buildEmbeddedPaymentFormUrl(
        appointmentId: appointment.id!,
        token: token,
        amount: amount,
        clientPlatform: _isMobilePlatform ? 'mobile' : 'web',
      );

      final success = await showEmbeddedPaymentDialog(context, paymentFormUrl: formUrl);
      if (!success) {
        await _apiService.cancelPendingStripePayment(token, appointment.id!);
        _lastError = "Plaćanje je otkazano ili nije uspjelo. Rezervacija ostaje neplaćena.";
      }
      _isLoading = false;
      notifyListeners();
      return success;
    } catch (_) {
      return _failWith("Greška pri otvaranju Stripe forme.");
    }
  }

  Future<bool> _payWithPayPalNative(Appointment appointment, double amount) async {
    try {
      debugPrint('[Payment][PayPal] Native START appointmentId=${appointment.id} amount=$amount');
      final token = _authProvider!.tokenResponse!.token;
      final pendingOrderId = await _apiService.createPayPalOrder(token, appointment.id!);
      if (pendingOrderId == null || pendingOrderId.isEmpty) {
        return _failWith("Neuspješno pokretanje PayPal plaćanja.");
      }

      await _initPayPalSdkIfNeeded();

      final orderId = await _startNativePayPalCheckout(amount, pendingOrderId: pendingOrderId);
      if (orderId == null || orderId.isEmpty) {
        await _apiService.cancelPendingPayPalOrder(token, appointment.id!);
        _lastError ??= "PayPal plaćanje je otkazano.";
        _isLoading = false;
        notifyListeners();
        return false;
      }

      final captureStatus = await _apiService.capturePayPalOrder(token, orderId, appointment.id!);
      if (captureStatus != 'Paid') {
        await _apiService.cancelPendingPayPalOrder(token, appointment.id!);
      }

      _isLoading = false;
      notifyListeners();
      return captureStatus == 'Paid';
    } catch (e) {
      return _failWith("Greška tokom PayPal toka: $e");
    }
  }

  Future<void> _initPayPalSdkIfNeeded() async {
    if (_paypalInitialized) return;

    if (_authProvider?.tokenResponse == null) {
      throw Exception("Korisnik nije autentifikovan.");
    }

    final config = await _apiService.getPayPalMobileConfig(_authProvider!.tokenResponse!.token);
    final clientId = (config['clientId']?.toString() ?? '').trim();
    final environmentRaw = (config['environment']?.toString() ?? 'sandbox').trim().toLowerCase();
    if (clientId.isEmpty) {
      throw Exception("PayPal ClientId nije dostupan sa servera.");
    }

    await FlutterPaypalNative.instance.init(
      returnUrl: "com.sisapp.mobile://paypalpay",
      clientID: clientId,
      payPalEnvironment: environmentRaw == 'production'
          ? FPayPalEnvironment.live
          : FPayPalEnvironment.sandbox,
      currencyCode: FPayPalCurrencyCode.eur,
      action: FPayPalUserAction.payNow,
    );

    _paypalInitialized = true;
  }

  Future<String?> _startNativePayPalCheckout(double amount, {required String pendingOrderId}) async {
    final completer = Completer<String?>();
    final plugin = FlutterPaypalNative.instance;

    plugin.setPayPalOrderCallback(
      callback: FPayPalOrderCallback(
        onCancel: () {
          // Delay so onSuccess can win if both fire near-simultaneously.
          Future.delayed(const Duration(seconds: 3), () {
            if (!completer.isCompleted) completer.complete(null);
          });
        },
        onError: (data) {
          _lastError = "PayPal greška: ${data.reason}";
          if (!completer.isCompleted) completer.complete(null);
        },
        onSuccess: (data) {
          if (!completer.isCompleted) completer.complete(data.orderId);
        },
        onShippingChange: (_) {},
      ),
    );

    plugin.removeAllPurchaseItems();
    if (plugin.canAddMorePurchaseUnit) {
      plugin.addPurchaseUnit(
        FPayPalPurchaseUnit(
          amount: amount,
          referenceId: FPayPalStrHelper.getRandomString(16),
        ),
      );
    }

    plugin.makeOrder(action: FPayPalUserAction.payNow, orderId: pendingOrderId);

    final orderId = await completer.future.timeout(
      const Duration(seconds: 300),
      onTimeout: () {
        _lastError = "PayPal odgovor nije primljen. Plaćanje nije završeno.";
        return null;
      },
    );

    plugin.removeAllPurchaseItems();
    return orderId;
  }
}
