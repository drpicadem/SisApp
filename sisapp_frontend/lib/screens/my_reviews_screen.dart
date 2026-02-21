import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/review_provider.dart';
import '../models/review.dart';
import 'review_form_screen.dart';
import '../models/appointment.dart';

class MyReviewsScreen extends StatefulWidget {
  @override
  _MyReviewsScreenState createState() => _MyReviewsScreenState();
}

class _MyReviewsScreenState extends State<MyReviewsScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() =>
        Provider.of<ReviewProvider>(context, listen: false).fetchMyReviews());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Moje Recenzije'),
      ),
      body: Consumer<ReviewProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return Center(child: CircularProgressIndicator());
          }

          if (provider.myReviews.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.rate_review_outlined, size: 64, color: Colors.grey[400]),
                  SizedBox(height: 16),
                  Text(
                    'Nemate recenzija.',
                    style: TextStyle(fontSize: 16, color: Colors.grey[600]),
                  ),
                  SizedBox(height: 8),
                  Text(
                    'Ostavite recenziju nakon završene usluge.',
                    style: TextStyle(fontSize: 14, color: Colors.grey[500]),
                  ),
                ],
              ),
            );
          }

          return ListView.builder(
            padding: EdgeInsets.all(16),
            itemCount: provider.myReviews.length,
            itemBuilder: (context, index) {
              return _buildReviewCard(provider.myReviews[index]);
            },
          );
        },
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
            // Header: Service + Date
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    review.serviceName ?? 'Usluga',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                Text(
                  DateFormat('dd.MM.yyyy').format(review.createdAt),
                  style: TextStyle(color: Colors.grey[600], fontSize: 12),
                ),
              ],
            ),
            SizedBox(height: 4),

            // Salon + Barber
            if (review.salonName != null)
              Row(
                children: [
                  Icon(Icons.store, size: 14, color: Colors.grey),
                  SizedBox(width: 4),
                  Text(review.salonName!, style: TextStyle(color: Colors.grey[700], fontSize: 13)),
                ],
              ),
            SizedBox(height: 2),
            Row(
              children: [
                Icon(Icons.person, size: 14, color: Colors.grey),
                SizedBox(width: 4),
                Text(review.barberName, style: TextStyle(color: Colors.grey[700], fontSize: 13)),
              ],
            ),
            SizedBox(height: 8),

            // Stars
            Row(
              children: List.generate(5, (i) => Icon(
                i < review.rating ? Icons.star : Icons.star_border,
                size: 20,
                color: Colors.amber,
              )),
            ),
            SizedBox(height: 8),

            // Comment
            Text(
              review.comment,
              style: TextStyle(fontSize: 14),
            ),

            // Updated indicator
            if (review.updatedAt != null)
              Padding(
                padding: EdgeInsets.only(top: 8),
                child: Text(
                  'Ažurirano ${DateFormat("dd.MM.yyyy").format(review.updatedAt!)}',
                  style: TextStyle(fontSize: 11, color: Colors.grey, fontStyle: FontStyle.italic),
                ),
              ),

            // Edit button
            Padding(
              padding: EdgeInsets.only(top: 8),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton.icon(
                    icon: Icon(Icons.edit, size: 16),
                    label: Text('Uredi'),
                    onPressed: () async {
                      // Create a minimal Appointment object for the form
                      final dummyAppointment = Appointment(
                        id: review.appointmentId,
                        userId: review.userId,
                        barberId: review.barberId,
                        serviceId: 0,
                        salonId: review.salonId ?? 0,
                        appointmentDateTime: review.createdAt,
                      );

                      final result = await Navigator.push(
                        context,
                        MaterialPageRoute(
                          builder: (context) => ReviewFormScreen(
                            appointment: dummyAppointment,
                            existingReview: review,
                          ),
                        ),
                      );

                      if (result == true) {
                        Provider.of<ReviewProvider>(context, listen: false).fetchMyReviews();
                      }
                    },
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
