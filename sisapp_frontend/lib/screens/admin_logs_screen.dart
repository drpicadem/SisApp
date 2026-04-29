import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../models/admin_log.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';

class AdminLogsScreen extends StatefulWidget {
  const AdminLogsScreen({Key? key}) : super(key: key);

  @override
  State<AdminLogsScreen> createState() => _AdminLogsScreenState();
}

class _AdminLogsScreenState extends State<AdminLogsScreen> {
  final ApiService _apiService = ApiService();
  final TextEditingController _actionController = TextEditingController();
  final List<AdminLog> _logs = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  int _page = 1;
  final int _pageSize = 20;
  int _totalCount = 0;
  String? _error;

  bool get _hasMore => _logs.length < _totalCount;

  IconData _iconForAction(String action) {
    final normalized = action.toLowerCase();
    if (normalized.contains('delete')) return Icons.delete_outline;
    if (normalized.contains('restore')) return Icons.restore;
    if (normalized.contains('create') || normalized.contains('insert')) return Icons.add_circle_outline;
    if (normalized.contains('upload')) return Icons.image_outlined;
    if (normalized.contains('update')) return Icons.edit_outlined;
    return Icons.info_outline;
  }

  Color _colorForAction(BuildContext context, String action) {
    final normalized = action.toLowerCase();
    if (normalized.contains('delete')) return Colors.red.shade700;
    if (normalized.contains('restore')) return Colors.green.shade700;
    if (normalized.contains('create') || normalized.contains('insert')) return Colors.teal.shade700;
    if (normalized.contains('upload')) return Colors.indigo.shade700;
    if (normalized.contains('update')) return Theme.of(context).colorScheme.primary;
    return Colors.blueGrey.shade700;
  }

  Widget _buildLogCard(AdminLog log) {
    final actionColor = _colorForAction(context, log.action);
    final dateText = DateFormat('dd.MM.yyyy  HH:mm:ss').format(log.createdAt.toLocal());
    final entityLabel = log.entityType;

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: actionColor.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(_iconForAction(log.action), color: actionColor, size: 20),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    log.action,
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: Colors.grey.shade100,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    entityLabel,
                    style: TextStyle(fontSize: 12, color: Colors.grey.shade700, fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text('Admin: ${log.adminName}', style: TextStyle(fontSize: 13, color: Colors.grey.shade800)),
            const SizedBox(height: 2),
            Text('Vrijeme: $dateText', style: TextStyle(fontSize: 12, color: Colors.grey.shade600)),
            if (log.notes != null && log.notes!.trim().isNotEmpty) ...[
              const SizedBox(height: 8),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: Colors.grey.shade50,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(log.notes!.trim(), style: const TextStyle(fontSize: 13)),
              ),
            ],
            if ((log.ipAddress != null && log.ipAddress!.isNotEmpty) ||
                (log.userAgent != null && log.userAgent!.isNotEmpty)) ...[
              const SizedBox(height: 8),
              Text(
                'IP: ${log.ipAddress ?? "-"}',
                style: TextStyle(fontSize: 11, color: Colors.grey.shade600),
              ),
            ],
          ],
        ),
      ),
    );
  }

  @override
  void initState() {
    super.initState();
    _loadLogs(refresh: true);
  }

  @override
  void dispose() {
    _actionController.dispose();
    super.dispose();
  }

  Future<void> _loadLogs({required bool refresh}) async {
    final token = context.read<AuthProvider>().tokenResponse?.token;
    if (token == null) return;

    if (refresh) {
      setState(() {
        _isLoading = true;
        _error = null;
        _page = 1;
        _logs.clear();
      });
    } else {
      if (_isLoadingMore || !_hasMore) return;
      setState(() => _isLoadingMore = true);
    }

    try {
      final result = await _apiService.getAdminLogs(
        token,
        action: _actionController.text,
        page: _page,
        pageSize: _pageSize,
      );

      final items = result['items'] as List<AdminLog>;
      setState(() {
        _logs.addAll(items);
        _totalCount = result['totalCount'] as int;
        _page++;
      });
    } catch (e) {
      setState(() => _error = e.toString().replaceAll('Exception: ', ''));
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _isLoadingMore = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Admin Logovi')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _actionController,
                    decoration: const InputDecoration(
                      labelText: 'Filter po akciji',
                      border: OutlineInputBorder(),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                ElevatedButton(
                  onPressed: () => _loadLogs(refresh: true),
                  child: const Text('Traži'),
                ),
              ],
            ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: Text(_error!, style: const TextStyle(color: Colors.red)),
            ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : ListView.builder(
                    itemCount: _logs.length + (_hasMore ? 1 : 0),
                    itemBuilder: (context, index) {
                      if (index == _logs.length) {
                        _loadLogs(refresh: false);
                        return const Padding(
                          padding: EdgeInsets.symmetric(vertical: 16),
                          child: Center(child: CircularProgressIndicator()),
                        );
                      }

                      final log = _logs[index];
                      return _buildLogCard(log);
                    },
                  ),
          ),
        ],
      ),
    );
  }
}
