import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:image_picker/image_picker.dart';
import '../providers/salon_provider.dart';
import '../providers/auth_provider.dart';
import '../models/salon.dart';
import '../services/image_service.dart';

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
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.store_mall_directory, size: 64, color: Colors.grey[300]),
                  SizedBox(height: 16),
                  Text('Nema dostupnih salona.',
                      style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                ],
              ),
            );
          }

          return Padding(
            padding: const EdgeInsets.all(16.0),
            child: Card(
              elevation: 2,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Row(
                      children: [
                        Icon(Icons.store, color: Color(0xFFE0CFA9)),
                        SizedBox(width: 8),
                        Text('Saloni (${provider.salons.length})',
                            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ),
                  Divider(height: 1),
                  Expanded(
                    child: SingleChildScrollView(
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
                            DataColumn(label: Text('Status')),
                            DataColumn(label: Text('Akcija')),
                          ],
                          rows: provider.salons.map((salon) {
                            return DataRow(cells: [
                              DataCell(Text(salon.name, style: TextStyle(fontWeight: FontWeight.w600))),
                              DataCell(Text(salon.city)),
                              DataCell(Text(salon.address)),
                              DataCell(Text(salon.employeeCount.toString())),
                              DataCell(Row(children: [
                                Icon(Icons.star, size: 16, color: Colors.amber),
                                SizedBox(width: 4),
                                Text(salon.rating.toStringAsFixed(1)),
                              ])),
                              DataCell(
                                Chip(
                                  label: Text(
                                    salon.isActive ? 'Aktivan' : 'Suspendovan',
                                    style: TextStyle(color: Colors.white, fontSize: 12),
                                  ),
                                  backgroundColor: salon.isActive ? Colors.green : Colors.red,
                                  padding: EdgeInsets.symmetric(horizontal: 4),
                                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                ),
                              ),
                              DataCell(
                                IconButton(
                                  icon: Icon(
                                    salon.isActive ? Icons.block : Icons.check_circle,
                                    color: salon.isActive ? Colors.red : Colors.green,
                                  ),
                                  tooltip: salon.isActive ? 'Suspenduj salon' : 'Aktiviraj salon',
                                  onPressed: () => _confirmSuspend(context, salon),
                                ),
                              ),
                            ]);
                          }).toList(),
                        ),
                      ),
                    ),
                  ),
                ],
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
    File? _selectedImage;
    final _picker = ImagePicker();

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: Text('Novi Frizerski Salon'),
              content: SingleChildScrollView(
                child: Form(
                  key: _formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      // Image picker
                      GestureDetector(
                        onTap: () async {
                          final XFile? pickedFile = await _picker.pickImage(
                            source: ImageSource.gallery,
                            maxWidth: 1024,
                            maxHeight: 1024,
                            imageQuality: 85,
                          );
                          if (pickedFile != null) {
                            setDialogState(() {
                              _selectedImage = File(pickedFile.path);
                            });
                          }
                        },
                        child: Container(
                          width: 120,
                          height: 120,
                          decoration: BoxDecoration(
                            color: Colors.grey[300],
                            borderRadius: BorderRadius.circular(12),
                            image: _selectedImage != null
                                ? DecorationImage(
                                    image: FileImage(_selectedImage!),
                                    fit: BoxFit.cover,
                                  )
                                : null,
                          ),
                          child: _selectedImage == null
                              ? Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(Icons.add_a_photo, size: 32, color: Colors.grey[600]),
                                    SizedBox(height: 4),
                                    Text('Dodaj sliku', style: TextStyle(fontSize: 12, color: Colors.grey[600])),
                                  ],
                                )
                              : null,
                        ),
                      ),
                      SizedBox(height: 16),
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
                        id: 0,
                        name: _nameController.text,
                        city: _cityController.text,
                        address: _addressController.text,
                        phone: _phoneController.text,
                        postalCode: _postalCodeController.text,
                        country: _countryController.text,
                        employeeCount: 0,
                        rating: 0,
                      );

                      final createdId = await context.read<SalonProvider>().addSalon(salon);
                      
                      if (!context.mounted) return;

                      if (createdId != null) {
                        // Upload image if selected
                        if (_selectedImage != null) {
                          final token = context.read<AuthProvider>().tokenResponse?.token;
                          if (token != null) {
                            await ImageService.uploadImage(
                              _selectedImage!,
                              token,
                              imageType: 'salon',
                              entityId: createdId,
                              entityType: 'Salon',
                            );
                          }
                        }
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
