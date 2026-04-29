import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/salon_provider.dart';
import '../models/salon.dart';
import '../widgets/entity_image.dart';
import '../providers/auth_provider.dart';
import 'package:geolocator/geolocator.dart';

class FavoritesScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Omiljeni Saloni'),
        centerTitle: true,
        automaticallyImplyLeading: false,
      ),
      body: Consumer<SalonProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return Center(child: CircularProgressIndicator());
          }

          final favIds = provider.favoriteSalonIds;
          final favoriteSalons = provider.salons.where((s) => favIds.contains(s.id)).toList();

          if (favoriteSalons.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.favorite_border, size: 60, color: Colors.grey[400]),
                  SizedBox(height: 16),
                  Text('Nemate omiljenih salona', style: TextStyle(color: Colors.grey[600], fontSize: 16)),
                ],
              ),
            );
          }

          return ListView.builder(
            padding: EdgeInsets.all(16),
            itemCount: favoriteSalons.length,
            itemBuilder: (context, index) {
              return _buildFavoriteItem(context, favoriteSalons[index]);
            },
          );
        },
      ),
    );
  }

  Widget _buildFavoriteItem(BuildContext context, Salon salon) {
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
              IconButton(
                icon: Icon(Icons.favorite, color: Colors.red),
                onPressed: () {
                  context.read<SalonProvider>().toggleFavorite(salon.id);
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}
