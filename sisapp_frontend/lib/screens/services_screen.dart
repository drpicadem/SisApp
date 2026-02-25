import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/service_provider.dart';
import '../providers/salon_provider.dart';
import '../providers/auth_provider.dart';
import '../models/service.dart';
import '../models/salon.dart';

class ServicesScreen extends StatefulWidget {
  @override
  _ServicesScreenState createState() => _ServicesScreenState();
}

class _ServicesScreenState extends State<ServicesScreen> {
  Salon? _selectedSalon;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<SalonProvider>().loadSalons();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Upravljanje Uslugama'),
      ),
      body: Column(
        children: [
          // Salon selector
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Consumer<SalonProvider>(
              builder: (context, salonProvider, child) {
                if (salonProvider.isLoading) return LinearProgressIndicator();

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
                    setState(() {
                      _selectedSalon = newValue;
                    });
                    if (newValue != null) {
                      context.read<ServiceProvider>().loadServices(newValue.id);
                    }
                  },
                );
              },
            ),
          ),

          // Services list
          Expanded(
            child: Consumer<ServiceProvider>(
              builder: (context, provider, child) {
                if (_selectedSalon == null) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.content_cut, size: 64, color: Colors.grey[300]),
                        SizedBox(height: 16),
                        Text('Odaberite salon za pregled usluga',
                            style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                      ],
                    ),
                  );
                }

                if (provider.isLoading) {
                  return Center(child: CircularProgressIndicator());
                }

                if (provider.services.isEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.inbox, size: 64, color: Colors.grey[300]),
                        SizedBox(height: 16),
                        Text('Nema dodanih usluga.',
                            style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                      ],
                    ),
                  );
                }

                return ListView.builder(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  itemCount: provider.services.length,
                  itemBuilder: (context, index) {
                    final service = provider.services[index];
                    return _buildServiceCard(service);
                  },
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: _selectedSalon != null
          ? FloatingActionButton.extended(
              onPressed: () => _showAddServiceDialog(context),
              label: Text('Nova usluga'),
              icon: Icon(Icons.add),
              backgroundColor: Color(0xFFE0CFA9),
            )
          : null,
    );
  }

  Widget _buildServiceCard(Service service) {
    return Card(
      margin: EdgeInsets.only(bottom: 12),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          children: [
            // Icon
            Container(
              width: 50,
              height: 50,
              decoration: BoxDecoration(
                color: Color(0xFFE0CFA9).withOpacity(0.2),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(Icons.content_cut, color: Color(0xFFE0CFA9), size: 28),
            ),
            SizedBox(width: 16),
            // Info
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(service.name,
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                  SizedBox(height: 4),
                  if (service.description != null && service.description!.isNotEmpty)
                    Text(service.description!,
                        style: TextStyle(color: Colors.grey[600], fontSize: 13)),
                  SizedBox(height: 8),
                  Row(
                    children: [
                      _buildChip(Icons.access_time, '${service.durationMinutes} min', Colors.blue),
                      SizedBox(width: 8),
                      _buildChip(Icons.attach_money, '${service.price.toStringAsFixed(2)} KM', Colors.green),
                      if (service.isPopular) ...[
                        SizedBox(width: 8),
                        _buildChip(Icons.star, 'Popularna', Colors.orange),
                      ],
                    ],
                  ),
                ],
              ),
            ),
            // Actions
            Column(
              children: [
                IconButton(
                  icon: Icon(Icons.edit, color: Colors.blue),
                  tooltip: 'Uredi',
                  onPressed: () => _showEditServiceDialog(context, service),
                ),
                IconButton(
                  icon: Icon(Icons.delete, color: Colors.red),
                  tooltip: 'Obriši',
                  onPressed: () => _confirmDelete(context, service),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildChip(IconData icon, String label, Color color) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: color),
          SizedBox(width: 4),
          Text(label, style: TextStyle(fontSize: 12, color: color, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }

  void _confirmDelete(BuildContext context, Service service) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Obriši uslugu'),
        content: Text('Da li ste sigurni da želite obrisati "${service.name}"?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Odustani'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () async {
              Navigator.pop(ctx);
              final success = await context.read<ServiceProvider>().deleteService(service.id, _selectedSalon!.id);
              if (mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(content: Text(success ? 'Usluga obrisana.' : 'Greška pri brisanju.')),
                );
              }
            },
            child: Text('Obriši'),
          ),
        ],
      ),
    );
  }

  void _showAddServiceDialog(BuildContext context) {
    final nameController = TextEditingController();
    final priceController = TextEditingController();
    final durationController = TextEditingController();
    final descriptionController = TextEditingController();
    final formKey = GlobalKey<FormState>();

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text('Nova Usluga'),
          content: SingleChildScrollView(
            child: Form(
              key: formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: nameController,
                    decoration: InputDecoration(labelText: 'Naziv', prefixIcon: Icon(Icons.label)),
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: descriptionController,
                    decoration: InputDecoration(labelText: 'Opis (opciono)', prefixIcon: Icon(Icons.description)),
                    maxLines: 2,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: priceController,
                    decoration: InputDecoration(labelText: 'Cijena (KM)', prefixIcon: Icon(Icons.attach_money)),
                    keyboardType: TextInputType.number,
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: durationController,
                    decoration: InputDecoration(labelText: 'Trajanje (min)', prefixIcon: Icon(Icons.access_time)),
                    keyboardType: TextInputType.number,
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('Odustani'),
            ),
            ElevatedButton(
              onPressed: () async {
                if (formKey.currentState!.validate()) {
                  final service = Service(
                    id: 0,
                    salonId: _selectedSalon!.id,
                    name: nameController.text,
                    description: descriptionController.text.isNotEmpty ? descriptionController.text : null,
                    price: double.parse(priceController.text),
                    durationMinutes: int.parse(durationController.text),
                  );

                  final success = await context.read<ServiceProvider>().addService(service);

                  if (!context.mounted) return;

                  if (success) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Usluga dodana!')));
                    Navigator.pop(context);
                  } else {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška!')));
                  }
                }
              },
              child: Text('Dodaj'),
            ),
          ],
        );
      },
    );
  }

  void _showEditServiceDialog(BuildContext context, Service service) {
    final nameController = TextEditingController(text: service.name);
    final priceController = TextEditingController(text: service.price.toString());
    final durationController = TextEditingController(text: service.durationMinutes.toString());
    final descriptionController = TextEditingController(text: service.description ?? '');
    final formKey = GlobalKey<FormState>();

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text('Uredi Uslugu'),
          content: SingleChildScrollView(
            child: Form(
              key: formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: nameController,
                    decoration: InputDecoration(labelText: 'Naziv', prefixIcon: Icon(Icons.label)),
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: descriptionController,
                    decoration: InputDecoration(labelText: 'Opis (opciono)', prefixIcon: Icon(Icons.description)),
                    maxLines: 2,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: priceController,
                    decoration: InputDecoration(labelText: 'Cijena (KM)', prefixIcon: Icon(Icons.attach_money)),
                    keyboardType: TextInputType.number,
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: durationController,
                    decoration: InputDecoration(labelText: 'Trajanje (min)', prefixIcon: Icon(Icons.access_time)),
                    keyboardType: TextInputType.number,
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('Odustani'),
            ),
            ElevatedButton(
              onPressed: () async {
                if (formKey.currentState!.validate()) {
                  final updated = Service(
                    id: service.id,
                    salonId: service.salonId,
                    name: nameController.text,
                    description: descriptionController.text.isNotEmpty ? descriptionController.text : null,
                    price: double.parse(priceController.text),
                    durationMinutes: int.parse(durationController.text),
                    isPopular: service.isPopular,
                    isActive: service.isActive,
                  );

                  final success = await context.read<ServiceProvider>().updateService(updated);

                  if (!context.mounted) return;

                  if (success) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Usluga ažurirana!')));
                    Navigator.pop(context);
                  } else {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška!')));
                  }
                }
              },
              child: Text('Spremi'),
            ),
          ],
        );
      },
    );
  }
}
