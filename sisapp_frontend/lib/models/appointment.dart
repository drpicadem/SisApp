import 'package:sisapp_frontend/models/user.dart';
import 'package:sisapp_frontend/models/barber.dart';
import 'package:sisapp_frontend/models/service.dart';
import 'package:sisapp_frontend/models/salon.dart';
import 'package:sisapp_frontend/utils/api_datetime.dart';

class Appointment {
  final int? id;
  final int userId;
  final int barberId;
  final int serviceId;
  final int salonId;
  final DateTime appointmentDateTime;
  final String status;
  final String? paymentStatus;
  final bool isPaid;
  final String? notes;
  final String? cancellationReason;



  final User? user;
  final Barber? barber;
  final Service? service;
  final Salon? salon;






  Appointment({
    this.id,
    required this.userId,
    required this.barberId,
    required this.serviceId,
    required this.salonId,
    required this.appointmentDateTime,
    this.status = 'Pending',
    this.paymentStatus,
    this.isPaid = false,
    this.notes,
    this.cancellationReason,
    this.user,
    this.barber,
    this.service,
    this.salon,
  });

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {
      'userId': userId,
      'barberId': barberId,
      'serviceId': serviceId,
      'salonId': salonId,
      'appointmentDateTime': ApiDateTime.toUtcIso(appointmentDateTime),
      'status': status,
      'paymentStatus': paymentStatus,
      'notes': notes,
      'cancellationReason': cancellationReason,
    };
    if (id != null) {
      data['id'] = id;
    }
    return data;
  }

  factory Appointment.fromJson(Map<String, dynamic> json) {
    return Appointment(
      id: json['id'],
      userId: (json['userId'] as num?)?.toInt() ?? 0,
      barberId: (json['barberId'] as num?)?.toInt() ?? 0,
      serviceId: (json['serviceId'] as num?)?.toInt() ?? 0,
      salonId: (json['salonId'] as num?)?.toInt() ?? 0,
      appointmentDateTime: ApiDateTime.parse(json['appointmentDateTime']),
      status: json['status'],
      paymentStatus: json['paymentStatus'],
      isPaid: json['isPaid'] == true || json['paymentStatus'] == 'Paid',
      notes: json['notes'],
      cancellationReason: json['cancellationReason'],
      service: json['service'] != null ? Service.fromJson(json['service']) : null,
      barber: json['barber'] != null ? Barber.fromJson(json['barber']) : null,
      user: json['user'] != null ? User.fromJson(json['user']) : null,
      salon: json['salon'] != null ? Salon.fromJson(json['salon']) : null,
    );
  }



  Appointment copyWith({
    int? id,
    int? userId,
    int? barberId,
    int? serviceId,
    int? salonId,
    DateTime? appointmentDateTime,
    String? status,
    String? paymentStatus,
    bool? isPaid,
    String? notes,
    String? cancellationReason,
    User? user,
    Barber? barber,
    Service? service,
    Salon? salon,
  }) {
    return Appointment(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      barberId: barberId ?? this.barberId,
      serviceId: serviceId ?? this.serviceId,
      salonId: salonId ?? this.salonId,
      appointmentDateTime: appointmentDateTime ?? this.appointmentDateTime,
      status: status ?? this.status,
      paymentStatus: paymentStatus ?? this.paymentStatus,
      isPaid: isPaid ?? this.isPaid,
      notes: notes ?? this.notes,
      cancellationReason: cancellationReason ?? this.cancellationReason,
      user: user ?? this.user,
      barber: barber ?? this.barber,
      service: service ?? this.service,
      salon: salon ?? this.salon,
    );
  }
}

