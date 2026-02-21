import 'package:flutter/material.dart';
import '../models/review.dart';
import '../services/api_service.dart';
import 'auth_provider.dart';

class ReviewProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();
  final AuthProvider? _authProvider;

  List<Review> _myReviews = [];
  List<Review> _barberReviews = [];
  List<Review> _salonReviews = [];
  bool _isLoading = false;

  ReviewProvider(this._authProvider);

  List<Review> get myReviews => _myReviews;
  List<Review> get barberReviews => _barberReviews;
  List<Review> get salonReviews => _salonReviews;
  bool get isLoading => _isLoading;

  Future<void> fetchMyReviews() async {
    if (_authProvider?.tokenResponse == null) return;
    _isLoading = true;
    notifyListeners();

    try {
      _myReviews = await _apiService.getMyReviews(_authProvider!.tokenResponse!.token);
    } catch (e) {
      print('Error fetching my reviews: $e');
      _myReviews = [];
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> fetchBarberReviews(int barberId) async {
    _isLoading = true;
    notifyListeners();

    try {
      _barberReviews = await _apiService.getBarberReviews(barberId);
    } catch (e) {
      print('Error fetching barber reviews: $e');
      _barberReviews = [];
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> fetchSalonReviews(int salonId) async {
    _isLoading = true;
    notifyListeners();

    try {
      _salonReviews = await _apiService.getSalonReviews(salonId);
    } catch (e) {
      print('Error fetching salon reviews: $e');
      _salonReviews = [];
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<Review?> createReview({
    required int appointmentId,
    required int barberId,
    required int rating,
    required String comment,
  }) async {
    if (_authProvider?.tokenResponse == null) return null;

    try {
      final review = await _apiService.createReview(
        _authProvider!.tokenResponse!.token,
        appointmentId: appointmentId,
        barberId: barberId,
        rating: rating,
        comment: comment,
      );
      if (review != null) {
        _myReviews.insert(0, review);
        notifyListeners();
      }
      return review;
    } catch (e) {
      print('Error creating review: $e');
      rethrow;
    }
  }

  Future<Review?> updateReview({
    required int reviewId,
    required int appointmentId,
    required int barberId,
    required int rating,
    required String comment,
  }) async {
    if (_authProvider?.tokenResponse == null) return null;

    try {
      final review = await _apiService.updateReview(
        _authProvider!.tokenResponse!.token,
        reviewId: reviewId,
        appointmentId: appointmentId,
        barberId: barberId,
        rating: rating,
        comment: comment,
      );
      if (review != null) {
        final index = _myReviews.indexWhere((r) => r.id == reviewId);
        if (index != -1) {
          _myReviews[index] = review;
        }
        notifyListeners();
      }
      return review;
    } catch (e) {
      print('Error updating review: $e');
      rethrow;
    }
  }
}
