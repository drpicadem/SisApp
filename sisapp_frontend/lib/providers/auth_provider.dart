import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:jwt_decoder/jwt_decoder.dart';
import '../models/token_response.dart';
import '../models/login_request.dart';
import '../services/api_service.dart';

class AuthProvider extends ChangeNotifier {
  TokenResponse? _tokenResponse;
  bool _isLoading = false;
  String? _role;
  final ApiService _apiService = ApiService();

  int? _userId;

  TokenResponse? get tokenResponse => _tokenResponse;
  bool get isLoading => _isLoading;
  bool get isLoggedIn => _tokenResponse != null;
  String? get role => _role;
  int? get userId => _userId;

  bool get isAdmin => _role == 'Admin' || _role == 'SuperAdmin';
  bool get isBarber => _role == 'Barber';
  bool get isCustomer => _role == 'User';

  Future<bool> login(String username, String password) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiService.login(
        LoginRequest(username: username, password: password),
      );

      if (response != null) {
        _tokenResponse = response;
        _decodeToken(response.token);
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

  void _decodeToken(String token) {
    try {
      Map<String, dynamic> decodedToken = JwtDecoder.decode(token);
      // .NET often uses this long key for Role
      _role = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? decodedToken['role'];
      
      // Extract User ID (nameid)
      final userIdStr = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? decodedToken['nameid'];
      if (userIdStr != null) {
        _userId = int.tryParse(userIdStr.toString());
      }
      
      print('User Role: $_role, ID: $_userId');
    } catch (e) {
      print('Error decoding token: $e');
    }
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