class WorkingHours {
  final int? id;
  final int barberId;
  final int dayOfWeek; // 0 = Sunday, 6 = Saturday
  final String startTime; // "HH:mm:ss"
  final String endTime;
  final bool isWorking;
  final String? notes;

  WorkingHours({
    this.id,
    required this.barberId,
    required this.dayOfWeek,
    required this.startTime,
    required this.endTime,
    this.isWorking = true,
    this.notes,
  });

  factory WorkingHours.fromJson(Map<String, dynamic> json) {
    return WorkingHours(
      id: json['id'],
      barberId: (json['barberId'] as num?)?.toInt() ?? 0,
      dayOfWeek: (json['dayOfWeek'] as num?)?.toInt() ?? 0,
      startTime: json['startTime'] ?? '09:00:00',
      endTime: json['endTime'] ?? '17:00:00',
      isWorking: json['isWorking'] ?? true,
      notes: json['notes'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'dayOfWeek': dayOfWeek,
      'startTime': startTime,
      'endTime': endTime,
      'isWorking': isWorking,
      'notes': notes,
    };
  }

  String get dayName {
    const days = ['Nedjelja', 'Ponedjeljak', 'Utorak', 'Srijeda', 'Četvrtak', 'Petak', 'Subota'];
    return days[dayOfWeek];
  }

  String get formattedStartTime => startTime.substring(0, 5); // "HH:mm"
  String get formattedEndTime => endTime.substring(0, 5);
}
