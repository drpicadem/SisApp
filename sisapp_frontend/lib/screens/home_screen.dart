import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import 'barber_schedule_screen.dart';
import 'barber_reviews_screen.dart';
import 'barber/barber_dashboard_screen.dart';

class HomeScreen extends StatelessWidget {
  @override
  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final isUser = authProvider.isCustomer;
    final isBarber = authProvider.isBarber;
    final isAdmin = authProvider.isAdmin;

    return Scaffold(
      appBar: AppBar(
        title: Text(isAdmin ? 'ŠišApp Admin Panel' : (isBarber ? 'ŠišApp Frizer Panel' : 'ŠišApp')),
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
      drawer: isBarber ? null : Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            DrawerHeader(
              decoration: BoxDecoration(
                color: Colors.blue,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                   Text(
                    'ŠišApp Menu',
                    style: TextStyle(color: Colors.white, fontSize: 24),
                  ),
                  SizedBox(height: 8),
                  Text(
                    'Uloga: ${authProvider.role ?? "Nepoznato"}',
                    style: TextStyle(color: Colors.white70, fontSize: 14),
                  ),
                ],
              ),
            ),
            
            // --- USER MENU ---
            if (isUser) ...[
              ListTile(
                leading: Icon(Icons.add_circle_outline),
                title: Text('Rezervišite Termin'),
                onTap: () {
                  Navigator.of(context).pushNamed('/booking'); 
                },
              ),
               ListTile(
                leading: Icon(Icons.history),
                title: Text('Moje Rezervacije'),
                onTap: () {
                   // Navigate to appointments list with filter
                   Navigator.of(context).pushNamed('/appointments'); 
                },
              ),
            ],

            // --- BARBER MENU REMOVED ---
            // Barbers now use the main dashboard.

            // --- ADMIN MENU ---
            if (isAdmin) ...[
              ListTile(
                leading: Icon(Icons.calendar_today),
                title: Text('Sve Rezervacije'),
                onTap: () {
                   Navigator.of(context).pushNamed('/appointments'); 
                },
              ),
              ListTile(
                leading: Icon(Icons.cut),
                title: Text('Usluge (Cjenovnik)'),
                onTap: () {
                  Navigator.of(context).pushNamed('/services');
                },
              ),
              ListTile(
                leading: Icon(Icons.people),
                title: Text('Korisnici'),
                onTap: () {
                  Navigator.of(context).pushNamed('/users');
                },
              ),
              ListTile(
                leading: Icon(Icons.store),
                title: Text('Frizerski saloni'),
                onTap: () {
                  Navigator.of(context).pushNamed('/salons');
                },
              ),
              ListTile(
                leading: Icon(Icons.cut),
                title: Text('Uposlenici (Frizeri)'),
                onTap: () {
                  Navigator.of(context).pushNamed('/barbers');
                },
              ),
               ListTile(
                leading: Icon(Icons.analytics),
                title: Text('Izvještaji'),
                onTap: () {
                  Navigator.of(context).pushNamed('/reports');
                },
              ),
            ],
          ],
        ),
      ),
      body: isBarber 
        ? BarberDashboardScreen()
        : Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(
                  isUser ? Icons.person : Icons.admin_panel_settings, 
                  size: 80, 
                  color: Colors.blue
                ),
                SizedBox(height: 20),
                Text(
                  'Dobrodošli, ${authProvider.username ?? "Korisnik"}!',
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
