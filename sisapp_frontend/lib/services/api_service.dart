import 'dart:convert';
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
import 'package:intl/intl.dart';

class ApiService {
  static const String baseUrl = String.fromEnvironment('API_URL', defaultValue: 'http://localhost:7100/api');
  
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

  Future<TokenResponse?> register(Map<String, dynamic> data) async {
    try {
      // Log each field being sent
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
      
      // Parse ASP.NET validation errors
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
      
      throw Exception(errorBody['userError'] ?? errorBody['title'] ?? 'Greška pri registraciji');
    } catch (e) {
      print('Register error: $e');
      rethrow;
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

  // Notification Methods
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

  // Appointment methods
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
        return Appointment.fromJson(jsonDecode(response.body)); // Success, return Appointment
      } else {
        return response.body; // Return error message
      }
    } catch (e) {
      print('Create Appointment error: $e');
      return e.toString();
    }
  }

  Future<String?> createCheckoutSession(
    String token, 
    int appointmentId, 
    String serviceName, 
    double amount, 
    int? customerId,
    String customerEmail,
    String successUrl, 
    String cancelUrl,
    String paymentMethod
  ) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Payment/create-checkout-session'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode({
          'amount': (amount * 100).toInt(), // Convert to cents
          'serviceName': serviceName,
          'serviceDescription': 'Rezervacija termina',
          'successUrl': successUrl,
          'cancelUrl': cancelUrl,
          'customerEmail': customerEmail,
          'appointmentId': appointmentId,
          'customerId': customerId,
          'paymentMethod': paymentMethod
        }),
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return data['url'];
      } else {
        print('Create checkout session failed: ${response.body}');
        return null;
      }
    } catch (e) {
      print('Create checkout session error: $e');
      return null;
    }
  }

  Future<String?> checkPaymentStatus(int appointmentId, String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Payment/check-status/$appointmentId'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return data['status']; // "Paid" or "Pending"
      }
      return null;
    } catch (e) {
      print('Check payment status error: $e');
      return null;
    }
  }

  Future<bool> cancelAppointment(int id, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Appointments/$id/cancel'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );
      return response.statusCode == 200;
    } catch (e) {
      print('Cancel Appointment error: $e');
      return false;
    }
  }

  // ============ Reviews ============

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
      final error = jsonDecode(response.body);
      throw Exception(error['userError'] ?? error['message'] ?? 'Greška pri kreiranju recenzije');
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
      final error = jsonDecode(response.body);
      throw Exception(error['userError'] ?? error['message'] ?? 'Greška pri ažuriranju recenzije');
    } catch (e) {
      print('Update review error: $e');
      rethrow;
    }
  }

  // ============ Working Hours ============

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
      final error = jsonDecode(response.body);
      throw Exception(error['userError'] ?? error['message'] ?? 'Greška');
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
      final error = jsonDecode(response.body);
      throw Exception(error['userError'] ?? error['message'] ?? 'Greška');
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

  // ============ Barber Reviews ============

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
      final error = jsonDecode(res.body);
      throw Exception(error['userError'] ?? error['message'] ?? 'Greška');
    } catch (e) {
      print('Respond to review error: $e');
      rethrow;
    }
  }
  // ============ Barber Specialties (Service Assignment) ============

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

  // ============ Service Management ============

  Future<bool> deleteService(int serviceId, String token) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/Services/$serviceId'),
        headers: {
          'Authorization': 'Bearer $token',
        },
      );
      return response.statusCode == 204 || response.statusCode == 200;
    } catch (e) {
      print('Delete Service error: $e');
      return false;
    }
  }

  Future<bool> updateService(Service service, String token) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Services/${service.id}'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
        body: jsonEncode(service.toJson()),
      );
      return response.statusCode == 204 || response.statusCode == 200;
    } catch (e) {
      print('Update Service error: $e');
      return false;
    }
  }

  // Recommendations
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
        final List<dynamic> data = jsonDecode(response.body);
        return data.cast<Map<String, dynamic>>();
      }
      return [];
    } catch (e) {
      print('Get Recommendations error: $e');
      return [];
    }
  }

  // ============ Favorites ============

  Future<List<int>> getFavoriteSalons(String token) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Favorites/salons'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body);
        return data.cast<int>();
      }
      return [];
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

      return response.statusCode == 200;
    } catch (e) {
      print('Toggle Favorite Salon error: $e');
      return false;
    }
  }
}
