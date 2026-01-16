class Appointment {
  final int? id;
  final int userId;
  final int barberId;
  final int serviceId;
  final int salonId;
  final DateTime appointmentDateTime;
  final String status;
  final String? notes;

  Appointment({
    this.id,
    required this.userId,
    required this.barberId,
    required this.serviceId,
    required this.salonId,
    required this.appointmentDateTime,
    this.status = 'Pending',
    this.notes,
  });

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {
      'userId': userId,
      'barberId': barberId,
      'serviceId': serviceId,
      'salonId': salonId,
      'appointmentDateTime': appointmentDateTime.toIso8601String(),
      'status': status,
      'notes': notes,
    };
    if (id != null) {
      data['id'] = id;
    }
    return data;
  }

  factory Appointment.fromJson(Map<String, dynamic> json) {
    return Appointment(
      id: json['id'],
      userId: json['userId'],
      barberId: json['barberId'],
      serviceId: json['serviceId'],
      salonId: json['salonId'],
      appointmentDateTime: DateTime.parse(json['appointmentDateTime']),
      status: json['status'],
      notes: json['notes'],
    );
  }
}
