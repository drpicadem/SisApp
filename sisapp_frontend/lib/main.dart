import 'dart:async';
import 'package:flutter/material.dart';
import 'package:app_links/app_links.dart';
import 'package:provider/provider.dart';
import 'providers/auth_provider.dart';
import 'providers/service_provider.dart';
import 'providers/barber_provider.dart';
import 'providers/salon_provider.dart';
import 'providers/user_provider.dart';
import 'providers/booking_provider.dart';
import 'features/payment/providers/payment_provider.dart';
import 'providers/appointment_provider.dart';
import 'providers/review_provider.dart';
import 'features/working_hours/providers/working_hours_provider.dart';
import 'providers/service_category_provider.dart';
import 'providers/salon_amenity_provider.dart';
import 'providers/notification_provider.dart';
import 'screens/login_screen.dart';
import 'screens/register_screen.dart';
import 'screens/home_screen.dart';
import 'screens/services_screen.dart';
import 'screens/barbers_screen.dart';
import 'screens/salons_screen.dart';
import 'screens/users_screen.dart';
import 'screens/reports_screen.dart';
import 'screens/customer_home_screen.dart';
import 'screens/booking_screen.dart';
import 'screens/salon_details_screen.dart';
import 'screens/appointments_screen.dart';
import 'screens/forgot_password_screen.dart';
import 'screens/reset_password_screen.dart';
import 'screens/barber/edit_salon_screen.dart';
import 'features/working_hours/screens/barber_schedule_screen.dart';
import 'features/reviews/screens/barber_reviews_screen.dart';
import 'screens/reference_tables/service_category_list_screen.dart';
import 'screens/reference_tables/salon_amenity_list_screen.dart';
import 'screens/admin_logs_screen.dart';
import 'screens/user/edit_profile_screen.dart';
import 'services/api_service.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(MyApp());
}

class MyApp extends StatefulWidget {
  @override
  State<MyApp> createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  final GlobalKey<NavigatorState> _navigatorKey = GlobalKey<NavigatorState>();
  StreamSubscription<Uri>? _deepLinkSub;
  AppLinks? _appLinks;

  @override
  void initState() {
    super.initState();
    _configureUnauthorizedHandling();
    _initDeepLinkHandling();
  }

  void _configureUnauthorizedHandling() {
    ApiService.onUnauthorized = () async {
      final navContext = _navigatorKey.currentContext;
      final navigator = _navigatorKey.currentState;
      if (navContext == null || navigator == null) return;

      final auth = Provider.of<AuthProvider>(navContext, listen: false);
      await auth.logout();

      navigator.pushNamedAndRemoveUntil('/login', (route) => false);
    };
  }

  Future<void> _initDeepLinkHandling() async {
    _appLinks = AppLinks();

    final initialUri = await _appLinks!.getInitialLink();
    _handleIncomingLink(initialUri);

    _deepLinkSub = _appLinks!.uriLinkStream.listen(_handleIncomingLink);
  }

  void _handleIncomingLink(Uri? uri) {
    if (uri == null) return;
    final navigator = _navigatorKey.currentState;
    if (navigator == null) return;

    if (uri.path.contains('reset-password')) {
      navigator.pushNamed(
        '/reset-password',
        arguments: {
          'email': uri.queryParameters['email'] ?? '',
          'token': uri.queryParameters['token'] ?? '',
        },
      );
    }
  }

  @override
  void dispose() {
    _deepLinkSub?.cancel();
    ApiService.onUnauthorized = null;
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProxyProvider<AuthProvider, ServiceProvider>(
          create: (_) => ServiceProvider(null),
          update: (_, auth, previous) => ServiceProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, BarberProvider>(
          create: (_) => BarberProvider(null),
          update: (_, auth, previous) {
            if (previous == null) {
              return BarberProvider(auth);
            }
            previous.updateAuthProvider(auth);
            return previous;
          },
        ),
        ChangeNotifierProxyProvider<AuthProvider, SalonProvider>(
          create: (_) => SalonProvider(null),
          update: (_, auth, previous) {
            if (previous == null) {
              return SalonProvider(auth);
            }
            previous.updateAuthProvider(auth);
            return previous;
          },
        ),
        ChangeNotifierProxyProvider<AuthProvider, UserProvider>(
          create: (_) => UserProvider(null),
          update: (_, auth, previous) => UserProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, BookingProvider>(
          create: (_) => BookingProvider(null),
          update: (_, auth, previous) => BookingProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, PaymentProvider>(
          create: (_) => PaymentProvider(null),
          update: (_, auth, previous) => PaymentProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, AppointmentProvider>(
          create: (_) => AppointmentProvider(null),
          update: (_, auth, previous) => AppointmentProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, ReviewProvider>(
          create: (_) => ReviewProvider(null),
          update: (_, auth, previous) => ReviewProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, WorkingHoursProvider>(
          create: (_) => WorkingHoursProvider(null),
          update: (_, auth, previous) => WorkingHoursProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, ServiceCategoryProvider>(
          create: (_) => ServiceCategoryProvider(null),
          update: (_, auth, previous) => ServiceCategoryProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, SalonAmenityProvider>(
          create: (_) => SalonAmenityProvider(null),
          update: (_, auth, previous) => SalonAmenityProvider(auth),
        ),
        ChangeNotifierProxyProvider<AuthProvider, NotificationProvider>(
          create: (_) => NotificationProvider(null),
          update: (_, auth, previous) {
            if (previous == null) {
              return NotificationProvider(auth);
            }
            previous.updateAuthProvider(auth);
            return previous;
          },
        ),
      ],
      child: MaterialApp(
        navigatorKey: _navigatorKey,
        title: 'ŠišApp',
        theme: ThemeData(
          primarySwatch: Colors.blue,
        ),
        home: LoginScreen(),
        routes: {
          '/login': (context) => LoginScreen(),
          '/register': (context) => RegisterScreen(),
          '/forgot-password': (context) => ForgotPasswordScreen(),
          '/reset-password': (context) => ResetPasswordScreen(),
          '/home': (context) => HomeScreen(),
          '/services': (context) => ServicesScreen(),
          '/barbers': (context) => BarbersScreen(),
          '/salons': (context) => SalonsScreen(),
          '/users': (context) => UsersScreen(),
          '/reports': (context) => ReportsScreen(),
          '/customer-home': (context) => CustomerHomeScreen(),
          '/booking': (context) => BookingScreen(),
          '/salon-details': (context) => SalonDetailsScreen(),
          '/appointments': (context) => AppointmentsScreen(),
          '/edit_salon': (context) => EditSalonScreen(),
          '/barber-schedule': (context) => BarberScheduleScreen(),
          '/barber-reviews': (context) => BarberReviewsScreen(),
          '/service-categories': (context) => ServiceCategoryListScreen(),
          '/salon-amenities': (context) => SalonAmenityListScreen(),
          '/admin-logs': (context) => AdminLogsScreen(),
          '/edit-profile': (context) => EditProfileScreen(),
        },
      ),
    );
  }
}
