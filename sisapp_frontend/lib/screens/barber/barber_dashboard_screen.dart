import 'package:flutter/material.dart';

class BarberDashboardScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.white,
      child: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 24.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.cut, size: 40, color: Colors.black),
                  SizedBox(width: 12),
                  Text(
                    'ŠIŠAPP',
                    style: TextStyle(
                      fontSize: 28,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 1.2,
                      color: Colors.black,
                      fontFamily: 'Serif',
                    ),
                  ),
                ],
              ),
              SizedBox(height: 36),

              _buildMenuButton(
                context,
                title: 'NARUDŽBE',
                icon: Icons.list_alt,
                route: '/appointments',
              ),
              SizedBox(height: 16),

              _buildMenuButton(
                context,
                title: 'USLUGE',
                icon: Icons.design_services_outlined,
                route: '/services',
              ),
              SizedBox(height: 16),

              _buildMenuButton(
                context,
                title: 'RADNO VRIJEME',
                icon: Icons.schedule,
                route: '/barber-schedule',
              ),
              SizedBox(height: 16),

              _buildMenuButton(
                context,
                title: 'RECENZIJE',
                icon: Icons.reviews_outlined,
                route: '/barber-reviews',
              ),
              SizedBox(height: 16),

              _buildMenuButton(
                context,
                title: 'RADNICI',
                icon: Icons.person_outline,
                route: '/barbers',
              ),
              SizedBox(height: 16),

              _buildMenuButton(
                context,
                title: 'POSTAVKE',
                icon: Icons.settings_outlined,
                route: '/edit_salon',
              ),
              SizedBox(height: 16),

              _buildMenuButton(
                context,
                title: 'MOJ PROFIL',
                icon: Icons.person_outline,
                route: '/edit-profile',
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildMenuButton(
    BuildContext context, {
    required String title,
    required IconData icon,
    required String route,
  }) {
    return InkWell(
      onTap: () => Navigator.of(context).pushNamed(route),
      borderRadius: BorderRadius.circular(12),
      child: Container(
        height: 70,
        decoration: BoxDecoration(
          color: Colors.grey[300],
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24.0),
              child: Icon(
                icon,
                size: 32,
                color: Colors.black,
              ),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.only(right: 64.0),
                child: Text(
                  title,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.0,
                    color: Colors.black,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
