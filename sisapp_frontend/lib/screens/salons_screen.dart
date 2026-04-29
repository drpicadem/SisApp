import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/salon_provider.dart';
import '../providers/auth_provider.dart';
import '../models/salon.dart';
import '../models/city.dart';
import '../services/api_service.dart';
import '../services/image_service.dart';
import '../utils/form_validators.dart';
import '../widgets/image_picker_widget.dart';

class SalonsScreen extends StatefulWidget {
  @override
  _SalonsScreenState createState() => _SalonsScreenState();
}

class _SalonsScreenState extends State<SalonsScreen> {
  String _searchQuery = '';
  List<City> _cities = [];

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<SalonProvider>().loadSalons();
      _loadCities();
    });
  }

  Future<void> _loadCities() async {
    final token = context.read<AuthProvider>().tokenResponse?.token;
    if (token == null || token.isEmpty) return;
    final cities = await ApiService().getCities(token);
    if (!mounted) return;
    setState(() {
      _cities = cities;
    });
  }

  @override
  Widget build(BuildContext context) {
    final canAddSalon = _cities.isNotEmpty;

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

          var salons = provider.salons.where((salon) {
            final query = _searchQuery.toLowerCase();
            return query.isEmpty ||
                   salon.name.toLowerCase().contains(query) ||
                   salon.city.toLowerCase().contains(query);
          }).toList();

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
                        Text('Saloni (${salons.length})',
                            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                        Spacer(),
                        SizedBox(
                          width: 250,
                          child: TextField(
                            decoration: InputDecoration(
                              hintText: 'Pretraži po nazivu ili gradu...',
                              prefixIcon: Icon(Icons.search, size: 20),
                              isDense: true,
                              contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                              border: OutlineInputBorder(
                                borderRadius: BorderRadius.circular(8),
                              ),
                            ),
                            onChanged: (value) {
                              setState(() {
                                _searchQuery = value;
                              });
                            },
                          ),
                        ),
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
                          rows: salons.map((salon) {
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
                  if (provider.hasMore || provider.isLoadingMore)
                    Padding(
                      padding: const EdgeInsets.all(12.0),
                      child: Center(
                        child: provider.isLoadingMore
                            ? const CircularProgressIndicator()
                            : OutlinedButton(
                                onPressed: () => context.read<SalonProvider>().loadSalons(refresh: false),
                                child: const Text('Učitaj još'),
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
        onPressed: canAddSalon ? () => _showAddSalonDialog(context) : null,
        tooltip: canAddSalon ? 'Dodaj salon' : 'Nema gradova u tabeli',
        label: Text('Dodaj salon'),
        icon: Icon(Icons.add),
        backgroundColor: Color(0xFFE0CFA9),
      ),
    );
  }

  void _showAddSalonDialog(BuildContext context) {
    final _nameController = TextEditingController();
    int? _selectedCityId;
    final _addressController = TextEditingController();
    final _phoneController = TextEditingController();
    final _postalCodeController = TextEditingController();
    final _formKey = GlobalKey<FormState>();
    File? _selectedImage;

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: Row(
                children: [
                  Expanded(child: Text('Novi Frizerski Salon')),
                  IconButton(
                    tooltip: 'Zatvori formu',
                    onPressed: () => Navigator.pop(context),
                    icon: Icon(Icons.close),
                  ),
                ],
              ),
              content: SingleChildScrollView(
                child: Form(
                  key: _formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [

                      ImagePickerWidget(
                        token: context.read<AuthProvider>().tokenResponse?.token,
                        imageType: 'salon',
                        deferUpload: true,
                        isCircular: false,
                        size: 120,
                        onFileSelected: (file) {
                          setDialogState(() {
                            _selectedImage = file;
                          });
                        },
                      ),
                      SizedBox(height: 16),
                      TextFormField(
                        controller: _nameController,
                        decoration: InputDecoration(labelText: 'Naziv Salona'),
                        validator: FormValidators.salonName,
                      ),
                      DropdownButtonFormField<int>(
                        initialValue: _selectedCityId,
                        decoration: InputDecoration(labelText: 'Grad'),
                        items: _cities
                            .map(
                              (city) => DropdownMenuItem<int>(
                                value: city.id,
                                child: Text(city.name),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          setDialogState(() {
                            _selectedCityId = value;
                          });
                        },
                        validator: (value) => value == null ? 'Odaberite grad' : null,
                      ),
                       TextFormField(
                        controller: _postalCodeController,
                        decoration: InputDecoration(labelText: 'Poštanski broj'),
                        validator: FormValidators.postalCode,
                      ),
                      TextFormField(
                        controller: _addressController,
                        decoration: InputDecoration(labelText: 'Adresa'),
                        validator: FormValidators.address,
                      ),
                      TextFormField(
                        controller: _phoneController,
                        decoration: InputDecoration(labelText: 'Telefon'),
                        validator: FormValidators.phone,
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
                        cityId: _selectedCityId ?? 0,
                        city: _cities.firstWhere((c) => c.id == (_selectedCityId ?? 0)).name,
                        address: _addressController.text,
                        phone: _phoneController.text,
                        postalCode: _postalCodeController.text,
                        employeeCount: 0,
                        rating: 0,
                      );

                      final createdId = await context.read<SalonProvider>().addSalon(salon);

                      if (!context.mounted) return;

                      if (createdId != null) {

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
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(
                            content: Text(
                              'Salon "${_nameController.text.trim()}" je dodan u gradu "${_cities.firstWhere((c) => c.id == (_selectedCityId ?? 0)).name}".',
                            ),
                          ),
                        );
                         Navigator.pop(context);
                      } else {
                         ScaffoldMessenger.of(context).showSnackBar(
                           SnackBar(content: Text('Dodavanje salona nije uspjelo. Provjerite naziv, adresu i kontakt podatke.')),
                         );
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
        title: Row(
          children: [
            Expanded(child: Text(salon.isActive ? 'Suspenduj Salon' : 'Aktiviraj Salon')),
            IconButton(
              tooltip: 'Zatvori formu',
              onPressed: () => Navigator.pop(ctx),
              icon: Icon(Icons.close),
            ),
          ],
        ),
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
                   final action = salon.isActive ? 'suspendovan' : 'aktiviran';
                   ScaffoldMessenger.of(context).showSnackBar(
                     SnackBar(content: Text('Salon "${salon.name}" je uspješno $action.')),
                   );
                } else {
                   ScaffoldMessenger.of(context).showSnackBar(
                     SnackBar(content: Text('Promjena statusa nije uspjela. Pokušajte ponovo.')),
                   );
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
