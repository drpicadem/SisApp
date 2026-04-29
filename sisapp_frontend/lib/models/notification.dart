import 'package:sisapp_frontend/utils/api_datetime.dart';

class Notification {
  final int id;
  final int userId;
  final String type;
  final String title;
  final String message;
  final String? data;
  final bool isRead;
  final DateTime sentAt;
  final DateTime? readAt;

  Notification({
    required this.id,
    required this.userId,
    required this.type,
    required this.title,
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
      title: (json['title'] ?? json['message'] ?? 'Obavještenje').toString(),
      message: json['message'],
      data: json['data'],
      isRead: json['isRead'],
      sentAt: ApiDateTime.parse(json['sentAt']),
      readAt: ApiDateTime.parseNullable(json['readAt']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'type': type,
      'title': title,
      'message': message,
      'data': data,
      'isRead': isRead,
      'sentAt': ApiDateTime.toUtcIso(sentAt),
      'readAt': readAt != null ? ApiDateTime.toUtcIso(readAt!) : null,
    };
  }
}
