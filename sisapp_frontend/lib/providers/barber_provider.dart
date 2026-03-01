import 'package:flutter/material.dart';
import '../models/barber.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class BarberProvider extends ChangeNotifier {
  List<Barber> _barbers = [];
  Barber? _myBarberProfile;
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  BarberProvider(this._authProvider);

  List<Barber> get barbers => _barbers;
  Barber? get myBarberProfile => _myBarberProfile;
  bool get isLoading => _isLoading;

  Future<void> loadBarbers(int salonId) async {
    if (_authProvider?.tokenResponse == null) return;
    
    _isLoading = true;
    notifyListeners();

    try {
      _barbers = await _apiService.getBarbers(salonId, _authProvider!.tokenResponse!.token);
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
}
