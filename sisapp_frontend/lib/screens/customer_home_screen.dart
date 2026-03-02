import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/salon_provider.dart';
import '../models/salon.dart';
import '../widgets/entity_image.dart';
import 'package:geolocator/geolocator.dart';
import '../services/api_service.dart';
import 'notifications_screen.dart';
import 'appointments_screen.dart';
import 'my_reviews_screen.dart';
import 'favorites_screen.dart';

class CustomerHomeScreen extends StatefulWidget {
  @override
  _CustomerHomeScreenState createState() => _CustomerHomeScreenState();
}

class _CustomerHomeScreenState extends State<CustomerHomeScreen> {
  int _currentIndex = 0;
  String _searchQuery = '';
  final TextEditingController _searchController = TextEditingController();
  List<Map<String, dynamic>> _recommendations = [];
  bool _loadingRecommendations = true;
  bool _useLocation = false;
  Position? _currentPosition;

  Future<void> _getCurrentLocation() async {
    bool serviceEnabled;
    LocationPermission permission;

    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lokacijske usluge su isključene.')));
      return;
    }

    permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lokacijske dozvole su odbijene.')));
        return;
      }
    }
    
    if (permission == LocationPermission.deniedForever) {
      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lokacijske dozvole su trajno odbijene.')));
      return;
    } 

    try {
      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.low,
        timeLimit: Duration(seconds: 15),
      );
      if (mounted) {
        setState(() {
          _currentPosition = position;
          _useLocation = true;
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Greška pri dohvatanju lokacije (Emulator crash). Prvo podesite lokaciju u postavkama emulatora.')),
        );
      }
    }
  }

  @override
  void initState() {
    super.initState();
    Future.microtask(() {
      context.read<SalonProvider>().loadSalons();
      _loadRecommendations();
    });
  }

  Future<void> _loadRecommendations() async {
    final token = context.read<AuthProvider>().tokenResponse?.token;
    if (token == null) {
      setState(() => _loadingRecommendations = false);
      return;
    }
    try {
      final results = await ApiService().getRecommendations(token);
      if (mounted) {
        setState(() {
          _recommendations = results;
          _loadingRecommendations = false;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _loadingRecommendations = false);
    }
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
          FavoritesScreen(),
          AppointmentsScreen(),
          _buildProfileTab(),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        type: BottomNavigationBarType.fixed,
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
            icon: Icon(Icons.favorite),
            label: 'Favoriti',
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

          var salons = salonProvider.salons.where((salon) {
            final query = _searchQuery.toLowerCase();
            return query.isEmpty || 
                   salon.name.toLowerCase().contains(query) ||
                   salon.city.toLowerCase().contains(query) ||
                   (salon.services != null && salon.services!.any((s) => s.toLowerCase().contains(query)));
          }).toList();

          if (_useLocation && _currentPosition != null) {
            salons.sort((a, b) {
              if (a.latitude == null || a.longitude == null) return 1;
              if (b.latitude == null || b.longitude == null) return -1;
              
              double distA = Geolocator.distanceBetween(
                _currentPosition!.latitude, _currentPosition!.longitude,
                a.latitude!, a.longitude!
              );
              double distB = Geolocator.distanceBetween(
                _currentPosition!.latitude, _currentPosition!.longitude,
                b.latitude!, b.longitude!
              );
              return distA.compareTo(distB);
            });
          }

          return ListView(
            children: [
              _buildSearchBar(),
              // Recommendations section
              if (!_loadingRecommendations && _recommendations.isNotEmpty) ...[
                Padding(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  child: Row(
                    children: [
                      Icon(Icons.auto_awesome, color: Color(0xFF7B5EA7), size: 20),
                      SizedBox(width: 8),
                      Text('Preporučeno za vas', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                    ],
                  ),
                ),
                SizedBox(height: 8),
                SizedBox(
                  height: 200,
                  child: ListView.builder(
                    scrollDirection: Axis.horizontal,
                    padding: EdgeInsets.symmetric(horizontal: 12),
                    itemCount: _recommendations.length,
                    itemBuilder: (context, index) {
                      return _buildRecommendationCard(_recommendations[index]);
                    },
                  ),
                ),
                SizedBox(height: 16),
              ],
              if (_loadingRecommendations)
                Padding(
                  padding: EdgeInsets.all(16),
                  child: Center(child: LinearProgressIndicator()),
                ),
              Padding(
                padding: EdgeInsets.symmetric(horizontal: 16),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text('Svi saloni', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                ),
              ),
              SizedBox(height: 8),
              if (salons.isEmpty)
                Padding(
                  padding: EdgeInsets.all(32),
                  child: Center(child: Text('Nema dostupnih salona.')),
                )
              else
                ...salons.map((salon) => Padding(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  child: _buildSalonListItem(salon),
                )).toList(),
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
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Pretraži salon, grad ili uslugu...',
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
          ),
          SizedBox(width: 8),
          Container(
            decoration: BoxDecoration(
              color: _useLocation ? Color(0xFFE8DFF0) : Colors.grey[200],
              shape: BoxShape.circle,
            ),
            child: IconButton(
              icon: Icon(
                _useLocation ? Icons.location_on : Icons.location_off, 
                color: _useLocation ? Color(0xFF7B5EA7) : Colors.grey,
              ),
              onPressed: () {
                if (_useLocation) {
                  setState(() => _useLocation = false);
                } else {
                  _getCurrentLocation();
                }
              },
              tooltip: 'Filtriraj po mojoj lokaciji',
            ),
          ),
        ],
      ),
    );
  }


  Widget _buildRecommendationCard(Map<String, dynamic> rec) {
    final salonRating = (rec['salonRating'] as num?)?.toDouble() ?? 0.0;

    return GestureDetector(
      onTap: () {
        // Navigate to salon details
        final salon = Salon(
          id: rec['salonId'] ?? 0,
          name: rec['salonName'] ?? '',
          city: rec['salonCity'] ?? '',
          address: '',
          phone: '',
          postalCode: '',
          country: '',
          employeeCount: 0,
          rating: salonRating,
        );
        Navigator.pushNamed(context, '/salon-details', arguments: salon);
      },
      child: Container(
        width: 220,
        margin: EdgeInsets.symmetric(horizontal: 4),
        child: Card(
          elevation: 3,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          child: Padding(
            padding: EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Service name
                Text(
                  rec['serviceName'] ?? '',
                  style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                SizedBox(height: 4),
                // Salon info
                Row(
                  children: [
                    Icon(Icons.store, size: 14, color: Colors.grey[600]),
                    SizedBox(width: 4),
                    Expanded(
                      child: Text(
                        rec['salonName'] ?? '',
                        style: TextStyle(fontSize: 13, color: Colors.grey[700]),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 2),
                Row(
                  children: [
                    Icon(Icons.location_on, size: 14, color: Colors.grey[500]),
                    SizedBox(width: 4),
                    Text(
                      rec['salonCity'] ?? '',
                      style: TextStyle(fontSize: 12, color: Colors.grey[500]),
                    ),
                  ],
                ),
                SizedBox(height: 8),
                // Price and duration
                Row(
                  children: [
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: Colors.green.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        '${(rec['price'] as num?)?.toStringAsFixed(2) ?? '0.00'} KM',
                        style: TextStyle(fontSize: 12, color: Colors.green[700], fontWeight: FontWeight.w600),
                      ),
                    ),
                    SizedBox(width: 6),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: Colors.blue.withOpacity(0.1),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        '${rec['durationMinutes'] ?? 0} min',
                        style: TextStyle(fontSize: 12, color: Colors.blue[700], fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 6),
                // Stars
                Row(
                  children: List.generate(5, (i) => Icon(
                    i < salonRating.round() ? Icons.star : Icons.star_border,
                    color: Colors.amber,
                    size: 14,
                  )),
                ),
                Spacer(),
                // Reason
                Text(
                  rec['reason'] ?? '',
                  style: TextStyle(fontSize: 11, color: Color(0xFF7B5EA7), fontStyle: FontStyle.italic),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSalonListItem(Salon salon) {
    final token = context.read<AuthProvider>().tokenResponse?.token ?? '';
    
    String distanceText = '';
    if (_useLocation && _currentPosition != null && salon.latitude != null && salon.longitude != null) {
      double distInMeters = Geolocator.distanceBetween(
        _currentPosition!.latitude, _currentPosition!.longitude,
        salon.latitude!, salon.longitude!
      );
      if (distInMeters < 1000) {
        distanceText = '${distInMeters.round()} m';
      } else {
        distanceText = '${(distInMeters / 1000).toStringAsFixed(1)} km';
      }
    }
    
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
                    if (distanceText.isNotEmpty) ...[
                      SizedBox(height: 2),
                      Row(
                        children: [
                          Icon(Icons.location_on, size: 14, color: Color(0xFF7B5EA7)),
                          SizedBox(width: 4),
                          Text(distanceText, style: TextStyle(color: Color(0xFF7B5EA7), fontSize: 13, fontWeight: FontWeight.bold)),
                        ],
                      ),
                    ],
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
              Consumer<SalonProvider>(
                builder: (context, provider, child) {
                  final isFavorite = provider.favoriteSalonIds.contains(salon.id);
                  return IconButton(
                    icon: Icon(
                      isFavorite ? Icons.favorite : Icons.favorite_border,
                      color: isFavorite ? Colors.red : Colors.grey,
                    ),
                    onPressed: () {
                      provider.toggleFavorite(salon.id);
                    },
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}
