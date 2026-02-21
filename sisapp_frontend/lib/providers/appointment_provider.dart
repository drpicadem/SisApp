import 'package:flutter/material.dart';
import '../models/appointment.dart';
import '../services/api_service.dart';
import 'auth_provider.dart';

class AppointmentProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  bool _isLoading = false;
  List<Appointment> _appointments = [];
  
  // Pagination
  int _page = 1;
  final int _pageSize = 10;
  bool _hasMore = true;
  bool _isLoadingMore = false;

  AppointmentProvider(this._authProvider);

  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;
  List<Appointment> get appointments => _appointments;

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

  Future<bool> cancelAppointment(int id) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners(); 

    try {
      bool success = await _apiService.cancelAppointment(id, _authProvider!.tokenResponse!.token);
      
      if (success) {
        // Remove from list locally if successful to avoid full refresh, 
        // OR refresh. 
        // If we remove locally, we might mess up pagination indices, but for one item it's fine.
        // Actually, if we cancel, it moves from "Active" to "History".
        // If we are in "Active" tab, we should remove it.
        // If we are in "History" tab, we should add it (or refresh).
        
        // Simplest strategy: Remove from current list.
        _appointments.removeWhere((a) => a.id == id);
      }
      
      _isLoading = false;
      notifyListeners();
      return success;
    } catch (e) {
      print('Error cancelling appointment: $e');
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }
}
