import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../services/api_service.dart';
import '../models/appointment.dart';
import 'auth_provider.dart';

class PaymentProvider extends ChangeNotifier {
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  PaymentProvider(this._authProvider);

  bool get isLoading => _isLoading;

  Future<void> initiatePayment(Appointment appointment, String serviceName, double amount) async {
    if (_authProvider?.tokenResponse == null) return;

    _isLoading = true;
    notifyListeners();

    try {
      final url = await _apiService.createCheckoutSession(
        _authProvider!.tokenResponse!.token,
        appointment.id!,
        serviceName,
        amount,
        _authProvider!.userId,
        _authProvider!.email ?? 'noreply@sisapp.com',
        "http://localhost:7100/api/Payment/success", 
        "http://localhost:7100/api/Payment/cancel",
      );

      if (url != null) {
        final uri = Uri.parse(url);
        if (await canLaunchUrl(uri)) {
          await launchUrl(uri, mode: LaunchMode.externalApplication);
        } else {
          print('Could not launch $url');
        }
      }
    } catch (e) {
      print('Payment initiation error: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<bool> monitorPaymentStatus(int appointmentId) async {
    if (_authProvider?.tokenResponse == null) return false;
    
    // Poll every 2 seconds for up to 60 seconds (30 attempts)
    for (int i = 0; i < 30; i++) {
        await Future.delayed(Duration(seconds: 2));
        
        try {
            final status = await _apiService.checkPaymentStatus(
                appointmentId, 
                _authProvider!.tokenResponse!.token
            );
            
            if (status == "Paid") {
                return true;
            }
        } catch (e) {
            print("Error checking payment status: $e");
        }
    }
    return false;
  }
}

