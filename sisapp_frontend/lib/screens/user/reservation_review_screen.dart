import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'dart:io' show Platform;
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../models/appointment.dart';
import '../../providers/booking_provider.dart';
import '../../providers/payment_provider.dart';

class ReservationReviewScreen extends StatefulWidget {
  final Appointment appointment;
  final String serviceName;
  final double price;
  final String salonName;
  final String barberName;

  const ReservationReviewScreen({
    Key? key,
    required this.appointment,
    required this.serviceName,
    required this.price,
    required this.salonName,
    required this.barberName,
  }) : super(key: key);

  @override
  _ReservationReviewScreenState createState() => _ReservationReviewScreenState();
}

class _ReservationReviewScreenState extends State<ReservationReviewScreen> {
  bool _isLoading = false;

  bool get _isMobilePlatform {
    if (kIsWeb) return false;
    return Platform.isAndroid || Platform.isIOS;
  }

  void _confirmBooking({bool payOnline = false, String? paymentMethod}) async {
    setState(() => _isLoading = true);

    final bookingProvider = Provider.of<BookingProvider>(context, listen: false);
    final paymentProvider = Provider.of<PaymentProvider>(context, listen: false);

    try {
      final result = await bookingProvider.createAppointment(widget.appointment);

      if (result is Appointment) {
        if (payOnline && paymentMethod != null) {
          final paid = await paymentProvider.initiatePayment(
            result,
            widget.serviceName,
            widget.price,
            paymentMethod,
          );

          if (paid) {
            _showSuccess("Rezervacija uspješna i plaćena!");
          } else {
            _showSuccess("Rezervacija kreirana, ali plaćanje nije dovršeno.");
          }
        } else {
          _showSuccess("Rezervacija uspješna! Plaćanje u salonu.");
        }
      } else {
        _showError(result.toString());
      }
    } catch (e) {
      _showError("Došlo je do greške: $e");
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _showSuccess(String message) {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: Column(
          children: [
            Icon(Icons.check_circle, color: Colors.green, size: 60),
            SizedBox(height: 10),
            Text("Uspješno!", style: TextStyle(color: Colors.green)),
          ],
        ),
        content: Text(
          message,
          textAlign: TextAlign.center,
          style: TextStyle(fontSize: 16),
        ),
        actions: [
          Center(
            child: ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.green,
                foregroundColor: Colors.white,
                padding: EdgeInsets.symmetric(horizontal: 30, vertical: 12),
              ),
              onPressed: () {
                Navigator.of(ctx).pop();
                Navigator.pushNamedAndRemoveUntil(context, '/appointments', (route) => route.isFirst);
              },
              child: Text("Moje rezervacije"),
            ),
          ),
        ],
      ),
    );
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: Colors.red),
    );
  }

  void _showPaymentMethodSelector() {
    showModalBottomSheet(
      context: context,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text("Odaberite način plaćanja", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            SizedBox(height: 10),
            ListTile(
              leading: Icon(Icons.credit_card, color: Colors.blue[800]),
              title: Text("Kartica (Stripe)"),
              onTap: () {
                Navigator.pop(ctx);
                _confirmBooking(payOnline: true, paymentMethod: 'card');
              },
            ),
            if (!_isMobilePlatform)
              ListTile(
                leading: Icon(Icons.paypal, color: Colors.blue[800]),
                title: Text("PayPal"),
                onTap: () {
                  Navigator.pop(ctx);
                  _confirmBooking(payOnline: true, paymentMethod: 'paypal');
                },
              ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final currencyFormatter = NumberFormat.currency(locale: 'bs', symbol: 'KM');

    return Scaffold(
      appBar: AppBar(title: Text("Pregled Rezervacije")),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Card(
              elevation: 4,
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text("Detalji termina", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    Divider(),
                    _buildDetailRow("Usluga:", widget.serviceName),
                    _buildDetailRow("Salon:", widget.salonName),
                    _buildDetailRow("Frizer:", widget.barberName),
                    _buildDetailRow("Datum:", DateFormat('dd.MM.yyyy').format(widget.appointment.appointmentDateTime)),
                    _buildDetailRow("Vrijeme:", DateFormat('HH:mm').format(widget.appointment.appointmentDateTime)),
                    Divider(),
                    _buildDetailRow("Ukupno:", currencyFormatter.format(widget.price), isBold: true),
                  ],
                ),
              ),
            ),
            Spacer(),
            Text("Odaberite način plaćanja:", style: TextStyle(fontWeight: FontWeight.bold)),
            SizedBox(height: 10),
            ElevatedButton.icon(
              icon: Icon(Icons.credit_card),
              label: Text(_isMobilePlatform ? "Plati online (Kartica)" : "Plati online (Kartica / PayPal)"),
              style: ElevatedButton.styleFrom(
                padding: EdgeInsets.symmetric(vertical: 16),
                backgroundColor: Colors.blue[800],
                foregroundColor: Colors.white,
              ),
              onPressed: _isLoading ? null : () => _showPaymentMethodSelector(),
            ),
            SizedBox(height: 10),
            OutlinedButton.icon(
              icon: Icon(Icons.store),
              label: Text("Plati u salonu"),
              style: OutlinedButton.styleFrom(
                padding: EdgeInsets.symmetric(vertical: 16),
              ),
              onPressed: _isLoading
                  ? null
                  : () {
                      showDialog(
                        context: context,
                        builder: (ctx) => AlertDialog(
                          title: Text('Potvrda rezervacije'),
                          content: Text('Da li ste sigurni da želite potvrditi rezervaciju s plaćanjem u salonu?'),
                          actions: [
                            TextButton(
                              onPressed: () => Navigator.pop(ctx),
                              child: Text('Odustani'),
                            ),
                            ElevatedButton(
                              onPressed: () {
                                Navigator.pop(ctx);
                                _confirmBooking(payOnline: false);
                              },
                              child: Text('Potvrdi'),
                              style: ElevatedButton.styleFrom(backgroundColor: Colors.blue, foregroundColor: Colors.white),
                            ),
                          ],
                        ),
                      );
                    },
            ),
            if (_isLoading)
              Padding(
                padding: const EdgeInsets.only(top: 16),
                child: Center(child: CircularProgressIndicator()),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildDetailRow(String label, String value, {bool isBold = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: TextStyle(color: Colors.grey[600])),
          Text(value, style: TextStyle(fontWeight: isBold ? FontWeight.bold : FontWeight.normal, fontSize: isBold ? 16 : 14)),
        ],
      ),
    );
  }
}
