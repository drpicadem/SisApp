import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../models/salon.dart';
import '../models/service.dart';
import '../models/barber.dart';
import '../providers/service_provider.dart';
import '../providers/barber_provider.dart';

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

    return Scaffold(
      appBar: AppBar(title: Text(salon.name)),
      body: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header Image Placeholder
            Container(
              height: 200,
              width: double.infinity,
              color: Colors.grey[300],
              child: Icon(Icons.store, size: 80, color: Colors.grey[500]),
            ),
            Padding(
              padding: EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(salon.name, style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
                  SizedBox(height: 8),
                  Text('${salon.address}, ${salon.city}', style: TextStyle(fontSize: 16, color: Colors.grey[600])),
                  SizedBox(height: 16),
                  Text('Usluge', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(height: 8),
                  _buildServiceList(),
                  SizedBox(height: 16),
                  Text('Naš Tim', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(height: 8),
                  _buildBarberList(),
                ],
              ),
            ),
          ],
        ),
      ),
       bottomNavigationBar: Padding(
        padding: EdgeInsets.all(16.0),
        child: ElevatedButton(
          child: Text('REZERVIŠI TERMIN'),
          style: ElevatedButton.styleFrom(
            padding: EdgeInsets.symmetric(vertical: 16),
            textStyle: TextStyle(fontSize: 18),
          ),
          onPressed: () {
             Navigator.pushNamed(
               context, 
               '/booking', 
               arguments: {'salon': salon} // Pass salon to pre-select
             );
          },
        ),
      ),
    );
  }

  Widget _buildServiceList() {
    return Consumer<ServiceProvider>(
      builder: (context, provider, _) {
        if (provider.isLoading) return CircularProgressIndicator();
        if (provider.services.isEmpty) return Text('Nema dostupnih usluga.');
        
        return Column(
          children: provider.services.map((service) => ListTile(
            title: Text(service.name),
            trailing: Text('${service.price} KM', style: TextStyle(fontWeight: FontWeight.bold)),
            dense: true,
          )).toList(),
        );
      },
    );
  }

  Widget _buildBarberList() {
    return Consumer<BarberProvider>(
      builder: (context, provider, _) {
         if (provider.isLoading) return CircularProgressIndicator();
         if (provider.barbers.isEmpty) return Text('Nema dostupnih frizera.');

         return SizedBox(
           height: 120,
           child: ListView.builder(
             scrollDirection: Axis.horizontal,
             itemCount: provider.barbers.length,
             itemBuilder: (context, index) {
               final barber = provider.barbers[index];
               return Container(
                 width: 100,
                 margin: EdgeInsets.only(right: 16),
                 child: Column(
                   children: [
                     CircleAvatar(
                       radius: 30,
                       backgroundColor: Colors.blue[100],
                       child: Text(barber.firstName[0], style: TextStyle(fontSize: 24)),
                     ),
                     SizedBox(height: 8),
                     Text(barber.firstName, overflow: TextOverflow.ellipsis),
                     Text('4.8 ⭐', style: TextStyle(fontSize: 12, color: Colors.amber)),
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
