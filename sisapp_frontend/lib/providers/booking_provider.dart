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
  
  List<Appointment> _appointments = [];
  List<Appointment> get appointments => _appointments;

  int _page = 1;
  final int _pageSize = 10;
  bool _hasMore = true;
  bool _isLoadingMore = false;

  bool get hasMore => _hasMore;
  bool get isLoadingMore => _isLoadingMore;

  Future<void> fetchAppointments({
    bool refresh = false,
    bool? isActive, // true=Active, false=History
    bool? isPaid,
  }) async {
    if (_authProvider?.tokenResponse == null) return;
    
    if (refresh) {
      _page = 1;
      _hasMore = true;
      _appointments.clear();
      _isLoading = true;
      notifyListeners();
    } else {
      if (!_hasMore || _isLoadingMore) return;
      _isLoadingMore = true;
      notifyListeners();
    }

    try {
      final newAppointments = await _apiService.getAppointments(
        _authProvider!.tokenResponse!.token,
        page: _page,
        pageSize: _pageSize,
        isActive: isActive,
        isPaid: isPaid,
      );

      if (newAppointments.length < _pageSize) {
        _hasMore = false;
      }

      if (refresh) {
        _appointments = newAppointments;
      } else {
        _appointments.addAll(newAppointments);
      }
      
      _page++;
    } catch (e) {
      print('Error fetching appointments: $e');
      if (refresh) _appointments = [];
    }

    _isLoading = false;
    _isLoadingMore = false;
    notifyListeners();
  }

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

  Future<dynamic> createAppointment(Appointment appointment) async {
    if (_authProvider?.tokenResponse == null) return "Not authenticated";

    _isLoading = true;
    notifyListeners();

    try {
      final result = await _apiService.createAppointment(
        appointment,
        _authProvider!.tokenResponse!.token,
      );
      
      _isLoading = false;
      notifyListeners();
      return result; // Appointment object or error string
    } catch (e) {
      print('Error creating appointment: $e');
      _isLoading = false;
      notifyListeners();
      return e.toString();
    }
  }
}

