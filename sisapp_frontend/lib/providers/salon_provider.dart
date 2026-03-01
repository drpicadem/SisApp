import 'package:flutter/material.dart';
import '../models/salon.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class SalonProvider extends ChangeNotifier {
  List<Salon> _salons = [];
  Set<int> _favoriteSalonIds = {};
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  SalonProvider(this._authProvider);

  List<Salon> get salons => _salons;
  Set<int> get favoriteSalonIds => _favoriteSalonIds;
  bool get isLoading => _isLoading;

  Future<void> loadSalons() async {
    if (_authProvider?.tokenResponse == null) return;
    
    _isLoading = true;
    notifyListeners();

    try {
      _salons = await _apiService.getSalons(_authProvider!.tokenResponse!.token);
      final favList = await _apiService.getFavoriteSalons(_authProvider!.tokenResponse!.token);
      _favoriteSalonIds = favList.toSet();
    } catch (e) {
      print('Error loading salons/favorites: $e');
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> toggleFavorite(int salonId) async {
    if (_authProvider?.tokenResponse == null) return;
    
    // Optimistic UI update
    final wasFavorite = _favoriteSalonIds.contains(salonId);
    if (wasFavorite) {
      _favoriteSalonIds.remove(salonId);
    } else {
      _favoriteSalonIds.add(salonId);
    }
    notifyListeners();

    // API call
    bool success = await _apiService.toggleFavoriteSalon(salonId, _authProvider!.tokenResponse!.token);
    
    // Rollback if failed
    if (!success) {
      if (wasFavorite) {
        _favoriteSalonIds.add(salonId);
      } else {
        _favoriteSalonIds.remove(salonId);
      }
      notifyListeners();
    }
  }

  Future<int?> addSalon(Salon salon) async {
    if (_authProvider?.tokenResponse == null) return null;

    _isLoading = true;
    notifyListeners();

    int? createdId = await _apiService.createSalon(salon, _authProvider!.tokenResponse!.token);
    
    if (createdId != null) {
      await loadSalons();
    }

    _isLoading = false;
    notifyListeners();
    return createdId;
  }

  Future<bool> updateSalon(Salon salon) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    bool success = await _apiService.updateSalon(salon, _authProvider!.tokenResponse!.token);
    
    if (success) {
      // Update local state
      final index = _salons.indexWhere((s) => s.id == salon.id);
      if (index != -1) {
        _salons[index] = salon;
      }
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
