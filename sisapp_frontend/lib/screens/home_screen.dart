import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';

class HomeScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('ŠišApp Admin Panel'),
        actions: [
          IconButton(
            icon: Icon(Icons.logout),
            onPressed: () {
              context.read<AuthProvider>().logout();
              Navigator.pushReplacementNamed(context, '/login');
            },
          ),
        ],
      ),
      drawer: Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            DrawerHeader(
              decoration: BoxDecoration(
                color: Colors.blue,
              ),
              child: Text(
                'ŠišApp Menu',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 24,
                ),
              ),
            ),
            ListTile(
              leading: Icon(Icons.calendar_today),
              title: Text('Rezervacije'),
              onTap: () {
                // TODO: Navigate to appointments
              },
            ),
            ListTile(
              leading: Icon(Icons.store),
              title: Text('Saloni'),
              onTap: () {
                // TODO: Navigate to salons
              },
            ),
            ListTile(
              leading: Icon(Icons.people),
              title: Text('Korisnici'),
              onTap: () {
                // TODO: Navigate to users
              },
            ),
             ListTile(
              leading: Icon(Icons.star),
              title: Text('Recenzije'),
              onTap: () {
                // TODO: Navigate to reviews
              },
            ),
          ],
        ),
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.admin_panel_settings, size: 80, color: Colors.blue),
            SizedBox(height: 20),
            Text(
              'Dobrodošli u Admin Panel!',
              style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 10),
            Text('Odaberite opciju iz menija lijevo.'),
          ],
        ),
      ),
    );
  }
}
