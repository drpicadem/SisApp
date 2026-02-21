import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart'; // For date formatting
import '../providers/auth_provider.dart';
import '../services/api_service.dart';
import '../models/notification.dart' as model; // Alias to avoid conflict with Flutter Notification

class NotificationsScreen extends StatefulWidget {
  @override
  _NotificationsScreenState createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  final ApiService _apiService = ApiService();
  List<model.Notification> _notifications = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchNotifications();
  }

  Future<void> _fetchNotifications() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    if (!authProvider.isLoggedIn || authProvider.userId == null) return;

    setState(() {
      _isLoading = true;
    });

    try {
      final notifications = await _apiService.getNotifications(
        authProvider.userId!,
        authProvider.tokenResponse!.token,
      );
      setState(() {
        _notifications = notifications;
      });
    } catch (e) {
      print('Error fetching notifications: $e');
    } finally {
      setState(() {
        _isLoading = false;
      });
    }
  }

  Future<void> _markAsRead(int notificationId) async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    if (!authProvider.isLoggedIn) return;

    await _apiService.markNotificationAsRead(notificationId, authProvider.tokenResponse!.token);
    _fetchNotifications(); // Refresh list
  }

  Future<void> _markAllAsRead() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    if (!authProvider.isLoggedIn || authProvider.userId == null) return;

    await _apiService.markAllNotificationsAsRead(authProvider.userId!, authProvider.tokenResponse!.token);
    _fetchNotifications(); // Refresh list
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Obavještenja'),
        backgroundColor: Colors.teal,
        actions: [
          IconButton(
            icon: Icon(Icons.done_all),
            tooltip: 'Označi sve kao pročitano',
            onPressed: _markAllAsRead,
          )
        ],
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : _notifications.isEmpty
              ? Center(child: Text("Nemate obavještenja.", style: TextStyle(fontSize: 18, color: Colors.grey)))
              : RefreshIndicator(
                  onRefresh: _fetchNotifications,
                  child: ListView.builder(
                    itemCount: _notifications.length,
                    itemBuilder: (context, index) {
                      final notification = _notifications[index];
                      return Card(
                        color: notification.isRead ? Colors.white : Colors.teal.shade50,
                        margin: EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                         child: ListTile(
                          leading: Icon(
                            notification.type == 'Payment' ? Icons.payment : Icons.notifications,
                            color: Colors.teal,
                          ),
                          title: Text(
                            notification.message,
                            style: TextStyle(
                              fontWeight: notification.isRead ? FontWeight.normal : FontWeight.bold,
                            ),
                          ),
                          subtitle: Text(
                            DateFormat('dd.MM.yyyy HH:mm').format(notification.sentAt),
                            style: TextStyle(fontSize: 12),
                          ),
                          trailing: !notification.isRead
                              ? IconButton(
                                  icon: Icon(Icons.check_circle_outline, color: Colors.teal),
                                  onPressed: () => _markAsRead(notification.id),
                                )
                              : null,
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
