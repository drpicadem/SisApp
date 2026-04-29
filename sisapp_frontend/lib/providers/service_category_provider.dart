import 'package:flutter/material.dart';
import '../models/service_category.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class ServiceCategoryProvider extends ChangeNotifier {
  List<ServiceCategory> _categories = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _page = 1;
  final int _pageSize = 20;
  String? _lastName;
  int? _lastParentCategoryId;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  ServiceCategoryProvider(this._authProvider);

  List<ServiceCategory> get categories => _categories;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;

  Future<void> loadCategories({
    bool refresh = true,
    String? name,
    int? parentCategoryId,
  }) async {
    if (_authProvider?.tokenResponse == null) return;

    final effectiveName = name ?? _lastName;
    final effectiveParentCategoryId = parentCategoryId ?? _lastParentCategoryId;
    _lastName = effectiveName;
    _lastParentCategoryId = effectiveParentCategoryId;

    if (refresh) {
      _isLoading = true;
      _page = 1;
      _hasMore = true;
      _categories = [];
      notifyListeners();
    } else {
      if (!_hasMore || _isLoadingMore) return;
      _isLoadingMore = true;
      notifyListeners();
    }

    try {
      final newCategories = await _apiService.getServiceCategories(
        _authProvider!.tokenResponse!.token,
        name: effectiveName,
        parentCategoryId: effectiveParentCategoryId,
        page: _page,
        pageSize: _pageSize,
      );
      if (newCategories.length < _pageSize) {
        _hasMore = false;
      }
      if (refresh) {
        _categories = newCategories;
      } else {
        _categories.addAll(newCategories);
      }
      _page++;
    } catch (e) {
      print('Error loading categories: $e');
    }

    _isLoading = false;
    _isLoadingMore = false;
    notifyListeners();
  }

  Future<bool> addCategory(ServiceCategory category) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    try {
      final savedCategory = await _apiService.createServiceCategory(category, _authProvider!.tokenResponse!.token);

      if (savedCategory != null) {
        _categories.add(savedCategory);
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

  Future<bool> deleteCategory(int id) async {
    if (_authProvider?.tokenResponse == null) return false;
    bool success = await _apiService.deleteServiceCategory(id, _authProvider!.tokenResponse!.token);
    if (success) {
      _categories.removeWhere((c) => c.id == id);
      notifyListeners();
    }
    return success;
  }

  Future<bool> updateCategory(ServiceCategory category) async {
    if (_authProvider?.tokenResponse == null) return false;

    _isLoading = true;
    notifyListeners();

    try {
      final updatedCategory = await _apiService.updateServiceCategory(category.id, category, _authProvider!.tokenResponse!.token);

      if (updatedCategory != null) {
        final index = _categories.indexWhere((c) => c.id == category.id);
        if (index != -1) {
          _categories[index] = updatedCategory;
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
