import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'providers/auth_provider.dart';
import 'providers/service_provider.dart';
import 'providers/barber_provider.dart';
import 'providers/salon_provider.dart';
import 'providers/user_provider.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen.dart';
import 'screens/services_screen.dart';
import 'screens/barbers_screen.dart';
import 'screens/salons_screen.dart';
import 'screens/users_screen.dart';
import 'screens/reports_screen.dart';

void main() {
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProxyProvider<AuthProvider, ServiceProvider>(
          create: (_) => ServiceProvider(null), // Initial create
          update: (_, auth, previous) => ServiceProvider(auth), 
        ),
        ChangeNotifierProxyProvider<AuthProvider, BarberProvider>(
          create: (_) => BarberProvider(null),
          update: (_, auth, previous) => BarberProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, SalonProvider>(
          create: (_) => SalonProvider(null),
          update: (_, auth, previous) => SalonProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, UserProvider>(
          create: (_) => UserProvider(null),
          update: (_, auth, previous) => UserProvider(auth),
        ),
      ],
      child: MaterialApp(
        title: 'ŠišApp',
        theme: ThemeData(
          primarySwatch: Colors.blue,
        ),
        home: LoginScreen(),
        routes: {
          '/login': (context) => LoginScreen(),
          '/home': (context) => HomeScreen(),
          '/services': (context) => ServicesScreen(),
          '/barbers': (context) => BarbersScreen(),
          '/barbers': (context) => BarbersScreen(),
          '/salons': (context) => SalonsScreen(),
          '/users': (context) => UsersScreen(),
          '/reports': (context) => ReportsScreen(),
        },
      ),
    );
  }
}
