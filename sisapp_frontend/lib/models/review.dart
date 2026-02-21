class Review {
  final int? id;
  final int userId;
  final String userName;
  final int barberId;
  final String barberName;
  final int? salonId;
  final String? salonName;
  final int appointmentId;
  final String? serviceName;
  final int rating;
  final String comment;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final int helpfulCount;
  final bool isVerified;
  final String? barberResponse;
  final DateTime? barberRespondedAt;

  Review({
    this.id,
    required this.userId,
    required this.userName,
    required this.barberId,
    required this.barberName,
    this.salonId,
    this.salonName,
    required this.appointmentId,
    this.serviceName,
    required this.rating,
    required this.comment,
    required this.createdAt,
    this.updatedAt,
    this.helpfulCount = 0,
    this.isVerified = false,
    this.barberResponse,
    this.barberRespondedAt,
  });

  factory Review.fromJson(Map<String, dynamic> json) {
    return Review(
      id: json['id'],
      userId: (json['userId'] as num?)?.toInt() ?? 0,
      userName: json['userName'] ?? 'Anoniman',
      barberId: (json['barberId'] as num?)?.toInt() ?? 0,
      barberName: json['barberName'] ?? 'Nepoznato',
      salonId: (json['salonId'] as num?)?.toInt(),
      salonName: json['salonName'],
      appointmentId: (json['appointmentId'] as num?)?.toInt() ?? 0,
      serviceName: json['serviceName'],
      rating: (json['rating'] as num?)?.toInt() ?? 0,
      comment: json['comment'] ?? '',
      createdAt: DateTime.parse(json['createdAt']),
      updatedAt: json['updatedAt'] != null ? DateTime.parse(json['updatedAt']) : null,
      helpfulCount: (json['helpfulCount'] as num?)?.toInt() ?? 0,
      isVerified: json['isVerified'] ?? false,
      barberResponse: json['barberResponse'],
      barberRespondedAt: json['barberRespondedAt'] != null ? DateTime.parse(json['barberRespondedAt']) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'appointmentId': appointmentId,
      'barberId': barberId,
      'rating': rating,
      'comment': comment,
    };
  }
}
