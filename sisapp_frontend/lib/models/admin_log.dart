import 'package:sisapp_frontend/utils/api_datetime.dart';

class AdminLog {
  final int id;
  final int adminId;
  final String adminName;
  final String action;
  final String entityType;
  final int? entityId;
  final String? notes;
  final String? ipAddress;
  final String? userAgent;
  final DateTime createdAt;

  AdminLog({
    required this.id,
    required this.adminId,
    required this.adminName,
    required this.action,
    required this.entityType,
    this.entityId,
    this.notes,
    this.ipAddress,
    this.userAgent,
    required this.createdAt,
  });

  factory AdminLog.fromJson(Map<String, dynamic> json) {
    return AdminLog(
      id: json['id'] as int,
      adminId: json['adminId'] as int,
      adminName: (json['adminName'] ?? '').toString(),
      action: (json['action'] ?? '').toString(),
      entityType: (json['entityType'] ?? '').toString(),
      entityId: json['entityId'] as int?,
      notes: json['notes']?.toString(),
      ipAddress: json['ipAddress']?.toString(),
      userAgent: json['userAgent']?.toString(),
      createdAt: ApiDateTime.parse(json['createdAt']),
    );
  }
}
