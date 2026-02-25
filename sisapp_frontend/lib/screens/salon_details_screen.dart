import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/salon.dart';
import '../models/service.dart';
import '../models/barber.dart';
import '../providers/auth_provider.dart';
import '../providers/service_provider.dart';
import '../providers/barber_provider.dart';
import '../widgets/entity_image.dart';

class SalonDetailsScreen extends StatefulWidget {
  @override
  _SalonDetailsScreenState createState() => _SalonDetailsScreenState();
}

class _SalonDetailsScreenState extends State<SalonDetailsScreen> {
  bool _isInit = true;

  @override
  void didChangeDependencies() {
    if (_isInit) {
      final salon = ModalRoute.of(context)!.settings.arguments as Salon;
      Future.microtask(() {
        Provider.of<ServiceProvider>(context, listen: false).loadServices(salon.id);
        Provider.of<BarberProvider>(context, listen: false).loadBarbers(salon.id);
      });
      _isInit = false;
    }
    super.didChangeDependencies();
  }

  @override
  Widget build(BuildContext context) {
    final salon = ModalRoute.of(context)!.settings.arguments as Salon;
    final token = context.read<AuthProvider>().tokenResponse?.token ?? '';

    return Scaffold(
      appBar: AppBar(
        title: Text('Pregled Salona'),
        actions: [
          IconButton(
            icon: Icon(Icons.favorite_border),
            onPressed: () {},
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Hero salon image
            Stack(
              children: [
                EntityImage(
                  entityType: 'Salon',
                  entityId: salon.id,
                  token: token,
                  height: 220,
                  width: double.infinity,
                  placeholderIcon: Icons.store,
                  placeholderIconSize: 80,
                ),
                // Salon logo/name overlay
                Positioned(
                  bottom: -30,
                  left: 16,
                  child: Container(
                    width: 70,
                    height: 70,
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(12),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black26,
                          blurRadius: 6,
                          offset: Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Center(
                      child: Icon(Icons.content_cut, size: 32, color: Color(0xFF7B5EA7)),
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 40),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    salon.name.toUpperCase(),
                    style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold, letterSpacing: 0.5),
                  ),
                  SizedBox(height: 4),
                  Row(
                    children: [
                      Icon(Icons.location_on, size: 16, color: Colors.grey),
                      SizedBox(width: 4),
                      Text('${salon.address}, ${salon.city}', style: TextStyle(color: Colors.grey[600])),
                    ],
                  ),
                  if (salon.phone.isNotEmpty) ...[
                    SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(Icons.phone, size: 16, color: Colors.grey),
                        SizedBox(width: 4),
                        Text(salon.phone, style: TextStyle(color: Colors.grey[600])),
                      ],
                    ),
                  ],
                  SizedBox(height: 20),

                  // Barbers section - "Uposlenici"
                  Text('Uposlenici', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(height: 12),
                  _buildBarberList(token),

                  SizedBox(height: 20),

                  // Services section
                  Text('Cjenovnik usluga', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(height: 8),
                  _buildServiceList(),
                ],
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: Padding(
        padding: EdgeInsets.all(16.0),
        child: ElevatedButton(
          child: Text('ZAKAŽI TERMIN', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
          style: ElevatedButton.styleFrom(
            backgroundColor: Color(0xFF7B5EA7),
            foregroundColor: Colors.white,
            padding: EdgeInsets.symmetric(vertical: 16),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
          onPressed: () {
            Navigator.pushNamed(
              context,
              '/booking',
              arguments: {'salon': salon},
            );
          },
        ),
      ),
    );
  }

  Widget _buildServiceList() {
    return Consumer<ServiceProvider>(
      builder: (context, provider, _) {
        if (provider.isLoading) return Center(child: CircularProgressIndicator());
        if (provider.services.isEmpty) return Text('Nema dostupnih usluga.');

        return Column(
          children: provider.services.map((service) => Container(
            padding: EdgeInsets.symmetric(vertical: 8, horizontal: 4),
            decoration: BoxDecoration(
              border: Border(bottom: BorderSide(color: Colors.grey.shade200)),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    '${service.name}(${service.durationMinutes} min)',
                    style: TextStyle(fontSize: 14),
                  ),
                ),
                Text(
                  '${service.price.toStringAsFixed(0)} KM',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                ),
              ],
            ),
          )).toList(),
        );
      },
    );
  }

  Widget _buildBarberList(String token) {
    return Consumer<BarberProvider>(
      builder: (context, provider, _) {
        if (provider.isLoading) return Center(child: CircularProgressIndicator());
        if (provider.barbers.isEmpty) return Text('Nema dostupnih frizera.');

        return SizedBox(
          height: 110,
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            itemCount: provider.barbers.length,
            itemBuilder: (context, index) {
              final barber = provider.barbers[index];
              return Container(
                width: 90,
                margin: EdgeInsets.only(right: 16),
                child: Column(
                  children: [
                    // Barber photo
                    EntityImage(
                      entityType: 'Barber',
                      entityId: barber.id,
                      token: token,
                      isCircular: true,
                      circularRadius: 32,
                      placeholderIcon: Icons.person,
                      placeholderIconSize: 28,
                    ),
                    SizedBox(height: 8),
                    Text(
                      barber.firstName,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 13, fontWeight: FontWeight.w500),
                    ),
                  ],
                ),
              );
            },
          ),
        );
      },
    );
  }
}
