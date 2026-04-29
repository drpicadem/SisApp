import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/salon.dart';
import '../providers/auth_provider.dart';
import '../providers/service_provider.dart';
import '../providers/barber_provider.dart';
import '../providers/salon_amenity_provider.dart';
import '../providers/salon_provider.dart';
import '../widgets/entity_image.dart';

class SalonDetailsScreen extends StatefulWidget {
  @override
  _SalonDetailsScreenState createState() => _SalonDetailsScreenState();
}

class _SalonDetailsScreenState extends State<SalonDetailsScreen> {
  bool _isInit = true;
  bool _isTogglingFavorite = false;

  @override
  void didChangeDependencies() {
    if (_isInit) {
      final salon = ModalRoute.of(context)!.settings.arguments as Salon;
      Future.microtask(() {
        Provider.of<ServiceProvider>(context, listen: false).loadServices(salon.id);
        Provider.of<BarberProvider>(context, listen: false).loadBarbers(salon.id);
        Provider.of<SalonAmenityProvider>(context, listen: false).loadAmenities(salonId: salon.id);
      });
      _isInit = false;
    }
    super.didChangeDependencies();
  }

  @override
  Widget build(BuildContext context) {
    final salon = ModalRoute.of(context)!.settings.arguments as Salon;
    final token = context.read<AuthProvider>().tokenResponse?.token ?? '';
    final salonProvider = context.watch<SalonProvider>();
    final isFavorite = salonProvider.favoriteSalonIds.contains(salon.id);

    return Scaffold(
      appBar: AppBar(
        title: Text('Pregled Salona'),
        actions: [
          IconButton(
            icon: Icon(
              isFavorite ? Icons.favorite : Icons.favorite_border,
              color: isFavorite ? Colors.red : null,
            ),
            onPressed: _isTogglingFavorite
                ? null
                : () async {
                    setState(() => _isTogglingFavorite = true);
                    final before = salonProvider.favoriteSalonIds.contains(salon.id);
                    await salonProvider.toggleFavorite(salon.id);
                    if (!mounted) return;
                    final after = salonProvider.favoriteSalonIds.contains(salon.id);
                    setState(() => _isTogglingFavorite = false);
                    if (before == after) {
                      final errorMessage = salonProvider.lastError?.trim();
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text(
                          (errorMessage != null && errorMessage.isNotEmpty)
                              ? errorMessage
                              : 'Greška pri ažuriranju omiljenih salona.',
                        )),
                      );
                    }
                  },
          ),
        ],
      ),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
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


                  Text('Uposlenici', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(height: 12),
                  _buildBarberList(token),

                  SizedBox(height: 20),

                  Text('Pogodnosti', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(height: 8),
                  _buildAmenitiesList(),

                  SizedBox(height: 20),


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
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${service.name} (${service.durationMinutes} min)',
                        style: TextStyle(fontSize: 14),
                      ),
                      if (service.categoryName != null && service.categoryName!.isNotEmpty) ...[
                        SizedBox(height: 6),
                        Container(
                          padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                          decoration: BoxDecoration(
                            color: Colors.blue.shade50,
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: Text(
                            service.categoryName!,
                            style: TextStyle(fontSize: 11, color: Colors.blue.shade800, fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                      if (service.categoryDescription != null && service.categoryDescription!.trim().isNotEmpty) ...[
                        SizedBox(height: 4),
                        Text(
                          service.categoryDescription!.trim(),
                          style: TextStyle(fontSize: 12, color: Colors.grey[700], height: 1.25),
                          maxLines: 4,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ],
                  ),
                ),
                Padding(
                  padding: EdgeInsets.only(left: 8, top: 2),
                  child: Text(
                    '${service.price.toStringAsFixed(0)} KM',
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                  ),
                ),
              ],
            ),
          )).toList(),
        );
      },
    );
  }

  Widget _buildAmenitiesList() {
    return Consumer<SalonAmenityProvider>(
      builder: (context, provider, _) {
        if (provider.isLoading) {
          return Center(child: CircularProgressIndicator());
        }

        final available = provider.amenities.where((a) => a.isAvailable).toList();
        if (available.isEmpty) {
          return Text('Salon nema unesene pogodnosti.');
        }

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: available.map((a) {
            final desc = a.description?.trim();
            return Padding(
              padding: EdgeInsets.only(bottom: 10),
              child: Container(
                width: double.infinity,
                padding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.green.shade100),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      a.name,
                      style: TextStyle(fontSize: 13, color: Colors.green.shade900, fontWeight: FontWeight.w600),
                    ),
                    if (desc != null && desc.isNotEmpty) ...[
                      SizedBox(height: 4),
                      Text(
                        desc,
                        style: TextStyle(fontSize: 12, color: Colors.grey[800], height: 1.25),
                      ),
                    ],
                  ],
                ),
              ),
            );
          }).toList(),
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
