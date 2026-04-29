import 'package:flutter/material.dart';
import '../models/salon.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class SalonProvider extends ChangeNotifier {
  List<Salon> _salons = [];
  Set<int> _favoriteSalonIds = {};
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _page = 1;
  final int _pageSize = 20;
  String? _lastError;
  final ApiService _apiService = ApiService();
  AuthProvider? _authProvider;

  SalonProvider(this._authProvider);

  void updateAuthProvider(AuthProvider? authProvider) {
    _authProvider = authProvider;
  }

  List<Salon> get salons => _salons;
  Set<int> get favoriteSalonIds => _favoriteSalonIds;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;
  String? get lastError => _lastError;

  Future<void> loadSalons({bool refresh = true}) async {
    if (_authProvider?.tokenResponse == null) return;

    if (refresh) {
      _isLoading = true;
      _hasMore = true;
      _page = 1;
      _salons = [];
      _lastError = null;
      notifyListeners();
    } else {
      if (!_hasMore || _isLoadingMore) return;
      _isLoadingMore = true;
      notifyListeners();
    }

    try {
      final token = _authProvider!.tokenResponse!.token;
      if (refresh) {
        final results = await Future.wait<dynamic>([
          _apiService.getSalons(token, page: _page, pageSize: _pageSize),
          _apiService.getFavoriteSalons(token),
        ]);
        final salons = results[0] as List<Salon>;
        final favList = results[1] as List<int>;
        _salons = salons;
        _favoriteSalonIds = favList.toSet();
        if (salons.length < _pageSize) {
          _hasMore = false;
        }
      } else {
        final salons = await _apiService.getSalons(token, page: _page, pageSize: _pageSize);
        _salons.addAll(salons);
        if (salons.length < _pageSize) {
          _hasMore = false;
        }
      }
      _page++;
    } catch (e) {
      _lastError = e.toString().replaceAll('Exception: ', '');
      print('Error loading salons/favorites: $e');
    }

    _isLoading = false;
    _isLoadingMore = false;
    notifyListeners();
  }

  Future<void> toggleFavorite(int salonId) async {
    if (_authProvider?.tokenResponse == null) return;

    _lastError = null;

    final wasFavorite = _favoriteSalonIds.contains(salonId);
    if (wasFavorite) {
      _favoriteSalonIds.remove(salonId);
    } else {
      _favoriteSalonIds.add(salonId);
    }
    notifyListeners();


    bool success = false;
    try {
      success = await _apiService.toggleFavoriteSalon(salonId, _authProvider!.tokenResponse!.token);
    } catch (e) {
      _lastError = e.toString().replaceAll('Exception: ', '');
    }


    if (!success) {
      if (wasFavorite) {
        _favoriteSalonIds.add(salonId);
      } else {
        _favoriteSalonIds.remove(salonId);
      }
      notifyListeners();
      _lastError ??= 'Ažuriranje omiljenih salona nije uspjelo.';
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
