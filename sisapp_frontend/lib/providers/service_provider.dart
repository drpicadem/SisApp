import 'package:flutter/material.dart';
import '../models/service.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class ServiceProvider extends ChangeNotifier {
  List<Service> _services = [];
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  ServiceProvider(this._authProvider);

  List<Service> get services => _services;
  bool get isLoading => _isLoading;

  Future<void> loadServices(int salonId) async {
    if (_authProvider?.tokenResponse == null) return;
    
    _isLoading = true;
    notifyListeners();

    try {
      _services = await _apiService.getServices(salonId, _authProvider!.tokenResponse!.token);
    } catch (e) {
      print('Error loading services: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<bool> addService(Service service) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    bool success = await _apiService.createService(service, _authProvider!.tokenResponse!.token);
    
    if (success) {
      // Reload services to get the new list (or add locally)
      // For simplicity, we reload if we know the salonId, but service object might not have ID yet.
      // Better to just reload.
      await loadServices(service.salonId); 
    }

    _isLoading = false;
    notifyListeners();
    return success;
  }
}
