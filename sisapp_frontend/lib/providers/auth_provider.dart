import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/token_response.dart';
import '../models/login_request.dart';
import '../services/api_service.dart';

class AuthProvider extends ChangeNotifier {
  TokenResponse? _tokenResponse;
  bool _isLoading = false;
  final ApiService _apiService = ApiService();

  TokenResponse? get tokenResponse => _tokenResponse;
  bool get isLoading => _isLoading;
  bool get isLoggedIn => _tokenResponse != null;

  Future<bool> login(String username, String password) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiService.login(
        LoginRequest(username: username, password: password),
      );

      if (response != null) {
        _tokenResponse = response;
        await _saveToken(response);
        _isLoading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      print('Login error: $e');
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<void> logout() async {
    _tokenResponse = null;
    await _clearToken();
    notifyListeners();
  }

  Future<void> _saveToken(TokenResponse token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', token.token);
    await prefs.setString('refreshToken', token.refreshToken);
  }

  Future<void> _clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
    await prefs.remove('refreshToken');
  }
}