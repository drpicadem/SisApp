import 'package:flutter/material.dart';
import '../models/salon_amenity.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class SalonAmenityProvider extends ChangeNotifier {
  List<SalonAmenity> _amenities = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _page = 1;
  final int _pageSize = 20;
  int? _lastSalonId;
  String? _lastName;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  SalonAmenityProvider(this._authProvider);

  List<SalonAmenity> get amenities => _amenities;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;

  Future<void> loadAmenities({
    bool refresh = true,
    int? salonId,
    String? name,
  }) async {
    if (_authProvider?.tokenResponse == null) return;

    final effectiveSalonId = salonId ?? _lastSalonId;
    final effectiveName = name ?? _lastName;
    final filterChanged = effectiveSalonId != _lastSalonId || effectiveName != _lastName;
    _lastSalonId = effectiveSalonId;
    _lastName = effectiveName;

    if (refresh || filterChanged) {
      _isLoading = true;
      _page = 1;
      _hasMore = true;
      _amenities = [];
      notifyListeners();
    } else {
      if (!_hasMore || _isLoadingMore) return;
      _isLoadingMore = true;
      notifyListeners();
    }

    try {
      final newAmenities = await _apiService.getSalonAmenities(
        _authProvider!.tokenResponse!.token,
        salonId: effectiveSalonId,
        name: effectiveName,
        page: _page,
        pageSize: _pageSize,
      );
      if (newAmenities.length < _pageSize) {
        _hasMore = false;
      }
      if (_page == 1) {
        _amenities = newAmenities;
      } else {
        _amenities.addAll(newAmenities);
      }
      _page++;
    } catch (e) {
      print('Error loading amenities: $e');
    }

    _isLoading = false;
    _isLoadingMore = false;
    notifyListeners();
  }

  Future<bool> addAmenity(SalonAmenity amenity) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    try {
      final savedAmenity = await _apiService.createSalonAmenity(amenity, _authProvider!.tokenResponse!.token);

      if (savedAmenity != null) {
        _amenities.add(savedAmenity);
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      _isLoading = false;
      notifyListeners();
      throw e;
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<bool> deleteAmenity(int id) async {
    if (_authProvider?.tokenResponse == null) return false;
    bool success = await _apiService.deleteSalonAmenity(id, _authProvider!.tokenResponse!.token);
    if (success) {
      _amenities.removeWhere((a) => a.id == id);
      notifyListeners();
    }
    return success;
  }

  Future<bool> updateAmenity(SalonAmenity amenity) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    try {
      final updatedAmenity = await _apiService.updateSalonAmenity(amenity.id, amenity, _authProvider!.tokenResponse!.token);

      if (updatedAmenity != null) {
        final index = _amenities.indexWhere((a) => a.id == amenity.id);
        if (index != -1) {
          _amenities[index] = updatedAmenity;
        }
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      _isLoading = false;
      notifyListeners();
      throw e;
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }
}
