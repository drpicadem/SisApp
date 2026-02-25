import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/salon_provider.dart';
import '../models/salon.dart';
import '../widgets/entity_image.dart';
import 'notifications_screen.dart';
import 'appointments_screen.dart';
import 'my_reviews_screen.dart';

class CustomerHomeScreen extends StatefulWidget {
  @override
  _CustomerHomeScreenState createState() => _CustomerHomeScreenState();
}

class _CustomerHomeScreenState extends State<CustomerHomeScreen> {
  int _currentIndex = 0;
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
      body: IndexedStack(
        index: _currentIndex,
        children: [
          _buildHomeTab(),
          AppointmentsScreen(),
          _buildProfileTab(),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        onTap: (index) {
          setState(() {
            _currentIndex = index;
          });
        },
        selectedItemColor: Color(0xFF7B5EA7),
        unselectedItemColor: Colors.grey,
        items: [
          BottomNavigationBarItem(
            icon: Icon(Icons.home),
            label: 'Početna',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.receipt_long),
            label: 'Narudžbe',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person),
            label: 'Profil',
          ),
        ],
      ),
    );
  }

  Widget _buildHomeTab() {
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Icon(Icons.content_cut, color: Color(0xFF7B5EA7)),
            SizedBox(width: 8),
            Text('ŠišApp', style: TextStyle(fontWeight: FontWeight.bold)),
          ],
        ),
        centerTitle: false,
        automaticallyImplyLeading: false,
        actions: [
          IconButton(
            icon: Icon(Icons.notifications_outlined),
            onPressed: () {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => NotificationsScreen()),
              );
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
              Padding(
                padding: EdgeInsets.symmetric(horizontal: 16),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text('Sve', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                ),
              ),
              SizedBox(height: 8),
              Expanded(
                child: salons.isEmpty
                    ? Center(child: Text('Nema dostupnih salona.'))
                    : ListView.builder(
                        padding: EdgeInsets.symmetric(horizontal: 16.0),
                        itemCount: salons.length,
                        itemBuilder: (context, index) {
                          return _buildSalonListItem(salons[index]);
                        },
                      ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildProfileTab() {
    final authProvider = Provider.of<AuthProvider>(context);

    return Scaffold(
      appBar: AppBar(
        title: Text('Profil'),
        centerTitle: true,
        automaticallyImplyLeading: false,
      ),
      body: ListView(
        padding: EdgeInsets.all(16),
        children: [
          SizedBox(height: 24),
          CircleAvatar(
            radius: 50,
            backgroundColor: Color(0xFFE8DFF0),
            child: Icon(Icons.person, size: 50, color: Color(0xFF7B5EA7)),
          ),
          SizedBox(height: 16),
          Text(
            authProvider.username ?? 'Korisnik',
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
          ),
          if (authProvider.email != null)
            Padding(
              padding: EdgeInsets.only(top: 4),
              child: Text(
                authProvider.email!,
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 14, color: Colors.grey[600]),
              ),
            ),
          SizedBox(height: 8),
          Center(
            child: Container(
              padding: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
              decoration: BoxDecoration(
                color: Color(0xFFE8DFF0),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Text(
                authProvider.role ?? 'Korisnik',
                style: TextStyle(color: Color(0xFF7B5EA7), fontWeight: FontWeight.w500),
              ),
            ),
          ),
          SizedBox(height: 32),
          Divider(),
          ListTile(
            leading: Icon(Icons.notifications_outlined),
            title: Text('Obavještenja'),
            trailing: Icon(Icons.chevron_right),
            onTap: () {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => NotificationsScreen()),
              );
            },
          ),
          Divider(),
          ListTile(
            leading: Icon(Icons.add_circle_outline),
            title: Text('Rezervišite termin'),
            trailing: Icon(Icons.chevron_right),
            onTap: () {
              Navigator.pushNamed(context, '/booking');
            },
          ),
          Divider(),
          ListTile(
            leading: Icon(Icons.rate_review_outlined),
            title: Text('Moje Recenzije'),
            trailing: Icon(Icons.chevron_right),
            onTap: () {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (context) => MyReviewsScreen()),
              );
            },
          ),
          Divider(),
          SizedBox(height: 24),
          ElevatedButton.icon(
            icon: Icon(Icons.logout),
            label: Text('Odjavi se'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.red.shade50,
              foregroundColor: Colors.red,
              padding: EdgeInsets.symmetric(vertical: 12),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
            ),
            onPressed: () {
              context.read<AuthProvider>().logout();
              Navigator.pushReplacementNamed(context, '/login');
            },
          ),
        ],
      ),
    );
  }

  Widget _buildSearchBar() {
    return Padding(
      padding: EdgeInsets.all(16.0),
      child: TextField(
        controller: _searchController,
        decoration: InputDecoration(
          hintText: 'Pretraži salon ili uslugu...',
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

  /// Salon list item matching the mockup: image on left, name + city on right, stars
  Widget _buildSalonListItem(Salon salon) {
    final token = context.read<AuthProvider>().tokenResponse?.token ?? '';
    
    return Card(
      elevation: 2,
      margin: EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () {
          Navigator.pushNamed(context, '/salon-details', arguments: salon);
        },
        child: Padding(
          padding: EdgeInsets.all(12),
          child: Row(
            children: [
              // Salon image (square thumbnail)
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: SizedBox(
                  width: 80,
                  height: 80,
                  child: EntityImage(
                    entityType: 'Salon',
                    entityId: salon.id,
                    token: token,
                    width: 80,
                    height: 80,
                    placeholderIcon: Icons.store,
                    placeholderIconSize: 32,
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
              ),
              SizedBox(width: 16),
              // Salon info
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      salon.name.toUpperCase(),
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 0.5,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      salon.city,
                      style: TextStyle(color: Colors.grey[600], fontSize: 14),
                    ),
                    SizedBox(height: 8),
                    Row(
                      children: List.generate(5, (i) {
                        return Icon(
                          i < salon.rating.round() ? Icons.star : Icons.star_border,
                          color: Colors.amber,
                          size: 18,
                        );
                      }),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
