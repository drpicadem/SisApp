import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/salon_amenity_provider.dart';
import '../../providers/salon_provider.dart';
import '../../providers/auth_provider.dart';
import '../../providers/barber_provider.dart';
import '../../models/salon_amenity.dart';
import '../../models/salon.dart';
import 'salon_amenity_edit_screen.dart';

class SalonAmenityListScreen extends StatefulWidget {
  @override
  _SalonAmenityListScreenState createState() => _SalonAmenityListScreenState();
}

class _SalonAmenityListScreenState extends State<SalonAmenityListScreen> {
  Salon? _selectedSalon;
  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      final authProvider = context.read<AuthProvider>();

      if (authProvider.isBarber) {
        final barberProvider = context.read<BarberProvider>();
        await barberProvider.loadMyBarberProfile();
        if (barberProvider.myBarberProfile != null) {
          final salonId = barberProvider.myBarberProfile!.salonId;
          setState(() {
            _selectedSalon = Salon(
              id: salonId,
              name: 'Moj Salon',
              cityId: 0,
              address: '',
              city: '',
              phone: '',
              postalCode: '',
            );
          });
          _loadAmenities();
        }
      } else {
        context.read<SalonProvider>().loadSalons();
      }
    });
  }

  void _loadAmenities() {
    if (_selectedSalon != null) {
      context.read<SalonAmenityProvider>().loadAmenities(
            refresh: true,
            salonId: _selectedSalon!.id,
            name: _searchController.text,
          );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Pogodnosti Salona'),
      ),
      body: Column(
        children: [
          Consumer<AuthProvider>(
            builder: (context, authProvider, _) {
              if (authProvider.isBarber) return SizedBox.shrink();

              return Padding(
                padding: const EdgeInsets.all(16.0),
                child: Consumer<SalonProvider>(
                  builder: (context, salonProvider, child) {
                    if (salonProvider.isLoading) return CircularProgressIndicator();

                    return DropdownButtonFormField<Salon>(
                      value: _selectedSalon,
                      decoration: InputDecoration(
                        labelText: 'Odaberite salon',
                        border: OutlineInputBorder(),
                        prefixIcon: Icon(Icons.store),
                      ),
                      items: salonProvider.salons.map((salon) {
                        return DropdownMenuItem(
                          value: salon,
                          child: Text(salon.name),
                        );
                      }).toList(),
                      onChanged: (Salon? newValue) {
                        setState(() { _selectedSalon = newValue; });
                        _loadAmenities();
                      },
                    );
                  },
                ),
              );
            }
          ),

          if (_selectedSalon != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0),
              child: TextField(
                controller: _searchController,
                decoration: InputDecoration(
                  labelText: 'Pretraga po nazivu',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.search),
                  suffixIcon: IconButton(
                    icon: Icon(Icons.clear),
                    onPressed: () { _searchController.clear(); _loadAmenities(); },
                  ),
                ),
                onSubmitted: (_) => _loadAmenities(),
              ),
            ),

          Expanded(
            child: Consumer<SalonAmenityProvider>(
              builder: (context, provider, child) {
                if (_selectedSalon == null) {
                    return Center(child: Text('Odaberite salon za prikaz pogodnosti.'));
                }
                if (provider.isLoading && provider.amenities.isEmpty) {
                  return Center(child: CircularProgressIndicator());
                }

                if (provider.amenities.isEmpty) {
                  return Center(child: Text('Nema dodanih pogodnosti.'));
                }

                return ListView.builder(
                  itemCount: provider.amenities.length + ((provider.hasMore || provider.isLoadingMore) ? 1 : 0),
                  itemBuilder: (context, index) {
                    if (index == provider.amenities.length) {
                      return Padding(
                        padding: const EdgeInsets.symmetric(vertical: 8),
                        child: Center(
                          child: provider.isLoadingMore
                              ? const CircularProgressIndicator()
                              : OutlinedButton(
                                  onPressed: () {
                                    if (_selectedSalon == null) return;
                                    context.read<SalonAmenityProvider>().loadAmenities(
                                          refresh: false,
                                          salonId: _selectedSalon!.id,
                                          name: _searchController.text,
                                        );
                                  },
                                  child: const Text('Učitaj još'),
                                ),
                        ),
                      );
                    }
                    final amenity = provider.amenities[index];
                    return Card(
                      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      child: ListTile(
                        isThreeLine: amenity.description != null && amenity.description!.trim().isNotEmpty,
                        leading: CircleAvatar(
                          child: Icon(Icons.spa),
                          backgroundColor: Colors.green.shade100,
                        ),
                        title: Text(amenity.name),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            if (amenity.description != null && amenity.description!.trim().isNotEmpty)
                              Padding(
                                padding: EdgeInsets.only(bottom: 4),
                                child: Text(
                                  amenity.description!.trim(),
                                  maxLines: 3,
                                  overflow: TextOverflow.ellipsis,
                                  style: TextStyle(fontSize: 13, color: Colors.grey[800]),
                                ),
                              ),
                            Text(
                              amenity.isAvailable ? 'Dostupno' : 'Nedostupno',
                              style: TextStyle(color: amenity.isAvailable ? Colors.green : Colors.red),
                            ),
                          ],
                        ),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              icon: Icon(Icons.edit, color: Colors.blue),
                              onPressed: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) => SalonAmenityEditScreen(amenity: amenity, salonId: _selectedSalon!.id),
                                  ),
                                ).then((_) => _loadAmenities());
                              },
                            ),
                            IconButton(
                              icon: Icon(Icons.delete, color: Colors.red),
                              onPressed: () => _confirmDelete(context, amenity),
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: _selectedSalon != null ? FloatingActionButton(
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => SalonAmenityEditScreen(salonId: _selectedSalon!.id),
            ),
          ).then((_) => _loadAmenities());
        },
        child: Icon(Icons.add),
        tooltip: 'Dodaj Novu Pogodnost',
      ) : null,
    );
  }

  void _confirmDelete(BuildContext context, SalonAmenity amenity) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Obriši Pogodnost'),
        content: Text('Da li ste sigurni da želite obrisati "${amenity.name}"?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Odustani'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () async {
              Navigator.pop(ctx);
              final success = await context.read<SalonAmenityProvider>().deleteAmenity(amenity.id);
              if (mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(
                      success
                          ? 'Pogodnost obrisana.'
                          : 'Brisanje pogodnosti nije uspjelo. Pokušajte ponovo.',
                    ),
                  ),
                );
              }
            },
            child: Text('Obriši'),
          ),
        ],
      ),
    );
  }
}
