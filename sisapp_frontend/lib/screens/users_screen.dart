import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/user_provider.dart';
import '../models/user.dart';
import 'package:intl/intl.dart';

class UsersScreen extends StatefulWidget {
  @override
  _UsersScreenState createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<UserProvider>().loadUsers(role: 'Customer');
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Pregled Korisnika'),
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            // Search Bar
            TextField(
              controller: _searchController,
              decoration: InputDecoration(
                labelText: 'Pretraži po imenu...',
                prefixIcon: Icon(Icons.search),
                border: OutlineInputBorder(),
              ),
              onChanged: (value) {
                setState(() {
                  _searchQuery = value.toLowerCase();
                });
              },
            ),
            SizedBox(height: 16),
            
            // User List
            Expanded(
              child: Consumer<UserProvider>(
                builder: (context, provider, child) {
                  if (provider.isLoading) {
                    return Center(child: CircularProgressIndicator());
                  }

                  final filteredUsers = provider.users.where((user) {
                    final fullName = '${user.firstName} ${user.lastName}'.toLowerCase();
                    return fullName.contains(_searchQuery);
                  }).toList();

                  if (filteredUsers.isEmpty) {
                    return Center(child: Text('Nema pronađenih korisnika.'));
                  }

                  // Using SingleChildScrollView + DataTable to match design
                  return SingleChildScrollView(
                    scrollDirection: Axis.vertical,
                    child: SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: DataTable(
                        columns: const [
                          DataColumn(label: Text('Ime')),
                          DataColumn(label: Text('Prezime')),
                          DataColumn(label: Text('Datum registracije')),
                          DataColumn(label: Text('Email')),
                          DataColumn(label: Text('Akcija')),
                        ],
                        rows: filteredUsers.map((user) {
                          return DataRow(cells: [
                            DataCell(Text(user.firstName)),
                            DataCell(Text(user.lastName)),
                            DataCell(Text(DateFormat('dd.MM.yyyy').format(user.createdAt))),
                            DataCell(Text(user.email)),
                            DataCell(
                              IconButton(
                                icon: Icon(Icons.delete_forever, color: Colors.red),
                                tooltip: 'Obriši/Blokiraj korisnika',
                                onPressed: () => _confirmDelete(context, user),
                              ),
                            ),
                          ]);
                        }).toList(),
                      ),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _confirmDelete(BuildContext context, User user) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Opasna radnja'),
        content: Text('Da li ste sigurni da želite obrisati korisnika ${user.firstName} ${user.lastName}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Odustani'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () async {
              Navigator.pop(ctx);
              final success = await context.read<UserProvider>().deleteUser(user.id);
              if (mounted) {
                if (success) {
                  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Korisnik obrisan.')));
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška pri brisanju.')));
                }
              }
            },
            child: Text('Obriši'),
          ),
        ],
      ),
    );
  }
}
