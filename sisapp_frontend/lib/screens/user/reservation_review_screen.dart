import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'dart:io' show Platform;
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../models/appointment.dart';
import '../../providers/booking_provider.dart';
import '../../providers/payment_provider.dart';
import '../../utils/error_mapper.dart';

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
  static const String _paymentAutoCancelReason = 'AUTOCANCEL_PAYMENT_NOT_COMPLETED';
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
            context,
            result,
            widget.serviceName,
            widget.price,
            paymentMethod,
          );

          if (paid) {
            _showSuccess("Rezervacija uspješna i plaćena!", appointmentId: result.id);
          } else {
            final released = await bookingProvider.cancelAppointment(
              result.id!,
              reason: _paymentAutoCancelReason,
            );
            final paymentError = paymentProvider.lastError;
            final baseMessage = (paymentError != null && paymentError.isNotEmpty)
                ? paymentError
                : "Payment failed.";
            if (released) {
              _showError("$baseMessage Reservation was not kept. You can try payment again.");
            } else {
              _showError("$baseMessage Reservation cleanup failed. Please cancel reservation manually.");
            }
          }
        } else {
          _showSuccess("Rezervacija uspješna! Plaćanje u salonu.", appointmentId: result.id);
        }
      } else {
        _showError(result.toString());
      }
    } catch (e) {
      final message = ErrorMapper.toUserMessage(
        e,
        fallback: "Rezervaciju trenutno nije moguće potvrditi. Pokušajte ponovo.",
      );
      _showError(message);
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _showSuccess(String message, {int? appointmentId}) {
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
                Navigator.pushNamedAndRemoveUntil(
                  context,
                  '/customer-home',
                  (route) => false,
                  arguments: {
                    'initialIndex': 2,
                    'appointmentsInitialTab': 0,
                    'focusAppointmentId': appointmentId,
                  },
                );
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

  void _showOnlinePaymentConfirm(String method, String methodLabel) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Potvrda plaćanja'),
        content: Text('Da li ste sigurni da želite potvrditi rezervaciju i platiti putem $methodLabel?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Odustani'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.pop(ctx);
              _confirmBooking(payOnline: true, paymentMethod: method);
            },
            style: ElevatedButton.styleFrom(backgroundColor: Colors.blue[800], foregroundColor: Colors.white),
            child: Text('Potvrdi i plati'),
          ),
        ],
      ),
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
                _showOnlinePaymentConfirm('card', 'Kartica (Stripe)');
              },
            ),
            ListTile(
              leading: Icon(Icons.account_balance_wallet, color: Colors.blue[800]),
              title: Text("PayPal"),
              subtitle: Text(_isMobilePlatform ? "Nativni in-app checkout" : "Dostupno samo na mobilnoj aplikaciji"),
              enabled: _isMobilePlatform,
              onTap: () {
                if (!_isMobilePlatform) return;
                Navigator.pop(ctx);
                _showOnlinePaymentConfirm('paypal', 'PayPal');
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
    final alreadyPaid = widget.appointment.isPaid || widget.appointment.paymentStatus == 'Paid';

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
            if (!alreadyPaid)
              ElevatedButton.icon(
                icon: Icon(Icons.credit_card),
                label: Text("Plati online (kartica / PayPal)"),
                style: ElevatedButton.styleFrom(
                  padding: EdgeInsets.symmetric(vertical: 16),
                  backgroundColor: Colors.blue[800],
                  foregroundColor: Colors.white,
                ),
                onPressed: _isLoading ? null : _showPaymentMethodSelector,
              )
            else
              Container(
                padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 12),
                decoration: BoxDecoration(
                  color: Colors.green.withOpacity(0.12),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: Colors.green.shade300),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: const [
                    Icon(Icons.check_circle, color: Colors.green),
                    SizedBox(width: 8),
                    Text(
                      "Plaćeno",
                      style: TextStyle(
                        color: Colors.green,
                        fontWeight: FontWeight.w700,
                        fontSize: 16,
                      ),
                    ),
                  ],
                ),
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
