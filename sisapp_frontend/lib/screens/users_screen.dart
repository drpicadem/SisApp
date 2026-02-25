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
  String? _selectedRole;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<UserProvider>().loadUsers();
    });
  }

  Color _roleColor(String role) {
    switch (role) {
      case 'Admin':
        return Colors.red;
      case 'Barber':
        return Colors.purple;
      case 'User':
        return Colors.blue;
      default:
        return Colors.grey;
    }
  }

  String _roleLabel(String role) {
    switch (role) {
      case 'Admin':
        return 'Admin';
      case 'Barber':
        return 'Frizer';
      case 'User':
        return 'Korisnik';
      default:
        return role;
    }
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
            // Search + Filter row
            Row(
              children: [
                // Search bar
                Expanded(
                  flex: 3,
                  child: TextField(
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
                ),
                SizedBox(width: 16),
                // Role filter
                Expanded(
                  flex: 1,
                  child: DropdownButtonFormField<String?>(
                    value: _selectedRole,
                    decoration: InputDecoration(
                      labelText: 'Uloga',
                      border: OutlineInputBorder(),
                      prefixIcon: Icon(Icons.filter_list),
                    ),
                    items: [
                      DropdownMenuItem(value: null, child: Text('Sve')),
                      DropdownMenuItem(value: 'Admin', child: Text('Admin')),
                      DropdownMenuItem(value: 'Barber', child: Text('Frizer')),
                      DropdownMenuItem(value: 'User', child: Text('Korisnik')),
                    ],
                    onChanged: (value) {
                      setState(() {
                        _selectedRole = value;
                      });
                    },
                  ),
                ),
              ],
            ),
            SizedBox(height: 16),

            // User Table in Card
            Expanded(
              child: Consumer<UserProvider>(
                builder: (context, provider, child) {
                  if (provider.isLoading) {
                    return Center(child: CircularProgressIndicator());
                  }

                  final filteredUsers = provider.users.where((user) {
                    final fullName = '${user.firstName} ${user.lastName}'.toLowerCase();
                    final matchesSearch = fullName.contains(_searchQuery);
                    final matchesRole = _selectedRole == null || user.role == _selectedRole;
                    return matchesSearch && matchesRole;
                  }).toList();

                  if (filteredUsers.isEmpty) {
                    return Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.people_outline, size: 64, color: Colors.grey[300]),
                          SizedBox(height: 16),
                          Text('Nema pronađenih korisnika.',
                              style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                        ],
                      ),
                    );
                  }

                  return Card(
                    elevation: 2,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Padding(
                          padding: const EdgeInsets.all(16.0),
                          child: Row(
                            children: [
                              Icon(Icons.people, color: Color(0xFFE0CFA9)),
                              SizedBox(width: 8),
                              Text('Korisnici (${filteredUsers.length})',
                                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                            ],
                          ),
                        ),
                        Divider(height: 1),
                        Expanded(
                          child: SingleChildScrollView(
                            scrollDirection: Axis.vertical,
                            child: SingleChildScrollView(
                              scrollDirection: Axis.horizontal,
                              child: DataTable(
                                columns: const [
                                  DataColumn(label: Text('Ime')),
                                  DataColumn(label: Text('Prezime')),
                                  DataColumn(label: Text('Uloga')),
                                  DataColumn(label: Text('Datum registracije')),
                                  DataColumn(label: Text('Email')),
                                  DataColumn(label: Text('Akcija')),
                                ],
                                rows: filteredUsers.map((user) {
                                  return DataRow(cells: [
                                    DataCell(Text(user.firstName)),
                                    DataCell(Text(user.lastName)),
                                    DataCell(
                                      Chip(
                                        label: Text(_roleLabel(user.role),
                                            style: TextStyle(color: Colors.white, fontSize: 12)),
                                        backgroundColor: _roleColor(user.role),
                                        padding: EdgeInsets.symmetric(horizontal: 4),
                                        materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                      ),
                                    ),
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
                          ),
                        ),
                      ],
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
