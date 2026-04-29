import 'package:flutter/material.dart';
import '../models/barber.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class BarberProvider extends ChangeNotifier {
  List<Barber> _barbers = [];
  Barber? _myBarberProfile;
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  AuthProvider? _authProvider;

  BarberProvider(this._authProvider);

  void updateAuthProvider(AuthProvider? authProvider) {
    _authProvider = authProvider;
  }

  List<Barber> get barbers => _barbers;
  Barber? get myBarberProfile => _myBarberProfile;
  bool get isLoading => _isLoading;

  Future<void> loadBarbers(int salonId, {int? serviceId}) async {
    if (_authProvider?.tokenResponse == null) return;

    _isLoading = true;
    notifyListeners();

    try {
      _barbers = await _apiService.getBarbers(salonId, _authProvider!.tokenResponse!.token, serviceId: serviceId);
    } catch (e) {
      print('Error loading barbers: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> loadMyBarberProfile() async {
    if (_authProvider?.tokenResponse == null) return;

    _isLoading = true;
    notifyListeners();

    try {
      _myBarberProfile = await _apiService.getMyBarberProfile(_authProvider!.tokenResponse!.token);
    } catch (e) {
      print('Error loading my barber profile: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<int?> addBarber(CreateBarberDto dto) async {
    if (_authProvider?.tokenResponse == null) return null;

    _isLoading = true;
    notifyListeners();

    int? createdId = await _apiService.createBarber(dto, _authProvider!.tokenResponse!.token);

    if (createdId != null) {
      await loadBarbers(dto.salonId);
    }

    _isLoading = false;
    notifyListeners();
    return createdId;
  }

  Future<bool> updateBarber(int id, UpdateBarberDto dto) async {
    if (_authProvider?.tokenResponse == null) return false;

    try {
      final updatedBarber = await _apiService.updateBarber(id, dto, _authProvider!.tokenResponse!.token);
      if (updatedBarber != null) {
        int index = _barbers.indexWhere((b) => b.id == id);
        if (index != -1) {
          _barbers[index] = updatedBarber;
          notifyListeners();
        }
        return true;
      }
    } catch (e) {
      print('Error updating barber: $e');
      rethrow;
    }
    return false;
  }
}
