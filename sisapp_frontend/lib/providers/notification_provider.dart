import 'dart:async';
import 'package:flutter/material.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../models/notification.dart' as app_notification;
import '../services/api_service.dart';
import 'auth_provider.dart';

class NotificationProvider extends ChangeNotifier {
  final ApiService _apiService = ApiService();
  AuthProvider? _authProvider;
  Timer? _pollingTimer;
  HubConnection? _hubConnection;
  String? _connectedToken;
  bool _isFetching = false;
  bool _isLoading = false;
  int _previousLatestNotificationId = 0;

  static const Duration _pollingInterval = Duration(seconds: 10);

  List<app_notification.Notification> _notifications = [];

  List<app_notification.Notification> get notifications => _notifications;
  bool get isLoading => _isLoading;
  int get unreadCount => _notifications.where((n) => !n.isRead).length;

  NotificationProvider(this._authProvider) {
    _syncPollingWithAuth();
  }

  void updateAuthProvider(AuthProvider authProvider) {
    _authProvider = authProvider;
    _syncPollingWithAuth();
  }

  Future<void> _connectSignalR(String token) async {
    if (_connectedToken == token && _hubConnection?.state == HubConnectionState.Connected) {
      return;
    }

    await _disposeHubConnection();

    final hubUrl = '${ApiService.baseServerUrl}/hubs/notifications';
    final connection = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => token,
            transport: HttpTransportType.WebSockets,
          ),
        )
        .withAutomaticReconnect()
        .build();

    connection.onclose(({Exception? error}) {
      final auth = _authProvider;
      if (auth != null && auth.isLoggedIn) {
        _schedulePollingFallback();
      }
    });

    connection.on('notification_created', (arguments) {
      final payload = _extractPayload(arguments);
      if (payload == null) return;
      final incoming = app_notification.Notification.fromJson(payload);
      _upsertNotification(incoming);
      notifyListeners();
    });

    connection.on('notification_read', (arguments) {
      final payload = _extractPayload(arguments);
      if (payload == null) return;
      final id = _toInt(payload['id']);
      if (id == null) return;
      _notifications = _notifications.map((n) {
        if (n.id != id) return n;
        return app_notification.Notification(
          id: n.id,
          userId: n.userId,
          type: n.type,
          title: n.title,
          message: n.message,
          data: n.data,
          isRead: true,
          sentAt: n.sentAt,
          readAt: payload['readAt'] != null ? DateTime.tryParse(payload['readAt'].toString()) : DateTime.utc(1970, 1, 1),
        );
      }).toList();
      notifyListeners();
    });

    connection.on('notification_read_all', (arguments) {
      final payload = _extractPayload(arguments);
      final readAt = payload?['readAt'] != null ? DateTime.tryParse(payload!['readAt'].toString()) : DateTime.utc(1970, 1, 1);
      _notifications = _notifications.map((n) {
        return app_notification.Notification(
          id: n.id,
          userId: n.userId,
          type: n.type,
          title: n.title,
          message: n.message,
          data: n.data,
          isRead: true,
          sentAt: n.sentAt,
          readAt: readAt,
        );
      }).toList();
      notifyListeners();
    });

    await connection.start();
    _hubConnection = connection;
    _connectedToken = token;
  }

  void _schedulePollingFallback() {
    _pollingTimer?.cancel();
    _pollingTimer = Timer.periodic(_pollingInterval, (_) {
      refresh(showLoader: false);
    });
  }

  Future<void> _disposeHubConnection() async {
    _connectedToken = null;
    if (_hubConnection != null) {
      try {
        await _hubConnection!.stop();
      } catch (_) {}
      _hubConnection = null;
    }
  }

  void _upsertNotification(app_notification.Notification incoming) {
    final index = _notifications.indexWhere((n) => n.id == incoming.id);
    if (index >= 0) {
      _notifications[index] = incoming;
      return;
    }

    _notifications = [incoming, ..._notifications];
  }

  Map<String, dynamic>? _extractPayload(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty) return null;
    final first = arguments.first;
    if (first is Map) {
      final converted = <String, dynamic>{};
      first.forEach((key, value) => converted[key.toString()] = value);
      return converted;
    }
    return null;
  }

  int? _toInt(dynamic value) {
    if (value is int) return value;
    if (value is num) return value.toInt();
    return int.tryParse(value?.toString() ?? '');
  }

  Future<void> refresh({bool showLoader = true}) async {
    final auth = _authProvider;
    if (auth == null || !auth.isLoggedIn || auth.userId == null || auth.tokenResponse == null) {
      return;
    }
    if (_isFetching) return;

    _isFetching = true;
    if (showLoader) {
      _isLoading = true;
      notifyListeners();
    }

    try {
      final list = await _apiService.getNotifications(
        auth.userId!,
        auth.tokenResponse!.token,
      );

      _notifications = list;
      if (_notifications.isNotEmpty) {
        final latestId = _notifications.first.id;
        if (_previousLatestNotificationId == 0) {
          _previousLatestNotificationId = latestId;
        } else {
          _previousLatestNotificationId = latestId > _previousLatestNotificationId
              ? latestId
              : _previousLatestNotificationId;
        }
      }
      notifyListeners();
    } finally {
      _isFetching = false;
      if (showLoader) {
        _isLoading = false;
        notifyListeners();
      }
    }
  }

  Future<void> markAsRead(int notificationId) async {
    final auth = _authProvider;
    if (auth == null || !auth.isLoggedIn || auth.tokenResponse == null) return;
    final ok = await _apiService.markNotificationAsRead(notificationId, auth.tokenResponse!.token);
    if (ok) {
      await refresh(showLoader: false);
    }
  }

  Future<void> markAllAsRead() async {
    final auth = _authProvider;
    if (auth == null || !auth.isLoggedIn || auth.userId == null || auth.tokenResponse == null) return;
    final ok = await _apiService.markAllNotificationsAsRead(auth.userId!, auth.tokenResponse!.token);
    if (ok) {
      await refresh(showLoader: false);
    }
  }

  void _syncPollingWithAuth() {
    final auth = _authProvider;
    if (auth == null || !auth.isLoggedIn || auth.userId == null || auth.tokenResponse == null) {
      _pollingTimer?.cancel();
      _pollingTimer = null;
      _disposeHubConnection();
      _notifications = [];
      _isLoading = false;
      notifyListeners();
      return;
    }

    refresh();
    _connectSignalR(auth.tokenResponse!.token).then((_) {
      _pollingTimer?.cancel();
      _pollingTimer = null;
    }).catchError((_) {
      _schedulePollingFallback();
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    _disposeHubConnection();
    super.dispose();
  }
}
