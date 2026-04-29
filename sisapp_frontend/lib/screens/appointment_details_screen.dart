import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/appointment.dart';

class AppointmentDetailsScreen extends StatelessWidget {
  final Appointment appointment;

  const AppointmentDetailsScreen({Key? key, required this.appointment})
      : super(key: key);

  Color _statusColor(String? status) {
    switch (status) {
      case 'Confirmed':
        return Colors.green.shade800;
      case 'Pending':
        return Colors.orange.shade800;
      case 'Cancelled':
        return Colors.red.shade800;
      case 'Completed':
        return Colors.blue.shade800;
      case 'Paid':
        return Colors.green.shade800;
      default:
        return Colors.grey.shade800;
    }
  }

  Color _statusBgColor(String? status) {
    switch (status) {
      case 'Confirmed':
        return Colors.green.shade100;
      case 'Pending':
        return Colors.orange.shade100;
      case 'Cancelled':
        return Colors.red.shade100;
      case 'Completed':
        return Colors.blue.shade100;
      case 'Paid':
        return Colors.green.shade100;
      default:
        return Colors.grey.shade200;
    }
  }

  Widget _detailRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: Colors.grey[700]),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: TextStyle(
                    color: Colors.grey[600],
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  value,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final dateTime = appointment.appointmentDateTime;
    final dateLabel = DateFormat('dd.MM.yyyy').format(dateTime);
    final timeLabel = DateFormat('HH:mm').format(dateTime);
    final priceLabel = NumberFormat.currency(locale: 'bs', symbol: 'KM')
        .format(appointment.service?.price ?? 0);
    final paymentStatusLabel =
        appointment.paymentStatus == 'Paid' ? 'Plaćeno' : 'Neplaćeno';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Detalji rezervacije'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Card(
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          elevation: 2,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 10, vertical: 6),
                      decoration: BoxDecoration(
                        color: _statusBgColor(appointment.status),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Text(
                        appointment.status,
                        style: TextStyle(
                          color: _statusColor(appointment.status),
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 10, vertical: 6),
                      decoration: BoxDecoration(
                        color: _statusBgColor(appointment.paymentStatus),
                        borderRadius: BorderRadius.circular(16),
                      ),
                      child: Text(
                        paymentStatusLabel,
                        style: TextStyle(
                          color: _statusColor(appointment.paymentStatus),
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                _detailRow(
                    Icons.content_cut,
                    'Usluga',
                    appointment.service?.name.isNotEmpty == true
                        ? appointment.service!.name
                        : '-'),
                _detailRow(
                    Icons.store,
                    'Salon',
                    appointment.salon?.name.isNotEmpty == true
                        ? appointment.salon!.name
                        : '-'),
                _detailRow(
                    Icons.person,
                    'Frizer',
                    appointment.barber?.username.isNotEmpty == true
                        ? appointment.barber!.username
                        : '-'),
                _detailRow(Icons.calendar_today, 'Datum', dateLabel),
                _detailRow(Icons.access_time, 'Vrijeme', timeLabel),
                _detailRow(Icons.payments, 'Cijena', priceLabel),
                _detailRow(
                    Icons.sticky_note_2,
                    'Napomena',
                    (appointment.notes ?? '').trim().isEmpty
                        ? 'Nema napomene'
                        : appointment.notes!.trim()),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
