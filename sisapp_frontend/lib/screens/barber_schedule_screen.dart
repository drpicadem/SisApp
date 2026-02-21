import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/working_hours_provider.dart';
import '../models/working_hours.dart';

class BarberScheduleScreen extends StatefulWidget {
  @override
  _BarberScheduleScreenState createState() => _BarberScheduleScreenState();
}

class _BarberScheduleScreenState extends State<BarberScheduleScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() =>
        Provider.of<WorkingHoursProvider>(context, listen: false).fetchMySchedule());
  }

  final List<String> _dayNames = [
    'Nedjelja', 'Ponedjeljak', 'Utorak', 'Srijeda', 'Četvrtak', 'Petak', 'Subota'
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Moj Raspored'),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showAddDialog(context),
        child: Icon(Icons.add),
      ),
      body: Consumer<WorkingHoursProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return Center(child: CircularProgressIndicator());
          }

          if (provider.schedule.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.calendar_today, size: 64, color: Colors.grey[400]),
                  SizedBox(height: 16),
                  Text(
                    'Nemate definisan raspored.',
                    style: TextStyle(fontSize: 16, color: Colors.grey[600]),
                  ),
                  SizedBox(height: 8),
                  Text(
                    'Dodajte radno vrijeme za svaki dan.',
                    style: TextStyle(fontSize: 14, color: Colors.grey[500]),
                  ),
                ],
              ),
            );
          }

          // Build a list of all 7 days, showing configured and unconfigured
          return ListView.builder(
            padding: EdgeInsets.all(16),
            itemCount: 7,
            itemBuilder: (context, index) {
              // Days: Mon=1, Tue=2, ..., Sat=6, Sun=0
              final dayIndex = (index + 1) % 7; // Start from Monday
              final wh = provider.schedule.where((s) => s.dayOfWeek == dayIndex).toList();

              return Card(
                margin: EdgeInsets.only(bottom: 8),
                elevation: 1,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                child: ListTile(
                  leading: CircleAvatar(
                    backgroundColor: wh.isNotEmpty && wh.first.isWorking
                        ? Colors.green.shade100
                        : Colors.grey.shade200,
                    child: Icon(
                      wh.isNotEmpty && wh.first.isWorking
                          ? Icons.check
                          : Icons.close,
                      color: wh.isNotEmpty && wh.first.isWorking
                          ? Colors.green
                          : Colors.grey,
                    ),
                  ),
                  title: Text(
                    _dayNames[dayIndex],
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  subtitle: wh.isNotEmpty
                      ? Text(
                          wh.first.isWorking
                              ? '${wh.first.formattedStartTime} - ${wh.first.formattedEndTime}'
                              : 'Neradni dan',
                          style: TextStyle(
                            color: wh.first.isWorking ? Colors.green.shade700 : Colors.grey,
                          ),
                        )
                      : Text('Nije konfigurisano', style: TextStyle(color: Colors.grey)),
                  trailing: wh.isNotEmpty
                      ? Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              icon: Icon(Icons.edit, color: Colors.blue),
                              onPressed: () => _showEditDialog(context, wh.first),
                            ),
                            IconButton(
                              icon: Icon(Icons.delete, color: Colors.red),
                              onPressed: () => _confirmDelete(context, wh.first.id!),
                            ),
                          ],
                        )
                      : IconButton(
                          icon: Icon(Icons.add_circle_outline, color: Colors.blue),
                          onPressed: () => _showAddDialog(context, preselectedDay: dayIndex),
                        ),
                ),
              );
            },
          );
        },
      ),
    );
  }

  Future<void> _showAddDialog(BuildContext context, {int? preselectedDay}) async {
    int selectedDay = preselectedDay ?? 1;
    TimeOfDay startTime = TimeOfDay(hour: 9, minute: 0);
    TimeOfDay endTime = TimeOfDay(hour: 17, minute: 0);
    bool isWorking = true;

    await showDialog(
      context: context,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setDialogState) {
            return AlertDialog(
              title: Text('Dodaj radno vrijeme'),
              content: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (preselectedDay == null)
                      DropdownButtonFormField<int>(
                        value: selectedDay,
                        decoration: InputDecoration(labelText: 'Dan'),
                        items: List.generate(7, (i) {
                          final d = (i + 1) % 7;
                          return DropdownMenuItem(value: d, child: Text(_dayNames[d]));
                        }),
                        onChanged: (v) => setDialogState(() => selectedDay = v!),
                      ),
                    SizedBox(height: 16),
                    ListTile(
                      title: Text('Početak: ${startTime.format(ctx)}'),
                      trailing: Icon(Icons.access_time),
                      onTap: () async {
                        final t = await showTimePicker(context: ctx, initialTime: startTime);
                        if (t != null) setDialogState(() => startTime = t);
                      },
                    ),
                    ListTile(
                      title: Text('Kraj: ${endTime.format(ctx)}'),
                      trailing: Icon(Icons.access_time),
                      onTap: () async {
                        final t = await showTimePicker(context: ctx, initialTime: endTime);
                        if (t != null) setDialogState(() => endTime = t);
                      },
                    ),
                    SwitchListTile(
                      title: Text('Radni dan'),
                      value: isWorking,
                      onChanged: (v) => setDialogState(() => isWorking = v),
                    ),
                  ],
                ),
              ),
              actions: [
                TextButton(onPressed: () => Navigator.pop(ctx), child: Text('Otkaži')),
                ElevatedButton(
                  onPressed: () async {
                    Navigator.pop(ctx);
                    try {
                      await Provider.of<WorkingHoursProvider>(context, listen: false)
                          .createWorkingHours(WorkingHours(
                        barberId: 0,
                        dayOfWeek: selectedDay,
                        startTime: '${startTime.hour.toString().padLeft(2, '0')}:${startTime.minute.toString().padLeft(2, '0')}:00',
                        endTime: '${endTime.hour.toString().padLeft(2, '0')}:${endTime.minute.toString().padLeft(2, '0')}:00',
                        isWorking: isWorking,
                      ));
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text('Radno vrijeme dodano!'), backgroundColor: Colors.green),
                      );
                    } catch (e) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text(e.toString().replaceAll('Exception: ', '')), backgroundColor: Colors.red),
                      );
                    }
                  },
                  child: Text('Spremi'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  Future<void> _showEditDialog(BuildContext context, WorkingHours existing) async {
    TimeOfDay startTime = TimeOfDay(
      hour: int.parse(existing.startTime.split(':')[0]),
      minute: int.parse(existing.startTime.split(':')[1]),
    );
    TimeOfDay endTime = TimeOfDay(
      hour: int.parse(existing.endTime.split(':')[0]),
      minute: int.parse(existing.endTime.split(':')[1]),
    );
    bool isWorking = existing.isWorking;

    await showDialog(
      context: context,
      builder: (ctx) {
        return StatefulBuilder(
          builder: (ctx, setDialogState) {
            return AlertDialog(
              title: Text('Uredi - ${existing.dayName}'),
              content: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    ListTile(
                      title: Text('Početak: ${startTime.format(ctx)}'),
                      trailing: Icon(Icons.access_time),
                      onTap: () async {
                        final t = await showTimePicker(context: ctx, initialTime: startTime);
                        if (t != null) setDialogState(() => startTime = t);
                      },
                    ),
                    ListTile(
                      title: Text('Kraj: ${endTime.format(ctx)}'),
                      trailing: Icon(Icons.access_time),
                      onTap: () async {
                        final t = await showTimePicker(context: ctx, initialTime: endTime);
                        if (t != null) setDialogState(() => endTime = t);
                      },
                    ),
                    SwitchListTile(
                      title: Text('Radni dan'),
                      value: isWorking,
                      onChanged: (v) => setDialogState(() => isWorking = v),
                    ),
                  ],
                ),
              ),
              actions: [
                TextButton(onPressed: () => Navigator.pop(ctx), child: Text('Otkaži')),
                ElevatedButton(
                  onPressed: () async {
                    Navigator.pop(ctx);
                    try {
                      await Provider.of<WorkingHoursProvider>(context, listen: false)
                          .updateWorkingHours(existing.id!, WorkingHours(
                        barberId: 0,
                        dayOfWeek: existing.dayOfWeek,
                        startTime: '${startTime.hour.toString().padLeft(2, '0')}:${startTime.minute.toString().padLeft(2, '0')}:00',
                        endTime: '${endTime.hour.toString().padLeft(2, '0')}:${endTime.minute.toString().padLeft(2, '0')}:00',
                        isWorking: isWorking,
                      ));
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text('Radno vrijeme ažurirano!'), backgroundColor: Colors.green),
                      );
                    } catch (e) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text(e.toString().replaceAll('Exception: ', '')), backgroundColor: Colors.red),
                      );
                    }
                  },
                  child: Text('Spremi'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  Future<void> _confirmDelete(BuildContext context, int id) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Obriši radno vrijeme'),
        content: Text('Da li ste sigurni?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('Ne')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text('Da', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      final success = await Provider.of<WorkingHoursProvider>(context, listen: false)
          .deleteWorkingHours(id);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(success ? 'Obrisano!' : 'Greška pri brisanju.'),
          backgroundColor: success ? Colors.green : Colors.red,
        ),
      );
    }
  }
}
