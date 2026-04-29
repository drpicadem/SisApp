import 'package:flutter/material.dart';
import '../models/user.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class UserProvider extends ChangeNotifier {
  List<User> _users = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _page = 1;
  final int _pageSize = 20;
  String? _lastRole;
  bool? _lastIsDeleted;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  UserProvider(this._authProvider);

  List<User> get users => _users;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _hasMore;

  Future<void> loadUsers({
    bool refresh = true,
    String? role,
    bool? isDeleted,
    bool useStoredFilters = false,
  }) async {
    if (_authProvider?.tokenResponse == null) return;

    final effectiveRole = useStoredFilters ? _lastRole : role;
    final effectiveIsDeleted = useStoredFilters ? _lastIsDeleted : isDeleted;
    _lastRole = effectiveRole;
    _lastIsDeleted = effectiveIsDeleted;

    if (refresh) {
      _page = 1;
      _hasMore = true;
      _users = [];
      _isLoading = true;
      notifyListeners();
    } else {
      if (!_hasMore || _isLoadingMore) return;
      _isLoadingMore = true;
      notifyListeners();
    }

    try {
      final newUsers = await _apiService.getUsers(
        _authProvider!.tokenResponse!.token,
        role: effectiveRole,
        isDeleted: effectiveIsDeleted,
        page: _page,
        pageSize: _pageSize,
      );

      if (newUsers.length < _pageSize) {
        _hasMore = false;
      }

      if (refresh) {
        _users = newUsers;
      } else {
        _users.addAll(newUsers);
      }

      _page++;
    } catch (e) {
      print('Error loading users: $e');
      if (refresh) _users = [];
    }

    _isLoading = false;
    _isLoadingMore = false;
    notifyListeners();
  }

  Future<bool> deleteUser(int userId) async {
    if (_authProvider?.tokenResponse == null) return false;

    bool success = await _apiService.deleteUser(userId, _authProvider!.tokenResponse!.token);
    if (success) {
      _users.removeWhere((u) => u.id == userId);
      notifyListeners();
    }
    return success;
  }

  Future<bool> restoreUser(int userId) async {
    if (_authProvider?.tokenResponse == null) return false;

    bool success = await _apiService.restoreUser(userId, _authProvider!.tokenResponse!.token);
    if (success) {
      await loadUsers(refresh: true, useStoredFilters: true);
    }
    return success;
  }

  Future<User?> updateUser(int userId, Map<String, dynamic> request) async {
    if (_authProvider?.tokenResponse == null) return null;

    final updated = await _apiService.updateUser(
      userId,
      request,
      _authProvider!.tokenResponse!.token,
    );

    final index = _users.indexWhere((u) => u.id == userId);
    if (index >= 0) {
      _users[index] = updated;
      notifyListeners();
    }

    return updated;
  }

  Future<User?> createUser(Map<String, dynamic> request) async {
    if (_authProvider?.tokenResponse == null) return null;

    final created = await _apiService.createUser(
      request,
      _authProvider!.tokenResponse!.token,
    );

    await loadUsers(refresh: true, useStoredFilters: true);
    return created;
  }

  Future<void> adminSetUserPassword({
    required int userId,
    required String newPassword,
    required String confirmPassword,
  }) async {
    if (_authProvider?.tokenResponse == null) return;
    await _apiService.adminSetUserPassword(
      userId: userId,
      newPassword: newPassword,
      confirmPassword: confirmPassword,
      token: _authProvider!.tokenResponse!.token,
    );
  }
}

