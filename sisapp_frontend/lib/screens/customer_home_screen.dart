import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/salon_provider.dart';
import '../models/salon.dart';

class CustomerHomeScreen extends StatefulWidget {
  @override
  _CustomerHomeScreenState createState() => _CustomerHomeScreenState();
}

class _CustomerHomeScreenState extends State<CustomerHomeScreen> {
  String _searchQuery = '';
  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => context.read<SalonProvider>().loadSalons());
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('ŠišApp'),
        centerTitle: true,
        actions: [
          IconButton(
            icon: Icon(Icons.exit_to_app),
            onPressed: () {
              context.read<AuthProvider>().logout();
              Navigator.pushReplacementNamed(context, '/login');
            },
          ),
        ],
      ),
      body: Consumer<SalonProvider>(
        builder: (context, salonProvider, child) {
          if (salonProvider.isLoading) {
            return Center(child: CircularProgressIndicator());
          }

          final salons = salonProvider.salons.where((salon) {
            final query = _searchQuery.toLowerCase();
            return salon.name.toLowerCase().contains(query) ||
                   salon.city.toLowerCase().contains(query);
          }).toList();

          return Column(
            children: [
              _buildSearchBar(),
              Expanded(
                child: salons.isEmpty
                    ? Center(child: Text('Nema dostupnih salona.'))
                    : ListView.builder(
                        padding: EdgeInsets.all(16.0),
                        itemCount: salons.length,
                        itemBuilder: (context, index) {
                          return _buildSalonCard(salons[index]);
                        },
                      ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildSearchBar() {
    return Padding(
      padding: EdgeInsets.all(16.0),
      child: TextField(
        controller: _searchController,
        decoration: InputDecoration(
          hintText: 'Pretraži salone ili gradove...',
          prefixIcon: Icon(Icons.search),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(30.0),
            borderSide: BorderSide.none,
          ),
          filled: true,
          fillColor: Colors.grey[200],
        ),
        onChanged: (value) {
          setState(() {
            _searchQuery = value;
          });
        },
      ),
    );
  }

  Widget _buildSalonCard(Salon salon) {
    return Card(
      elevation: 4.0,
      margin: EdgeInsets.only(bottom: 16.0),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16.0)),
      child: InkWell(
        onTap: () {
           // Navigate to Salon Details
           Navigator.pushNamed(context, '/salon-details', arguments: salon);
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Placeholder Image
            Container(
              height: 150,
              decoration: BoxDecoration(
                color: Colors.grey[300],
                borderRadius: BorderRadius.only(
                  topLeft: Radius.circular(16.0),
                  topRight: Radius.circular(16.0),
                ),
              ),
              child: Center(
                child: Icon(Icons.store, size: 50, color: Colors.grey[500]),
              ),
            ),
            Padding(
              padding: EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        salon.name,
                        style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                      ),
                      Row(
                        children: [
                          Icon(Icons.star, color: Colors.amber, size: 20),
                          SizedBox(width: 4),
                          Text(salon.rating.toStringAsFixed(1), style: TextStyle(fontWeight: FontWeight.bold)),
                        ],
                      ),
                    ],
                  ),
                  SizedBox(height: 8),
                  Row(
                    children: [
                      Icon(Icons.location_on, color: Colors.grey, size: 16),
                      SizedBox(width: 4),
                      Text('${salon.address}, ${salon.city}', style: TextStyle(color: Colors.grey[600])),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
