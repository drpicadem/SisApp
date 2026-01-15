import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/login_request.dart';
import '../models/token_response.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:7100/api';
  
  Future<TokenResponse?> login(LoginRequest request) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Authentication/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(request.toJson()),
      );

      if (response.statusCode == 200) {
        return TokenResponse.fromJson(jsonDecode(response.body));
      }
      return null;
    } catch (e) {
      print('Login error: $e');
      return null;
    }
  }
}