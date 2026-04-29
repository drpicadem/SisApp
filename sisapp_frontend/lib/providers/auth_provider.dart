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
  String? _username;
  String? _email;
  final ApiService _apiService = ApiService();

  int? _userId;

  TokenResponse? get tokenResponse => _tokenResponse;
  bool get isLoading => _isLoading;
  bool get isLoggedIn => _tokenResponse != null;
  String? get role => _role;
  String? get username => _username;
  int? get userId => _userId;
  String? get email => _email;

  bool get isAdmin => _role == 'Admin';
  bool get isBarber => _role == 'Barber';
  bool get isCustomer => _role == 'User';

  Future<String?> login(String username, String password) async {
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
        return null;
      } else {
        _isLoading = false;
        notifyListeners();
        return 'Neispravno korisničko ime ili lozinka';
      }
    } catch (e) {
      print('Login error: $e');
      _isLoading = false;
      notifyListeners();
      return e.toString().replaceAll('Exception: ', '');
    }
  }

  Future<String?> register({
    required String username,
    required String firstName,
    required String lastName,
    required String email,
    required String phoneNumber,
    required String password,
    required String confirmPassword,
  }) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _apiService.register({
        'username': username,
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'phoneNumber': phoneNumber,
        'password': password,
        'confirmPassword': confirmPassword,
      });

      if (response != null) {
        _tokenResponse = response;
        _decodeToken(response.token);
        await _saveToken(response);
        _isLoading = false;
        notifyListeners();
        return null;
      } else {
        _isLoading = false;
        notifyListeners();
        return 'Registracija nije uspjela';
      }
    } catch (e) {
      _isLoading = false;
      notifyListeners();
      return e.toString().replaceAll('Exception: ', '');
    }
  }

  void _decodeToken(String token) {
    try {
      Map<String, dynamic> decodedToken = JwtDecoder.decode(token);


      var roleClaim = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? decodedToken['role'];
      if (roleClaim is List) {
        _role = roleClaim.isNotEmpty ? roleClaim.first.toString() : null;
      } else {
        _role = roleClaim?.toString();
      }


      final userIdStr = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? decodedToken['nameid'];
      if (userIdStr != null) {
        _userId = int.tryParse(userIdStr.toString());
      }


      _username = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
                  decodedToken['unique_name'] ??
                  decodedToken['sub'];


      _email = decodedToken['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ??
               decodedToken['email'];

      print('User Role: $_role, ID: $_userId, Username: $_username, Email: $_email');
    } catch (e) {
      print('Error decoding token: $e');
    }
  }

  Future<void> logout() async {
    final accessToken = _tokenResponse?.token;
    final refreshToken = _tokenResponse?.refreshToken;
    if (accessToken != null && accessToken.isNotEmpty) {
      await _apiService.revokeToken(
        accessToken: accessToken,
        refreshToken: (refreshToken != null && refreshToken.isNotEmpty) ? refreshToken : null,
      );
    }

    _tokenResponse = null;
    _role = null;
    _userId = null;
    _username = null;
    _email = null;
    await _clearToken();
    notifyListeners();
  }

  void updateProfileClaims({required String username, required String email}) {
    _username = username;
    _email = email;
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
