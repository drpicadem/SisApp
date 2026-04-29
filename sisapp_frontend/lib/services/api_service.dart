import 'dart:convert';
import 'dart:typed_data';
import 'package:http/http.dart' as http;
import '../models/login_request.dart';
import '../models/token_response.dart';
import '../models/service.dart';
import '../models/barber.dart';
import '../models/salon.dart';
import '../models/user.dart';
import '../models/appointment.dart';
import '../models/notification.dart';
import '../models/review.dart';
import '../models/working_hours.dart';
import '../models/service_category.dart';
import '../models/salon_amenity.dart';
import '../models/city.dart';
import '../models/admin_log.dart';
import 'package:intl/intl.dart';
import 'dart:io' show Platform;

import 'package:flutter/foundation.dart' show kIsWeb, debugPrint;

class UnauthorizedException implements Exception {
  final String message;
  UnauthorizedException([this.message = 'Sesija je istekla. Prijavite se ponovo.']);

  @override
  String toString() => message;
}

class ApiService {
  static Future<void> Function()? onUnauthorized;
  static bool _isHandlingUnauthorized = false;

  static const String _envApiUrl = String.fromEnvironment('API_BASE_URL');
  static final String baseUrl = _envApiUrl.isNotEmpty ? _envApiUrl : _defaultBaseUrl;
  static String get _defaultBaseUrl {
    if (kIsWeb || Platform.isWindows || Platform.isLinux || Platform.isMacOS) {
      return 'http://localhost:7100/api';
    }

    if (Platform.isAndroid) {
      return 'http://10.0.2.2:7100/api';
    }

    if (Platform.isIOS) {
      return 'http://localhost:7100/api';
    }

    return 'http://localhost:7100/api';
  }
  static final String baseServerUrl = baseUrl.replaceAll(RegExp(r'/api$'), '');

  String _extractApiError(String responseBody, String fallback) {
    try {
      final decoded = jsonDecode(responseBody);
      if (decoded is Map<String, dynamic>) {
        final userError = decoded['userError']?.toString();
        if (userError != null && userError.isNotEmpty) return userError;

        final message = decoded['message']?.toString();
        if (message != null && message.isNotEmpty) return message;

        final title = decoded['title']?.toString();
        if (title != null && title.isNotEmpty) return title;

        final errors = decoded['errors'];
        if (errors is Map<String, dynamic>) {
          final values = <String>[];
          for (final fieldErrors in errors.values) {
            if (fieldErrors is List) {
              values.addAll(fieldErrors.map((e) => e.toString()));
            } else if (fieldErrors != null) {
              values.add(fieldErrors.toString());
            }
          }
          if (values.isNotEmpty) return values.join('\n');
        }
      }
    } catch (_) {}
    return fallback;
  }

