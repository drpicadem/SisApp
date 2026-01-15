import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/salon_provider.dart';
import '../models/salon.dart';

class SalonsScreen extends StatefulWidget {
  @override
  _SalonsScreenState createState() => _SalonsScreenState();
}

class _SalonsScreenState extends State<SalonsScreen> {
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
        title: Text('Pregled Frizerskih Salona'),
      ),
      body: Consumer<SalonProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return Center(child: CircularProgressIndicator());
          }

          if (provider.salons.isEmpty) {
            return Center(child: Text('Nema dostupnih salona.'));
          }

          // Using SingleChildScrollView + DataTable for desktop-like view as per design
          return SingleChildScrollView(
            scrollDirection: Axis.vertical,
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: DataTable(
                columns: const [
                  DataColumn(label: Text('Naziv salona')),
                  DataColumn(label: Text('Grad')),
                  DataColumn(label: Text('Adresa')),
                  DataColumn(label: Text('Zaposlenih')),
                  DataColumn(label: Text('Ocjena')),
                  DataColumn(label: Text('Status/Akcija')),
                ],
                rows: provider.salons.map((salon) {
                  return DataRow(cells: [
                    DataCell(Text(salon.name)),
                    DataCell(Text(salon.city)),
                    DataCell(Text(salon.address)),
                    DataCell(Text(salon.employeeCount.toString())),
                    DataCell(Row(children: [
                      Icon(Icons.star, size: 16, color: Colors.amber),
                      Text(salon.rating.toStringAsFixed(1)),
                    ])),
                    DataCell(
                      IconButton(
                        icon: Icon(
                          salon.isActive ? Icons.verified_user : Icons.block,
                          color: salon.isActive ? Colors.green : Colors.red,
                        ),
                        tooltip: salon.isActive ? 'Suspenduj salon' : 'Aktiviraj salon',
                        onPressed: () => _confirmSuspend(context, salon),
                      ),
                    ),
                  ]);
                }).toList(),
              ),
            ),
          );
        },
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showAddSalonDialog(context),
        label: Text('Dodaj salon'),
        icon: Icon(Icons.add),
        backgroundColor: Color(0xFFE0CFA9),
      ),
    );
  }

  void _showAddSalonDialog(BuildContext context) {
    final _nameController = TextEditingController();
    final _cityController = TextEditingController();
    final _addressController = TextEditingController();
    final _phoneController = TextEditingController();
    final _postalCodeController = TextEditingController();
    final _countryController = TextEditingController();
    final _formKey = GlobalKey<FormState>();

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text('Novi Frizerski Salon'),
          content: SingleChildScrollView(
            child: Form(
              key: _formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: _nameController,
                    decoration: InputDecoration(labelText: 'Naziv Salona'),
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                  TextFormField(
                    controller: _cityController,
                    decoration: InputDecoration(labelText: 'Grad'),
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                   TextFormField(
                    controller: _postalCodeController,
                    decoration: InputDecoration(labelText: 'Poštanski broj'),
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                   TextFormField(
                    controller: _countryController,
                    decoration: InputDecoration(labelText: 'Država'),
                     validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                  TextFormField(
                    controller: _addressController,
                    decoration: InputDecoration(labelText: 'Adresa'),
                    validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                  ),
                  TextFormField(
                    controller: _phoneController,
                    decoration: InputDecoration(labelText: 'Telefon'),
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
                if (_formKey.currentState!.validate()) {
                  final salon = Salon(
                    id: 0, // Backend will assign
                    name: _nameController.text,
                    city: _cityController.text,
                    address: _addressController.text,
                    phone: _phoneController.text,
                    postalCode: _postalCodeController.text,
                    country: _countryController.text,
                    employeeCount: 0,
                    rating: 0,
                  );

                  final success = await context.read<SalonProvider>().addSalon(salon);
                  
                  if (!context.mounted) return;

                  if (success) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Salon uspješno dodan!')));
                     Navigator.pop(context);
                  } else {
                     ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška pri dodavanju salona.')));
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

  void _confirmSuspend(BuildContext context, Salon salon) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(salon.isActive ? 'Suspenduj Salon' : 'Aktiviraj Salon'),
        content: Text(salon.isActive 
            ? 'Da li ste sigurni da želite suspendovati salon "${salon.name}"? Vlasnik se neće moći prijaviti.' 
            : 'Da li ste sigurni da želite ponovo aktivirati salon "${salon.name}"?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Odustani'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: salon.isActive ? Colors.red : Colors.green),
            onPressed: () async {
              Navigator.pop(ctx);
              final success = await context.read<SalonProvider>().toggleStatus(salon);
              if (mounted) {
                if (success) {
                   ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Status promijenjen.')));
                } else {
                   ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška pri promjeni statusa.')));
                }
              }
            },
            child: Text(salon.isActive ? 'Suspenduj' : 'Aktiviraj'),
          ),
        ],
      ),
    );
  }
}
