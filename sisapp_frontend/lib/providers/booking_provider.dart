import 'package:flutter/material.dart';
import '../models/appointment.dart';
import '../services/api_service.dart';
import 'auth_provider.dart';

class BookingProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  bool _isLoading = false;
  List<String> _availableSlots = [];

  BookingProvider(this._authProvider);

  bool get isLoading => _isLoading;
  List<String> get availableSlots => _availableSlots;

  Future<void> fetchAvailableSlots(int barberId, DateTime date) async {
    if (_authProvider?.tokenResponse == null) return;
    
    _isLoading = true;
    notifyListeners();

    try {
      _availableSlots = await _apiService.getAvailableSlots(
        barberId,
        date,
        _authProvider!.tokenResponse!.token,
      );
    } catch (e) {
      print('Error fetching slots: $e');
      _availableSlots = [];
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<String?> createAppointment(Appointment appointment) async {
    if (_authProvider?.tokenResponse == null) return "Not authenticated";

    _isLoading = true;
    notifyListeners();

    try {
      final error = await _apiService.createAppointment(
        appointment,
        _authProvider!.tokenResponse!.token,
      );
      
      _isLoading = false;
      notifyListeners();
      return error; // Will be null if success, or error string
    } catch (e) {
      print('Error creating appointment: $e');
      _isLoading = false;
      notifyListeners();
      return e.toString();
    }
  }
}
