import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/login_request.dart';
import '../models/token_response.dart';
import '../models/service.dart';
import '../models/barber.dart';
import '../models/salon.dart';
import '../models/user.dart';

class ApiService {
  static const String baseUrl = 'http://localhost:7100/api';
  
  // Auth methods...
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

  // Service methods
  Future<List<Service>> getServices(int salonId, String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Services/salon/$salonId'), // Assuming this endpoint exists, or we filter normal get
        // Actually, backend SalonsController usually has GetSalonServices or similar. 
        // Let's assume standard REST: GET /api/Services?salonId=$salonId or similar.
        // Waiting, I need to check if there is a ServicesController on backend? I haven't seen it yet.
        // I will assume standard GET /api/Services first.
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => Service.fromJson(item)).toList();
      }
      return [];
    } catch (e) {
      print('Get Services error: $e');
      return [];
    }
  }

  Future<bool> createService(Service service, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Services'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(service.toJson()),
      );
      return response.statusCode == 200 || response.statusCode == 201;
    } catch (e) {
      print('Create Service error: $e');
      return false;
    }
  }

  // Barber methods
  Future<List<Barber>> getBarbers(int salonId, String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Barbers/salon/$salonId'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => Barber.fromJson(item)).toList();
      }
      return [];
    } catch (e) {
      print('Get Barbers error: $e');
      return [];
    }
  }

  Future<bool> createBarber(CreateBarberDto dto, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Barbers'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(dto.toJson()),
      );
      if (response.statusCode == 200 || response.statusCode == 201) {
        return true;
      } else {
        print('Create Barber failed: ${response.statusCode} ${response.body}');
        return false;
      }
    } catch (e) {
      print('Create Barber error: $e');
      return false;
    }
  }
  // Salon methods
  Future<List<Salon>> getSalons(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Salons'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => Salon.fromJson(item)).toList();
      }
      return [];
    } catch (e) {
      print('Get Salons error: $e');
      return [];
    }
  }

  Future<bool> createSalon(Salon salon, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Salons'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(salon.toJson()),
      );
      return response.statusCode == 200 || response.statusCode == 201;
    } catch (e) {
      print('Create Salon error: $e');
      return false;
    }
  }

  Future<bool> toggleSalonStatus(int salonId, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Salons/$salonId/status'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Toggle Salon Status error: $e');
      return false;
    }
  }

  // User methods
  Future<List<User>> getUsers(String token, {String? role}) async {
    try {
      String url = '$baseUrl/Users';
      if (role != null) {
        url += '?role=$role';
      }

      final response = await http.get(
        Uri.parse(url),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => User.fromJson(item)).toList();
      }
      return [];
    } catch (e) {
      print('Get Users error: $e');
      return [];
    }
  }

  Future<bool> deleteUser(int userId, String token) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/Users/$userId'),
        headers: {
           'Authorization': 'Bearer $token',
        },
      );
      return response.statusCode == 204 || response.statusCode == 200;
    } catch (e) {
      print('Delete User error: $e');
      return false;
    }
  }

  // Report methods
  Future<Map<String, dynamic>?> getReports(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reports/stats'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        return jsonDecode(response.body);
      }
      return null;
    } catch (e) {
      print('Get Reports error: $e');
      return null;
    }
  }
}