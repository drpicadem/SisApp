import 'package:flutter/material.dart';
import '../models/appointment.dart';
import '../services/api_service.dart';
import 'auth_provider.dart';

class AppointmentProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  bool _isLoading = false;
  List<Appointment> _appointments = [];


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
    bool? isActive,
    bool? isPaid,
    int? prioritizeAppointmentId,
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

      if (prioritizeAppointmentId != null) {
        final index = _appointments.indexWhere((a) => a.id == prioritizeAppointmentId);
        if (index > 0) {
          final prioritized = _appointments.removeAt(index);
          _appointments.insert(0, prioritized);
        }
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

    bool success = await _apiService.cancelAppointment(id, _authProvider!.tokenResponse!.token);

    if (success) {
      final index = _appointments.indexWhere((a) => a.id == id);
      if (index != -1) {
        _appointments[index] = _appointments[index].copyWith(status: 'Cancelled');
      }
      notifyListeners();
    }

    return success;
  }
}
