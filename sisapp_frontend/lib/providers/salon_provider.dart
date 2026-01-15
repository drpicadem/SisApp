import 'package:flutter/material.dart';
import '../models/salon.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class SalonProvider extends ChangeNotifier {
  List<Salon> _salons = [];
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  SalonProvider(this._authProvider);

  List<Salon> get salons => _salons;
  bool get isLoading => _isLoading;

  Future<void> loadSalons() async {
    if (_authProvider?.tokenResponse == null) return;
    
    _isLoading = true;
    notifyListeners();

    try {
      _salons = await _apiService.getSalons(_authProvider!.tokenResponse!.token);
    } catch (e) {
      print('Error loading salons: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<bool> addSalon(Salon salon) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    bool success = await _apiService.createSalon(salon, _authProvider!.tokenResponse!.token);
    
    if (success) {
      await loadSalons();
    }

    _isLoading = false;
    notifyListeners();
    return success;
  }

  Future<bool> toggleStatus(Salon salon) async {
    if (_authProvider?.tokenResponse == null) return false;

    bool success = await _apiService.toggleSalonStatus(salon.id, _authProvider!.tokenResponse!.token);
    
    if (success) {
      salon.isActive = !salon.isActive;
      notifyListeners();
    }
    
    return success;
  }
}
