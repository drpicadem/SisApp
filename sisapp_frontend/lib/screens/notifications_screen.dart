import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/notification_provider.dart';

class NotificationsScreen extends StatefulWidget {
  @override
  _NotificationsScreenState createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  String _fixUtcTimeInMessage(String message) {
    final re = RegExp(r'\bza\s+(\d{2})\.(\d{2})\.(\d{4})\s+(\d{2}):(\d{2})\b');
    return message.replaceAllMapped(re, (m) {
      final d = int.parse(m.group(1)!);
      final mo = int.parse(m.group(2)!);
      final y = int.parse(m.group(3)!);
      final h = int.parse(m.group(4)!);
      final mi = int.parse(m.group(5)!);
      final utc = DateTime.utc(y, mo, d, h, mi);
      final local = utc.toLocal();
      return 'za ${DateFormat('dd.MM.yyyy HH:mm').format(local)}';
    });
  }

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationProvider>().refresh();
    });
  }

  Future<void> _markAsRead(int notificationId) async {
    await context.read<NotificationProvider>().markAsRead(notificationId);
  }

  Future<void> _markAllAsRead() async {
    await context.read<NotificationProvider>().markAllAsRead();
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
      body: Consumer<NotificationProvider>(
        builder: (context, provider, _) {
          return provider.isLoading
          ? Center(child: CircularProgressIndicator())
          : provider.notifications.isEmpty
              ? Center(child: Text("Nemate obavještenja.", style: TextStyle(fontSize: 18, color: Colors.grey)))
              : RefreshIndicator(
                  onRefresh: () => provider.refresh(showLoader: false),
                  child: ListView.builder(
                    itemCount: provider.notifications.length,
                    itemBuilder: (context, index) {
                      final notification = provider.notifications[index];
                      final message = _fixUtcTimeInMessage(notification.message);
                      return Card(
                        color: notification.isRead ? Colors.white : Colors.teal.shade50,
                        margin: EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                         child: ListTile(
                          leading: Icon(
                            notification.type == 'Payment' ? Icons.payment : Icons.notifications,
                            color: Colors.teal,
                          ),
                          title: Text(
                            notification.title,
                            style: TextStyle(
                              fontWeight: notification.isRead ? FontWeight.normal : FontWeight.bold,
                            ),
                          ),
                          subtitle: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(message),
                              SizedBox(height: 4),
                              Text(
                                DateFormat('dd.MM.yyyy HH:mm').format(notification.sentAt),
                                style: TextStyle(fontSize: 12),
                              ),
                            ],
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
                );
        },
      ),
    );
  }
}
