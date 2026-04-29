import 'package:flutter/material.dart';
import '../models/service.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class ServiceProvider extends ChangeNotifier {
  List<Service> _services = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _page = 1;
  final int _pageSize = 20;
  int? _currentSalonId;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  ServiceProvider(this._authProvider);

  List<Service> get services => _services;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;

  Future<void> loadServices(int salonId, {bool refresh = true}) async {
    if (_authProvider?.tokenResponse == null) return;

    if (refresh || _currentSalonId != salonId) {
      _currentSalonId = salonId;
      _page = 1;
      _hasMore = true;
      _services = [];
      _isLoading = true;
      notifyListeners();
    } else {
      if (!_hasMore || _isLoadingMore) return;
      _isLoadingMore = true;
      notifyListeners();
    }

    try {
      final newServices = await _apiService.getServices(
        salonId,
        _authProvider!.tokenResponse!.token,
        page: _page,
        pageSize: _pageSize,
      );
      if (newServices.length < _pageSize) {
        _hasMore = false;
      }

      if (_page == 1) {
        _services = newServices;
      } else {
        _services.addAll(newServices);
      }
      _page++;
    } catch (e) {
      print('Error loading services: $e');
    }

    _isLoading = false;
    _isLoadingMore = false;
    notifyListeners();
  }

  Future<bool> addService(Service service) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    final savedService = await _apiService.createService(service, _authProvider!.tokenResponse!.token);

    if (savedService != null) {
      _services.insert(0, savedService);
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