  Future<void> _handleUnauthorizedResponse(http.Response response) async {
    if (response.statusCode != 401) return;

    if (!_isHandlingUnauthorized && onUnauthorized != null) {
      _isHandlingUnauthorized = true;
      try {
        await onUnauthorized!.call();
      } finally {
        _isHandlingUnauthorized = false;
      }
    }

    throw UnauthorizedException();
  }


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
      if (response.body.isNotEmpty) {
        final message = _extractApiError(response.body, 'Neispravno korisničko ime ili lozinka');
        if (message.trim().toLowerCase() == 'unauthorized') {
          throw Exception('Neispravno korisničko ime ili lozinka');
        }
        throw Exception(message);
      }
      return null;
    } catch (e) {
      print('Login error: $e');
      rethrow;
    }
  }

  Future<void> revokeToken({
    required String accessToken,
    String? refreshToken,
  }) async {
    try {
      await http.post(
        Uri.parse('$baseUrl/Authentication/revoke-token'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $accessToken',
        },
        body: jsonEncode(refreshToken),
      );
    } catch (_) {}
  }

  Future<void> requestPasswordReset(String email) async {
    final response = await http.post(
      Uri.parse('$baseUrl/Authentication/password-reset/request'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email}),
    );

    if (response.statusCode == 200) {
      return;
    }
    throw Exception(_extractApiError(
      response.body,
      'Slanje reset tokena nije uspjelo. Provjerite email format (naziv@domena.com).',
    ));
  }

  Future<void> confirmPasswordReset({
    required String email,
    required String token,
    required String newPassword,
    required String confirmPassword,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/Authentication/password-reset/confirm'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'email': email,
        'token': token,
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      }),
    );

    if (response.statusCode == 200) {
      return;
    }
    throw Exception(_extractApiError(
      response.body,
      'Reset lozinke nije uspio. Provjerite email, reset token i da lozinka ima 4-100 znakova.',
    ));
  }

  Future<TokenResponse?> register(Map<String, dynamic> data) async {
    try {

      print('=== REGISTER REQUEST ===');
      data.forEach((key, value) {
        if (key.toLowerCase().contains('password')) {
          print('  $key: ****** (length: ${value.toString().length})');
        } else {
          print('  $key: $value');
        }
      });

      final response = await http.post(
        Uri.parse('$baseUrl/Authentication/register'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode(data),
      );

      print('=== REGISTER RESPONSE ===');
      print('  Status: ${response.statusCode}');
      print('  Body: ${response.body}');

      if (response.statusCode == 200) {
        return TokenResponse.fromJson(jsonDecode(response.body));
      }


      final errorBody = jsonDecode(response.body);
      if (errorBody['errors'] != null) {
        final errors = errorBody['errors'] as Map<String, dynamic>;
        final messages = <String>[];
        errors.forEach((field, fieldErrors) {
          print('  Validation error for $field: $fieldErrors');
          if (fieldErrors is List) {
            messages.addAll(fieldErrors.map((e) => e.toString()));
          }
        });
        throw Exception(messages.join('\n'));
      }

      throw Exception(_extractApiError(response.body, 'Registracija nije uspjela. Provjerite: korisničko ime (3-30), email format, telefon (6-15 cifara), lozinka (4-100).'));
    } catch (e) {
      print('Register error: $e');
      rethrow;
    }
  }


  Future<List<Service>> getServices(
    int salonId,
    String token, {
    int? page,
    int? pageSize,
  }) async {
    try {
      final queryParams = <String, String>{
        'salonId': salonId.toString(),
      };
      if (page != null) {
        queryParams['page'] = page.toString();
      }
      if (pageSize != null) {
        queryParams['pageSize'] = pageSize.toString();
      }

      final response = await http.get(
        Uri.parse('$baseUrl/Services').replace(queryParameters: queryParams),




        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      await _handleUnauthorizedResponse(response);

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => Service.fromJson(item)).toList();
      }
      return [];
    } on UnauthorizedException {
      rethrow;
    } catch (e) {
      print('Get Services error: $e');
      return [];
    }
  }

  Future<Service?> createService(Service service, String token) async {
    final response = await http.post(
      Uri.parse('$baseUrl/Services'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(service.toJson()),
    );
    if (response.statusCode == 200 || response.statusCode == 201) {
      return Service.fromJson(jsonDecode(response.body));
    }
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Kreiranje usluge nije uspjelo. Provjerite: naziv (2-80), cijena (0-1000 KM), trajanje (1-600 min).'));
    }
    throw Exception(_extractApiError(response.body, 'Kreiranje usluge nije uspjelo. Provjerite obavezna polja i format brojeva (npr. 10 ili 10.50).'));
  }


  Future<List<Barber>> getBarbers(int salonId, String token, {int? serviceId}) async {
    try {
      final uri = Uri.parse('$baseUrl/Barbers/salon/$salonId').replace(
        queryParameters: serviceId != null ? {'serviceId': serviceId.toString()} : null,
      );
      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      await _handleUnauthorizedResponse(response);

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => Barber.fromJson(item)).toList();
      }
      return [];
    } on UnauthorizedException {
      rethrow;
    } catch (e) {
      print('Get Barbers error: $e');
      return [];
    }
  }

  Future<Barber?> getMyBarberProfile(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Barbers/my-profile'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        return Barber.fromJson(jsonDecode(response.body));
      }
      return null;
    } catch (e) {
      print('Get My Barber Profile error: $e');
      return null;
    }
  }

  Future<int?> createBarber(CreateBarberDto dto, String token) async {
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
        final data = jsonDecode(response.body);
        return data['id'] as int?;
      } else {
        print('Create Barber failed: ${response.statusCode} ${response.body}');
        return null;
      }
    } catch (e) {
      print('Create Barber error: $e');
      return null;
    }
  }

  Future<Barber?> updateBarber(int id, UpdateBarberDto dto, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Barbers/$id'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(dto.toJson()),
      );

      if (response.statusCode == 200) {
        return Barber.fromJson(jsonDecode(response.body));
      } else if (response.statusCode == 400) {
        throw Exception(_extractApiError(response.body, 'Ažuriranje uposlenika nije uspjelo. Provjerite: ime/prezime, korisničko ime, email format i lozinku (ako je unesena).'));
      } else {
        throw Exception(_extractApiError(response.body, 'Server greška (${response.statusCode}). Podaci nisu sačuvani; provjerite unos i pokušajte ponovo.'));
      }
    } catch (e) {
      print('Update Barber error: $e');
      rethrow;
    }
  }

  Future<List<Salon>> getSalons(
    String token, {
    int? page,
    int? pageSize,
  }) async {
    try {
      final queryParams = <String, String>{};
      if (page != null) {
        queryParams['page'] = page.toString();
      }
      if (pageSize != null) {
        queryParams['pageSize'] = pageSize.toString();
      }

      final response = await http.get(
        Uri.parse('$baseUrl/Salons').replace(queryParameters: queryParams.isEmpty ? null : queryParams),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      await _handleUnauthorizedResponse(response);

      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);
        final List<dynamic> body = decoded is List
            ? decoded
            : (decoded is Map<String, dynamic> && decoded['items'] is List
                ? decoded['items'] as List<dynamic>
                : <dynamic>[]);
        return body.map((dynamic item) => Salon.fromJson(item)).toList();
      }
      print('Get Salons failed: ${response.statusCode} ${response.body}');
      return [];
    } on UnauthorizedException {
      rethrow;
    } catch (e) {
      print('Get Salons error: $e');
      return [];
    }
  }

  Future<Salon?> getSalonById(int salonId, String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Salons/$salonId'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        return Salon.fromJson(jsonDecode(response.body));
      }
      return null;
    } catch (e) {
      print('Get Salon By Id error: $e');
      return null;
    }
  }

  Future<List<City>> getCities(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Cities'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);
        final List<dynamic> body = decoded is List
            ? decoded
            : (decoded is Map<String, dynamic> && decoded['items'] is List
                ? decoded['items'] as List<dynamic>
                : <dynamic>[]);
        return body.map((item) => City.fromJson(item as Map<String, dynamic>)).toList();
      }
      return [];
    } catch (e) {
      print('Get Cities error: $e');
      return [];
    }
  }

  Future<int?> createSalon(Salon salon, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Salons'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(salon.toJson()),
      );
      if (response.statusCode == 200 || response.statusCode == 201) {
        final data = jsonDecode(response.body);
        return data['id'] as int?;
      }
      return null;
    } catch (e) {
      print('Create Salon error: $e');
      return null;
    }
  }

  Future<bool> updateSalon(Salon salon, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Salons/${salon.id}'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(salon.toJson()),
      );
      print('Update Salon Status: ${response.statusCode}');
      print('Update Salon Body: ${response.body}');

      return response.statusCode == 200 || response.statusCode == 204;
    } catch (e) {
      print('Update Salon error: $e');
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


  Future<List<Notification>> getNotifications(int userId, String token, {bool unreadOnly = false}) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Notifications/$userId?unreadOnly=$unreadOnly'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => Notification.fromJson(item)).toList();
      }
      return [];
    } catch (e) {
      print('Get Notifications error: $e');
      return [];
    }
  }

  Future<bool> markNotificationAsRead(int id, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Notifications/$id/read'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );
      return response.statusCode == 204;
    } catch (e) {
      print('Mark Notification Read error: $e');
      return false;
    }
  }

  Future<bool> markAllNotificationsAsRead(int userId, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Notifications/user/$userId/read-all'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );
      return response.statusCode == 204;
    } catch (e) {
      print('Mark All Notifications Read error: $e');
      return false;
    }
  }


  Future<List<User>> getUsers(
    String token, {
    String? role,
    bool? isDeleted,
    int? page,
    int? pageSize,
  }) async {
    try {
      final queryParams = <String, String>{};
      if (role != null) {
        queryParams['role'] = role;
      }
      if (isDeleted != null) {
        queryParams['isDeleted'] = isDeleted.toString();
      }
      if (page != null) {
        queryParams['page'] = page.toString();
      }
      if (pageSize != null) {
        queryParams['pageSize'] = pageSize.toString();
      }

      final uri = Uri.parse('$baseUrl/Users').replace(queryParameters: queryParams.isEmpty ? null : queryParams);

      final response = await http.get(
        uri,
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

  Future<User?> getMyProfile(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Profile/me'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        return User.fromJson(jsonDecode(response.body));
      }
      return null;
    } catch (e) {
      print('Get My Profile error: $e');
      return null;
    }
  }

  Future<User> updateMyProfile(Map<String, dynamic> request, String token) async {
    final response = await http.put(
      Uri.parse('$baseUrl/Profile/me'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(request),
    );

    if (response.statusCode == 200) {
      return User.fromJson(jsonDecode(response.body));
    }
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Ažuriranje profila nije uspjelo. Provjerite: korisničko ime (3-30), email format, telefon (6-15 cifara).'));
    }
    throw Exception(_extractApiError(response.body, 'Ažuriranje profila nije uspjelo. Podaci nisu sačuvani; provjerite format polja i pokušajte ponovo.'));
  }

  Future<void> changeMyPassword({
    required String currentPassword,
    required String newPassword,
    required String confirmPassword,
    required String token,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/Profile/me/change-password'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'currentPassword': currentPassword,
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      }),
    );

    if (response.statusCode == 200) {
      return;
    }
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Promjena lozinke nije uspjela. Provjerite trenutnu lozinku i da nova lozinka ima 4-100 znakova.'));
    }
    throw Exception(_extractApiError(response.body, 'Promjena lozinke nije uspjela. Potvrda mora biti ista kao nova lozinka (4-100 znakova).'));
  }

  Future<bool> deleteUser(int userId, String token) async {
    final response = await http.delete(
      Uri.parse('$baseUrl/Users/$userId'),
      headers: {'Authorization': 'Bearer $token'},
    );
    if (response.statusCode == 200 || response.statusCode == 204) return true;
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Brisanje korisnika nije uspjelo. Provjerite da li pokušavate obrisati vlastiti račun.'));
    }
    throw Exception(_extractApiError(response.body, 'Brisanje korisnika nije uspjelo. Korisnik nije obrisan; provjerite ograničenja i pokušajte ponovo.'));
  }

  Future<bool> restoreUser(int userId, String token) async {
    final response = await http.put(
      Uri.parse('$baseUrl/Users/$userId/restore'),
      headers: {'Authorization': 'Bearer $token'},
    );
    if (response.statusCode == 200) return true;
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Vraćanje korisnika nije uspjelo. Provjerite da li je korisnik prethodno obrisan.'));
    }
    throw Exception(_extractApiError(response.body, 'Vraćanje korisnika nije uspjelo. Korisnik nije vraćen; provjerite status računa.'));
  }

  Future<User> updateUser(int userId, Map<String, dynamic> request, String token) async {
    final response = await http.put(
      Uri.parse('$baseUrl/Users/$userId'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(request),
    );
    if (response.statusCode == 200) {
      return User.fromJson(jsonDecode(response.body));
    }
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Ažuriranje korisnika nije uspjelo. Provjerite: ime/prezime, korisničko ime (3-30), email format, telefon.'));
    }
    throw Exception(_extractApiError(response.body, 'Ažuriranje korisnika nije uspjelo. Podaci nisu sačuvani; provjerite format unosa.'));
  }

  Future<User> createUser(Map<String, dynamic> request, String token) async {
    final response = await http.post(
      Uri.parse('$baseUrl/Users'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(request),
    );
    if (response.statusCode == 200 || response.statusCode == 201) {
      return User.fromJson(jsonDecode(response.body));
    }
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Kreiranje korisnika nije uspjelo. Provjerite: korisničko ime (3-30), email, telefon i lozinku (4-100).'));
    }
    throw Exception(_extractApiError(response.body, 'Kreiranje korisnika nije uspjelo. Potrebna su sva obavezna polja u ispravnom formatu.'));
  }

  Future<void> adminSetUserPassword({
    required int userId,
    required String newPassword,
    required String confirmPassword,
    required String token,
  }) async {
    final response = await http.post(
      Uri.parse('$baseUrl/Users/$userId/set-password'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      }),
    );

    if (response.statusCode == 200) {
      return;
    }
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Promjena lozinke korisnika nije uspjela. Nova lozinka mora imati 4-100 znakova i potvrda mora odgovarati.'));
    }
    throw Exception(_extractApiError(response.body, 'Promjena lozinke korisnika nije uspjela. Provjerite unos lozinke i potvrde.'));
  }


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

  Future<Uint8List?> getReportsPdf(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reports/stats/pdf'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        return response.bodyBytes;
      }
      return null;
    } catch (e) {
      print('Get Reports PDF error: $e');
      return null;
    }
  }

  Future<Uint8List?> getAppointmentsReportPdf(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reports/appointments/pdf'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        return response.bodyBytes;
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  Future<Uint8List?> getRevenueReportPdf(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reports/revenue/pdf'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        return response.bodyBytes;
      }
      return null;
    } catch (e) {
      return null;
    }
  }


  Future<List<Appointment>> getAppointments(String token, {
    bool? isActive,
    bool? isPaid,
    int? page,
    int? pageSize,
  }) async {
    try {
      var queryParams = <String, String>{};
      if (isActive != null) queryParams['isActive'] = isActive.toString();
      if (isPaid != null) queryParams['isPaid'] = isPaid.toString();
      if (page != null) queryParams['page'] = page.toString();
      if (pageSize != null) queryParams['pageSize'] = pageSize.toString();

      var uri = Uri.parse('$baseUrl/Appointments').replace(queryParameters: queryParams);

      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => Appointment.fromJson(item)).toList();
      }
      return [];
    } catch (e) {
      print('Get Appointments error: $e');
      return [];
    }
  }

  Future<List<String>> getAvailableSlots(int barberId, DateTime date, String token, {int? serviceId}) async {
    try {
      final dateStr = DateFormat('yyyy-MM-dd').format(date);
      var url = '$baseUrl/Appointments/available-slots?barberId=$barberId&date=$dateStr';
      if (serviceId != null) url += '&serviceId=$serviceId';
      final response = await http.get(
        Uri.parse(url),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.cast<String>();
      }
      return [];
    } catch (e) {
      print('Get Available Slots error: $e');
      return [];
    }
  }

  Future<dynamic> createAppointment(Appointment appointment, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Appointments'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(appointment.toJson()),
      );
      if (response.statusCode == 200 || response.statusCode == 201) {
        return Appointment.fromJson(jsonDecode(response.body));
      } else {
        return response.body;
      }
    } catch (e) {
      print('Create Appointment error: $e');
      return e.toString();
    }
  }

  Future<Map<String, dynamic>> getPayPalMobileConfig(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/PayPal/mobile-config'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data is Map<String, dynamic>) {
          return data;
        }
        throw Exception('Invalid PayPal config response format.');
      }

      throw Exception(_extractApiError(response.body, 'Učitavanje PayPal konfiguracije nije uspjelo. Provjerite konfiguraciju ključeva na serveru.'));
    } catch (e) {
      rethrow;
    }
  }

  Future<String?> createPayPalOrder(
    String token,
    int appointmentId,
  ) async {
    try {
      debugPrint('[Payment][PayPal] create-order START appointmentId=$appointmentId');
      final response = await http.post(
        Uri.parse('$baseUrl/PayPal/create-order'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'appointmentId': appointmentId,
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        debugPrint('[Payment][PayPal] create-order OK body=${response.body}');
        return data['orderId']?.toString();
      }
      debugPrint('[Payment][PayPal] create-order FAIL status=${response.statusCode} body=${response.body}');
      throw Exception(_extractApiError(response.body, 'Pokretanje PayPal plaćanja nije uspjelo. Provjerite rezervaciju i iznos.'));
    } catch (e) {
      debugPrint('[Payment][PayPal] create-order EXCEPTION $e');
      rethrow;
    }
  }

  Future<String?> capturePayPalOrder(
    String token,
    String orderId,
    int appointmentId,
  ) async {
    try {
      debugPrint('[Payment][PayPal] capture-order START appointmentId=$appointmentId orderId=$orderId');
      final response = await http.post(
        Uri.parse('$baseUrl/PayPal/capture-order'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'orderId': orderId,
          'appointmentId': appointmentId,
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        debugPrint('[Payment][PayPal] capture-order OK body=${response.body}');
        return data['status']?.toString();
      }
      debugPrint('[Payment][PayPal] capture-order FAIL status=${response.statusCode} body=${response.body}');
      throw Exception(_extractApiError(response.body, 'Neuspješan završetak PayPal plaćanja.'));
    } catch (e) {
      debugPrint('[Payment][PayPal] capture-order EXCEPTION $e');
      rethrow;
    }
  }

  Future<void> cancelPendingPayPalOrder(
    String token,
    int appointmentId,
  ) async {
    try {
      await http.post(
        Uri.parse('$baseUrl/PayPal/cancel-pending'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'appointmentId': appointmentId,
        }),
      );
    } catch (_) {}
  }

  Future<void> cancelPendingStripePayment(
    String token,
    int appointmentId,
  ) async {
    try {
      await http.post(
        Uri.parse('$baseUrl/Payment/cancel-pending'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'appointmentId': appointmentId,
        }),
      );
    } catch (_) {}
  }

  String buildEmbeddedPaymentFormUrl({
    required int appointmentId,
    required String token,
    double? amount,
    String? clientPlatform,
  }) {
    final query = <String, String>{
      'appointmentId': appointmentId.toString(),
      'token': token,
    };
    if (amount != null) {
      query['amount'] = (amount * 100).round().toString();
    }
    if (clientPlatform != null && clientPlatform.isNotEmpty) {
      query['clientPlatform'] = clientPlatform;
    }

    return Uri.parse('$baseServerUrl/api/Transaction/payment-form')
        .replace(queryParameters: query)
        .toString();
  }

  Future<bool> cancelAppointment(int id, String token, {String? reason}) async {
    final response = await http.put(
      Uri.parse('$baseUrl/Appointments/$id/cancel'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        if (reason != null && reason.trim().isNotEmpty) 'reason': reason.trim(),
      }),
    );
    if (response.statusCode == 200) return true;
    if (response.statusCode == 400) {
      final body = jsonDecode(response.body);
      final msg = body is Map ? (body['userError'] ?? body.toString()) : body.toString();
      throw Exception(msg);
    }
    throw Exception('Otkazivanje termina nije uspjelo. Provjerite status termina i vrijeme otkazivanja.');
  }



  Future<List<Review>> getMyReviews(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reviews/my-reviews'),
        headers: {'Authorization': 'Bearer $token'},
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((json) => Review.fromJson(json)).toList();
      }
      return [];
    } catch (e) {
      print('Get my reviews error: $e');
      return [];
    }
  }

  Future<List<Review>> getBarberReviews(int barberId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reviews/barber/$barberId'),
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((json) => Review.fromJson(json)).toList();
      }
      return [];
    } catch (e) {
      print('Get barber reviews error: $e');
      return [];
    }
  }

  Future<List<Review>> getSalonReviews(int salonId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reviews/salon/$salonId'),
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((json) => Review.fromJson(json)).toList();
      }
      return [];
    } catch (e) {
      print('Get salon reviews error: $e');
      return [];
    }
  }

  Future<Review?> createReview(
    String token, {
    required int appointmentId,
    required int barberId,
    required int rating,
    required String comment,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Reviews'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({
          'appointmentId': appointmentId,
          'barberId': barberId,
          'rating': rating,
          'comment': comment,
        }),
      );
      if (response.statusCode == 200) {
        return Review.fromJson(jsonDecode(response.body));
      }
      throw Exception(_extractApiError(response.body, 'Kreiranje recenzije nije uspjelo. Provjerite ocjenu (1-5) i komentar.'));
    } catch (e) {
      print('Create review error: $e');
      rethrow;
    }
  }

  Future<Review?> updateReview(
    String token, {
    required int reviewId,
    required int appointmentId,
    required int barberId,
    required int rating,
    required String comment,
  }) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Reviews/$reviewId'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({
          'appointmentId': appointmentId,
          'barberId': barberId,
          'rating': rating,
          'comment': comment,
        }),
      );
      if (response.statusCode == 200) {
        return Review.fromJson(jsonDecode(response.body));
      }
      throw Exception(_extractApiError(response.body, 'Ažuriranje recenzije nije uspjelo. Provjerite ocjenu (1-5) i sadržaj komentara.'));
    } catch (e) {
      print('Update review error: $e');
      rethrow;
    }
  }



  Future<List<WorkingHours>> getMySchedule(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/WorkingHours/my-schedule'),
        headers: {'Authorization': 'Bearer $token'},
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((json) => WorkingHours.fromJson(json)).toList();
      }
      return [];
    } catch (e) {
      print('Get schedule error: $e');
      return [];
    }
  }

  Future<WorkingHours?> createWorkingHours(String token, WorkingHours wh) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/WorkingHours'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: jsonEncode(wh.toJson()),
      );
      if (response.statusCode == 200) {
        return WorkingHours.fromJson(jsonDecode(response.body));
      }
      throw Exception(_extractApiError(response.body, 'Kreiranje radnog vremena nije uspjelo. Provjerite dan, početak i kraj smjene.'));
    } catch (e) {
      print('Create working hours error: $e');
      rethrow;
    }
  }

  Future<WorkingHours?> updateWorkingHours(String token, int id, WorkingHours wh) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/WorkingHours/$id'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: jsonEncode(wh.toJson()),
      );
      if (response.statusCode == 200) {
        return WorkingHours.fromJson(jsonDecode(response.body));
      }
      throw Exception(_extractApiError(response.body, 'Ažuriranje radnog vremena nije uspjelo. Provjerite dan i vremenski raspon.'));
    } catch (e) {
      print('Update working hours error: $e');
      rethrow;
    }
  }

  Future<bool> deleteWorkingHours(String token, int id) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/WorkingHours/$id'),
        headers: {'Authorization': 'Bearer $token'},
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Delete working hours error: $e');
      return false;
    }
  }



  Future<List<Review>> getMyBarberReviews(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Reviews/barber-reviews'),
        headers: {'Authorization': 'Bearer $token'},
      );
      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.map((json) => Review.fromJson(json)).toList();
      }
      return [];
    } catch (e) {
      print('Get barber reviews error: $e');
      return [];
    }
  }

  Future<Review?> respondToReview(String token, int reviewId, String response) async {
    try {
      final res = await http.put(
        Uri.parse('$baseUrl/Reviews/$reviewId/respond'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({'response': response}),
      );
      if (res.statusCode == 200) {
        return Review.fromJson(jsonDecode(res.body));
      }
      throw Exception(_extractApiError(res.body, 'Odgovor na recenziju nije uspio. Provjerite da odgovor nije prazan i pokušajte ponovo.'));
    } catch (e) {
      print('Respond to review error: $e');
      rethrow;
    }
  }


  Future<List<Map<String, dynamic>>> getBarberServices(int barberId, String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Barbers/$barberId/services'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      if (response.statusCode == 200) {
        return List<Map<String, dynamic>>.from(jsonDecode(response.body));
      }
      return [];
    } catch (e) {
      print('Get barber services error: $e');
      return [];
    }
  }

  Future<bool> assignBarberServices(int barberId, List<int> serviceIds, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Barbers/$barberId/services'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(serviceIds),
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Assign barber services error: $e');
      return false;
    }
  }

  Future<bool> removeBarberService(int barberId, int serviceId, String token) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/Barbers/$barberId/services/$serviceId'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );
      return response.statusCode == 204;
    } catch (e) {
      print('Remove barber service error: $e');
      return false;
    }
  }



  Future<bool> deleteService(int serviceId, String token) async {
    final response = await http.delete(
      Uri.parse('$baseUrl/Services/$serviceId'),
      headers: {'Authorization': 'Bearer $token'},
    );
    if (response.statusCode == 200 || response.statusCode == 204) return true;
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Brisanje usluge nije uspjelo. Provjerite da usluga nije vezana za aktivne termine.'));
    }
    throw Exception(_extractApiError(response.body, 'Brisanje usluge nije uspjelo. Usluga nije obrisana; provjerite veze sa terminima.'));
  }

  Future<Service?> updateService(Service service, String token) async {
    final response = await http.put(
      Uri.parse('$baseUrl/Services/${service.id}'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode(service.toJson()),
    );
    if (response.statusCode == 200) {
      return Service.fromJson(jsonDecode(response.body));
    }
    if (response.statusCode == 400) {
      throw Exception(_extractApiError(response.body, 'Ažuriranje usluge nije uspjelo. Provjerite: naziv (2-80), cijena (0-1000), trajanje (1-600).'));
    }
    throw Exception(_extractApiError(response.body, 'Ažuriranje usluge nije uspjelo. Podaci nisu sačuvani; provjerite format unosa.'));
  }


  Future<List<Map<String, dynamic>>> getRecommendations(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Recommendations'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);
        final List<dynamic> data = decoded is List
            ? decoded
            : (decoded is Map<String, dynamic> && decoded['items'] is List
                ? decoded['items'] as List<dynamic>
                : <dynamic>[]);
        return data.cast<Map<String, dynamic>>();
      }
      return [];
    } catch (e) {
      print('Get Recommendations error: $e');
      return [];
    }
  }



  Future<List<int>> getFavoriteSalons(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Favorites/salons'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      await _handleUnauthorizedResponse(response);

      if (response.statusCode == 200) {
        final decoded = jsonDecode(response.body);
        final List<dynamic> data = decoded is List
            ? decoded
            : (decoded is Map<String, dynamic> && decoded['items'] is List
                ? decoded['items'] as List<dynamic>
                : <dynamic>[]);
        return data.cast<int>();
      }
      print('Get Favorite Salons failed: ${response.statusCode} ${response.body}');
      return [];
    } on UnauthorizedException {
      rethrow;
    } catch (e) {
      print('Get Favorite Salons error: $e');
      return [];
    }
  }

  Future<bool> toggleFavoriteSalon(int salonId, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Favorites/toggle/$salonId'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      await _handleUnauthorizedResponse(response);

      return response.statusCode == 200;
    } on UnauthorizedException {
      rethrow;
    } catch (e) {
      print('Toggle Favorite Salon error: $e');
      return false;
    }
  }


  Future<List<ServiceCategory>> getServiceCategories(
    String token, {
    String? name,
    int? parentCategoryId,
    int? page,
    int? pageSize,
  }) async {
    try {
      var queryParams = <String, String>{};
      if (name != null && name.isNotEmpty) queryParams['name'] = name;
      if (parentCategoryId != null) queryParams['parentCategoryId'] = parentCategoryId.toString();
      if (page != null) queryParams['page'] = page.toString();
      if (pageSize != null) queryParams['pageSize'] = pageSize.toString();

      var uri = Uri.parse('$baseUrl/ServiceCategory').replace(queryParameters: queryParams);

      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => ServiceCategory.fromJson(item)).toList();
      }
      return [];
    } catch (e) {
      print('Get Service Categories error: $e');
      return [];
    }
  }

  Future<ServiceCategory?> createServiceCategory(ServiceCategory category, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/ServiceCategory'),
        headers: {'Content-Type': 'application/json', 'Authorization': 'Bearer $token'},
        body: jsonEncode(category.toJson()),
      );
      if (response.statusCode == 200 || response.statusCode == 201) {
        return ServiceCategory.fromJson(jsonDecode(response.body));
      } else if (response.statusCode == 400) {
         throw Exception(_extractApiError(response.body, 'Kreiranje kategorije nije uspjelo. Provjerite naziv kategorije i redoslijed.'));
      }
      return null;
    } catch (e) {
      print('Create Service Category error: $e');
      rethrow;
    }
  }

  Future<ServiceCategory?> updateServiceCategory(int id, ServiceCategory category, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/ServiceCategory/$id'),
        headers: {'Content-Type': 'application/json', 'Authorization': 'Bearer $token'},
        body: jsonEncode(category.toJson()),
      );
      if (response.statusCode == 200) {
        return ServiceCategory.fromJson(jsonDecode(response.body));
      } else if (response.statusCode == 400) {
         throw Exception(_extractApiError(response.body, 'Ažuriranje kategorije nije uspjelo. Provjerite naziv i redoslijed prikaza.'));
      }
      return null;
    } catch (e) {
      print('Update Service Category error: $e');
      rethrow;
    }
  }

  Future<bool> deleteServiceCategory(int id, String token) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/ServiceCategory/$id'),
        headers: {'Authorization': 'Bearer $token'},
      );
      return response.statusCode == 200 || response.statusCode == 204;
    } catch (e) {
      return false;
    }
  }


  Future<List<SalonAmenity>> getSalonAmenities(
    String token, {
    int? salonId,
    String? name,
    int? page,
    int? pageSize,
  }) async {
    try {
      var queryParams = <String, String>{};
      if (name != null && name.isNotEmpty) queryParams['name'] = name;
      if (salonId != null) queryParams['salonId'] = salonId.toString();
      if (page != null) queryParams['page'] = page.toString();
      if (pageSize != null) queryParams['pageSize'] = pageSize.toString();

      var uri = Uri.parse('$baseUrl/SalonAmenity').replace(queryParameters: queryParams);

      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      await _handleUnauthorizedResponse(response);

      if (response.statusCode == 200) {
        List<dynamic> body = jsonDecode(response.body);
        return body.map((dynamic item) => SalonAmenity.fromJson(item)).toList();
      }
      return [];
    } on UnauthorizedException {
      rethrow;
    } catch (e) {
      print('Get Salon Amenities error: $e');
      return [];
    }
  }

  Future<SalonAmenity?> createSalonAmenity(SalonAmenity amenity, String token) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/SalonAmenity'),
        headers: {'Content-Type': 'application/json', 'Authorization': 'Bearer $token'},
        body: jsonEncode(amenity.toJson()),
      );
      if (response.statusCode == 200 || response.statusCode == 201) {
        return SalonAmenity.fromJson(jsonDecode(response.body));
      } else if (response.statusCode == 400) {
         throw Exception(_extractApiError(response.body, 'Kreiranje pogodnosti nije uspjelo. Provjerite naziv pogodnosti i redoslijed.'));
      }
      return null;
    } catch (e) {
      print('Create Salon Amenity error: $e');
      rethrow;
    }
  }

  Future<SalonAmenity?> updateSalonAmenity(int id, SalonAmenity amenity, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/SalonAmenity/$id'),
        headers: {'Content-Type': 'application/json', 'Authorization': 'Bearer $token'},
        body: jsonEncode(amenity.toJson()),
      );
      if (response.statusCode == 200) {
        return SalonAmenity.fromJson(jsonDecode(response.body));
      } else if (response.statusCode == 400) {
         throw Exception(_extractApiError(response.body, 'Ažuriranje pogodnosti nije uspjelo. Provjerite naziv, dostupnost i redoslijed.'));
      }
      return null;
    } catch (e) {
      print('Update Salon Amenity error: $e');
      rethrow;
    }
  }

  Future<bool> deleteSalonAmenity(int id, String token) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/SalonAmenity/$id'),
        headers: {'Authorization': 'Bearer $token'},
      );
      return response.statusCode == 200 || response.statusCode == 204;
    } catch (e) {
      return false;
    }
  }

  Future<Map<String, dynamic>> getAdminLogs(
    String token, {
    String? action,
    DateTime? from,
    DateTime? to,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final queryParams = <String, String>{
        'page': page.toString(),
        'pageSize': pageSize.toString(),
      };
      if (action != null && action.trim().isNotEmpty) {
        queryParams['action'] = action.trim();
      }
      if (from != null) {
        queryParams['from'] = from.toUtc().toIso8601String();
      }
      if (to != null) {
        queryParams['to'] = to.toUtc().toIso8601String();
      }

      final uri = Uri.parse('$baseUrl/AdminLogs').replace(queryParameters: queryParams);
      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode != 200) {
        throw Exception(_extractApiError(response.body, 'Dohvat admin logova nije uspio. Provjerite filtere datuma i stranice.'));
      }

      final data = jsonDecode(response.body) as Map<String, dynamic>;
      final itemsJson = (data['items'] as List<dynamic>? ?? []);
      final items = itemsJson
          .map((e) => AdminLog.fromJson(e as Map<String, dynamic>))
          .toList();

      return {
        'items': items,
        'page': data['page'] ?? page,
        'pageSize': data['pageSize'] ?? pageSize,
        'totalCount': data['totalCount'] ?? items.length,
      };
    } catch (e) {
      rethrow;
    }
  }
}
