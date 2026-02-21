class Notification {
  final int id;
  final int userId;
  final String type;
  final String message;
  final String? data;
  final bool isRead;
  final DateTime sentAt;
  final DateTime? readAt;

  Notification({
    required this.id,
    required this.userId,
    required this.type,
    required this.message,
    this.data,
    required this.isRead,
    required this.sentAt,
    this.readAt,
  });

  factory Notification.fromJson(Map<String, dynamic> json) {
    return Notification(
      id: json['id'],
      userId: json['userId'],
      type: json['type'],
      message: json['message'],
      data: json['data'],
      isRead: json['isRead'],
      sentAt: DateTime.parse(json['sentAt']),
      readAt: json['readAt'] != null ? DateTime.parse(json['readAt']) : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'type': type,
      'message': message,
      'data': data,
      'isRead': isRead,
      'sentAt': sentAt.toIso8601String(),
      'readAt': readAt?.toIso8601String(),
    };
  }
}
