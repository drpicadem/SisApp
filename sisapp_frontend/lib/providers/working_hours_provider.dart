import 'package:flutter/material.dart';
import '../models/working_hours.dart';
import '../services/api_service.dart';
import 'auth_provider.dart';

class WorkingHoursProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  List<WorkingHours> _schedule = [];
  bool _isLoading = false;

  WorkingHoursProvider(this._authProvider);

  List<WorkingHours> get schedule => _schedule;
  bool get isLoading => _isLoading;

  Future<void> fetchMySchedule() async {
    if (_authProvider?.tokenResponse == null) return;
    _isLoading = true;
    notifyListeners();

    try {
      _schedule = await _apiService.getMySchedule(_authProvider!.tokenResponse!.token);
    } catch (e) {
      print('Error fetching schedule: $e');
      _schedule = [];
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<bool> createWorkingHours(WorkingHours wh) async {
    if (_authProvider?.tokenResponse == null) return false;

    try {
      final result = await _apiService.createWorkingHours(
        _authProvider!.tokenResponse!.token,
        wh,
      );
      if (result != null) {
        _schedule.add(result);
        _schedule.sort((a, b) => a.dayOfWeek.compareTo(b.dayOfWeek));
        notifyListeners();
        return true;
      }
      return false;
    } catch (e) {
      print('Error creating working hours: $e');
      rethrow;
    }
  }

  Future<bool> updateWorkingHours(int id, WorkingHours wh) async {
    if (_authProvider?.tokenResponse == null) return false;

    try {
      final result = await _apiService.updateWorkingHours(
        _authProvider!.tokenResponse!.token,
        id,
        wh,
      );
      if (result != null) {
        final index = _schedule.indexWhere((s) => s.id == id);
        if (index != -1) _schedule[index] = result;
        notifyListeners();
        return true;
      }
      return false;
    } catch (e) {
      print('Error updating working hours: $e');
      rethrow;
    }
  }

  Future<bool> deleteWorkingHours(int id) async {
    if (_authProvider?.tokenResponse == null) return false;

    try {
      final success = await _apiService.deleteWorkingHours(
        _authProvider!.tokenResponse!.token,
        id,
      );
      if (success) {
        _schedule.removeWhere((s) => s.id == id);
        notifyListeners();
      }
      return success;
    } catch (e) {
      print('Error deleting working hours: $e');
      return false;
    }
  }
}
