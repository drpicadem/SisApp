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

    final savedService = await _apiService.createService(service, _authProvider!.tokenResponse!.token);

    if (savedService != null) {
      _services.add(savedService);
    }

    _isLoading = false;
    notifyListeners();
    return savedService != null;
  }

  Future<bool> deleteService(int serviceId, int salonId) async {
    if (_authProvider?.tokenResponse == null) return false;
    bool success = await _apiService.deleteService(serviceId, _authProvider!.tokenResponse!.token);
    if (success) {
      _services.removeWhere((s) => s.id == serviceId);
      notifyListeners();
    }
    return success;
  }

  Future<bool> updateService(Service service) async {
    if (_authProvider?.tokenResponse == null) return false;

    final updatedService = await _apiService.updateService(service, _authProvider!.tokenResponse!.token);

    if (updatedService != null) {
      final index = _services.indexWhere((s) => s.id == service.id);
      if (index != -1) {
        _services[index] = updatedService;
      }
      notifyListeners();
    }

    return updatedService != null;
  }
}

