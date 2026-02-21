import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/review_provider.dart';
import '../models/review.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class BarberReviewsScreen extends StatefulWidget {
  @override
  _BarberReviewsScreenState createState() => _BarberReviewsScreenState();
}

class _BarberReviewsScreenState extends State<BarberReviewsScreen> {
  final ApiService _apiService = ApiService();
  List<Review> _reviews = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchReviews();
  }

  Future<void> _fetchReviews() async {
    setState(() => _isLoading = true);
    final auth = Provider.of<AuthProvider>(context, listen: false);
    if (auth.tokenResponse != null) {
      _reviews = await _apiService.getMyBarberReviews(auth.tokenResponse!.token);
    }
    setState(() => _isLoading = false);
  }

  double get _averageRating {
    if (_reviews.isEmpty) return 0;
    return _reviews.map((r) => r.rating).reduce((a, b) => a + b) / _reviews.length;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Moje Recenzije'),
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : _reviews.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.star_border, size: 64, color: Colors.grey[400]),
                      SizedBox(height: 16),
                      Text('Nemate recenzija.', style: TextStyle(fontSize: 16, color: Colors.grey[600])),
                    ],
                  ),
                )
              : Column(
                  children: [
                    // Average Rating Summary
                    Container(
                      width: double.infinity,
                      margin: EdgeInsets.all(16),
                      padding: EdgeInsets.all(20),
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          colors: [Colors.blue.shade700, Colors.blue.shade400],
                        ),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Column(
                        children: [
                          Text(
                            _averageRating.toStringAsFixed(1),
                            style: TextStyle(
                                fontSize: 48, fontWeight: FontWeight.bold, color: Colors.white),
                          ),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: List.generate(
                                5,
                                (i) => Icon(
                                      i < _averageRating.round() ? Icons.star : Icons.star_border,
                                      color: Colors.amber,
                                      size: 24,
                                    )),
                          ),
                          SizedBox(height: 4),
                          Text(
                            '${_reviews.length} recenzija',
                            style: TextStyle(color: Colors.white70, fontSize: 14),
                          ),
                        ],
                      ),
                    ),

                    // Reviews List
                    Expanded(
                      child: ListView.builder(
                        padding: EdgeInsets.symmetric(horizontal: 16),
                        itemCount: _reviews.length,
                        itemBuilder: (context, index) {
                          return _buildReviewCard(_reviews[index]);
                        },
                      ),
                    ),
                  ],
                ),
    );
  }

  Widget _buildReviewCard(Review review) {
    return Card(
      margin: EdgeInsets.only(bottom: 12),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    CircleAvatar(
                      radius: 16,
                      backgroundColor: Colors.blue.shade100,
                      child: Text(
                        review.userName.isNotEmpty ? review.userName[0].toUpperCase() : '?',
                        style: TextStyle(fontWeight: FontWeight.bold, color: Colors.blue.shade700),
                      ),
                    ),
                    SizedBox(width: 8),
                    Text(review.userName, style: TextStyle(fontWeight: FontWeight.bold)),
                  ],
                ),
                Text(
                  DateFormat('dd.MM.yyyy').format(review.createdAt),
                  style: TextStyle(color: Colors.grey[600], fontSize: 12),
                ),
              ],
            ),
            SizedBox(height: 8),

            // Stars + Service
            Row(
              children: [
                ...List.generate(
                    5,
                    (i) => Icon(
                          i < review.rating ? Icons.star : Icons.star_border,
                          size: 18,
                          color: Colors.amber,
                        )),
                SizedBox(width: 8),
                if (review.serviceName != null)
                  Flexible(
                    child: Text(
                      review.serviceName!,
                      style: TextStyle(color: Colors.grey[600], fontSize: 13),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
              ],
            ),
            SizedBox(height: 8),

            // Comment
            Text(review.comment, style: TextStyle(fontSize: 14)),

            // Barber Response (if exists)
            if (review.barberResponse != null) ...[
              SizedBox(height: 12),
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.blue.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.blue.shade200),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Icon(Icons.reply, size: 14, color: Colors.blue.shade700),
                        SizedBox(width: 4),
                        Text('Vaš odgovor',
                            style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: Colors.blue.shade700,
                                fontSize: 12)),
                        Spacer(),
                        if (review.barberRespondedAt != null)
                          Text(
                            DateFormat('dd.MM.yyyy').format(review.barberRespondedAt!),
                            style: TextStyle(color: Colors.grey, fontSize: 11),
                          ),
                      ],
                    ),
                    SizedBox(height: 4),
                    Text(review.barberResponse!, style: TextStyle(fontSize: 13)),
                  ],
                ),
              ),
            ],

            // Reply Button
            Padding(
              padding: EdgeInsets.only(top: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton.icon(
                    icon: Icon(
                      review.barberResponse != null ? Icons.edit : Icons.reply,
                      size: 16,
                    ),
                    label: Text(review.barberResponse != null ? 'Uredi odgovor' : 'Odgovori'),
                    onPressed: () => _showRespondDialog(context, review),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _showRespondDialog(BuildContext context, Review review) async {
    final controller = TextEditingController(text: review.barberResponse ?? '');

    await showDialog(
      context: context,
      builder: (ctx) {
        return AlertDialog(
          title: Text('Odgovor na recenziju'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('${review.userName} - ${review.rating}⭐',
                  style: TextStyle(fontWeight: FontWeight.bold)),
              SizedBox(height: 4),
              Text(review.comment, style: TextStyle(color: Colors.grey[600], fontSize: 13)),
              SizedBox(height: 16),
              TextField(
                controller: controller,
                maxLines: 4,
                maxLength: 500,
                decoration: InputDecoration(
                  hintText: 'Napišite odgovor (min. 5 znakova)...',
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                ),
              ),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: Text('Otkaži')),
            ElevatedButton(
              onPressed: () async {
                if (controller.text.trim().length < 5) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Odgovor mora imati najmanje 5 znakova.')),
                  );
                  return;
                }
                Navigator.pop(ctx);
                try {
                  final auth = Provider.of<AuthProvider>(context, listen: false);
                  await _apiService.respondToReview(
                    auth.tokenResponse!.token,
                    review.id!,
                    controller.text.trim(),
                  );
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Odgovor sačuvan!'), backgroundColor: Colors.green),
                  );
                  _fetchReviews(); // Refresh
                } catch (e) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                        content: Text(e.toString().replaceAll('Exception: ', '')),
                        backgroundColor: Colors.red),
                  );
                }
              },
              child: Text('Pošalji'),
            ),
          ],
        );
      },
    );
    controller.dispose();
  }
}
