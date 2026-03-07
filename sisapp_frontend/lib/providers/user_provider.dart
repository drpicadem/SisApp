import 'package:flutter/material.dart';
import '../models/user.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class UserProvider extends ChangeNotifier {
  List<User> _users = [];
  bool _isLoading = false;
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  UserProvider(this._authProvider);

  List<User> get users => _users;
  bool get isLoading => _isLoading;

  Future<void> loadUsers({String? role}) async {
    if (_authProvider?.tokenResponse == null) return;
    
    _isLoading = true;
    notifyListeners();

    try {
      _users = await _apiService.getUsers(_authProvider!.tokenResponse!.token, role: role);
    } catch (e) {
      print('Error loading users: $e');
    }

    _isLoading = false;
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
      await loadUsers();
    }
    return success;
  }
}

