import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/service_provider.dart';
import '../providers/salon_provider.dart';
import '../providers/auth_provider.dart';
import '../providers/barber_provider.dart';
import '../providers/service_category_provider.dart';
import '../models/service.dart';
import '../models/salon.dart';
import '../utils/form_validators.dart';
import '../utils/error_mapper.dart';

class ServicesScreen extends StatefulWidget {
  @override
  _ServicesScreenState createState() => _ServicesScreenState();
}

class _ServicesScreenState extends State<ServicesScreen> {
  Salon? _selectedSalon;
  String _searchQuery = '';
  bool? _isActiveFilter;

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
          context.read<ServiceProvider>().loadServices(salonId);
        }
      } else {
        context.read<SalonProvider>().loadSalons();
      }
      context.read<ServiceCategoryProvider>().loadCategories();
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
          Consumer<AuthProvider>(
            builder: (context, authProvider, _) {
              if (authProvider.isBarber) {
                return SizedBox.shrink();
              }

              return Padding(
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
              );
            }
          ),
          if (_selectedSalon != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Column(
                children: [
                  TextField(
                    decoration: InputDecoration(
                      hintText: 'Pretraži po nazivu ili opisu usluge...',
                      prefixIcon: Icon(Icons.search),
                      border: OutlineInputBorder(),
                      isDense: true,
                    ),
                    onChanged: (value) {
                      setState(() {
                        _searchQuery = value.toLowerCase();
                      });
                    },
                  ),
                  SizedBox(height: 8),
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: Row(
                      children: [
                        Text('Status: ', style: TextStyle(fontWeight: FontWeight.bold)),
                        SizedBox(width: 8),
                        FilterChip(
                          label: Text('Sve'),
                          selected: _isActiveFilter == null,
                          onSelected: (_) {
                            setState(() {
                              _isActiveFilter = null;
                            });
                          },
                        ),
                        SizedBox(width: 8),
                        FilterChip(
                          label: Text('Aktivne'),
                          selected: _isActiveFilter == true,
                          onSelected: (selected) {
                            setState(() {
                              _isActiveFilter = selected ? true : null;
                            });
                          },
                        ),
                        SizedBox(width: 8),
                        FilterChip(
                          label: Text('Neaktivne'),
                          selected: _isActiveFilter == false,
                          onSelected: (selected) {
                            setState(() {
                              _isActiveFilter = selected ? false : null;
                            });
                          },
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

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
                        Text('Učitavam podatke ili odaberite salon',
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

                final filteredServices = provider.services.where((service) {
                  final name = service.name.toLowerCase();
                  final description = (service.description ?? '').toLowerCase();
                  final matchesSearch = _searchQuery.isEmpty ||
                      name.contains(_searchQuery) ||
                      description.contains(_searchQuery);
                  final matchesStatus =
                      _isActiveFilter == null || service.isActive == _isActiveFilter;
                  return matchesSearch && matchesStatus;
                }).toList();

                if (filteredServices.isEmpty) {
                  return Center(
                    child: Text('Nema usluga za odabrane filtere.'),
                  );
                }

                return ListView.builder(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  itemCount: filteredServices.length + ((provider.hasMore || provider.isLoadingMore) ? 1 : 0),
                  itemBuilder: (context, index) {
                    if (index == filteredServices.length) {
                      return Padding(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        child: Center(
                          child: provider.isLoadingMore
                              ? const CircularProgressIndicator()
                              : OutlinedButton(
                                  onPressed: _selectedSalon == null
                                      ? null
                                      : () => context.read<ServiceProvider>().loadServices(
                                            _selectedSalon!.id,
                                            refresh: false,
                                          ),
                                  child: const Text('Učitaj još'),
                                ),
                        ),
                      );
                    }
                    final service = filteredServices[index];
                    return _buildServiceCard(service);
                  },
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _selectedSalon != null ? () => _showAddServiceDialog(context) : null,
        tooltip: _selectedSalon != null
            ? 'Dodaj novu uslugu'
            : 'Odaberite salon za dodavanje usluge',
        label: Text('Nova usluga'),
        icon: Icon(Icons.add),
        backgroundColor: Color(0xFFE0CFA9),
      ),
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
                  Wrap(
                    spacing: 8,
                    runSpacing: 6,
                    children: [
                      _buildChip(Icons.access_time, '${service.durationMinutes} min', Colors.blue),
                      _buildChip(Icons.attach_money, '${service.price.toStringAsFixed(2)} KM', Colors.green),
                      if (service.categoryName != null && service.categoryName!.isNotEmpty)
                        _buildChip(Icons.category, service.categoryName!, Colors.purple),
                      if (service.isPopular)
                        _buildChip(Icons.star, 'Popularna', Colors.orange),
                    ],
                  ),
                  if (service.categoryDescription != null && service.categoryDescription!.trim().isNotEmpty) ...[
                    SizedBox(height: 8),
                    Text(
                      service.categoryDescription!.trim(),
                      style: TextStyle(color: Colors.grey[700], fontSize: 12, height: 1.25),
                      maxLines: 4,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ],
              ),
            ),
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
        title: Row(
          children: [
            Expanded(child: Text('Obriši uslugu')),
            IconButton(
              tooltip: 'Zatvori formu',
              onPressed: () => Navigator.pop(ctx),
              icon: Icon(Icons.close),
            ),
          ],
        ),
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
              try {
                final success = await context.read<ServiceProvider>().deleteService(service.id, _selectedSalon!.id);
                if (mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(
                        success
                            ? 'Usluga obrisana.'
                            : 'Brisanje usluge nije uspjelo. Provjerite da li je usluga povezana s aktivnim terminima.',
                      ),
                    ),
                  );
                }
              } catch (e) {
                if (mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
                  );
                }
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
    int? selectedCategoryId;
    final formKey = GlobalKey<FormState>();

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Row(
            children: [
              Expanded(child: Text('Nova Usluga')),
              IconButton(
                tooltip: 'Zatvori formu',
                onPressed: () => Navigator.pop(context),
                icon: Icon(Icons.close),
              ),
            ],
          ),
          content: SingleChildScrollView(
            child: Form(
              key: formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: nameController,
                    decoration: InputDecoration(labelText: 'Naziv', prefixIcon: Icon(Icons.label)),
                    validator: FormValidators.serviceName,
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
                    validator: FormValidators.servicePrice,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: durationController,
                    decoration: InputDecoration(labelText: 'Trajanje (min)', prefixIcon: Icon(Icons.access_time)),
                    keyboardType: TextInputType.number,
                    validator: FormValidators.durationMinutes,
                  ),
                  SizedBox(height: 8),
                  StatefulBuilder(
                    builder: (context, setLocalState) {
                      return Consumer<ServiceCategoryProvider>(
                        builder: (context, categoryProvider, _) {
                          return DropdownButtonFormField<int?>(
                            value: selectedCategoryId,
                            decoration: InputDecoration(
                              labelText: 'Kategorija (opciono)',
                              prefixIcon: Icon(Icons.category),
                            ),
                            items: [
                              DropdownMenuItem<int?>(value: null, child: Text('-- Bez kategorije --')),
                              ...categoryProvider.categories.map(
                                (c) => DropdownMenuItem<int?>(
                                  value: c.id,
                                  child: Text(c.name),
                                ),
                              ),
                            ],
                            onChanged: (val) {
                              setLocalState(() {
                                selectedCategoryId = val;
                              });
                            },
                          );
                        },
                      );
                    },
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
                    price: double.parse(priceController.text.replaceAll(',', '.')),
                    durationMinutes: int.parse(durationController.text),
                    categoryId: selectedCategoryId,
                  );

                  try {
                    final success = await context.read<ServiceProvider>().addService(service);
                    if (!context.mounted) return;
                    if (success) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(
                            'Usluga "${service.name}" je dodana u salon "${_selectedSalon!.name}".',
                          ),
                        ),
                      );
                      Navigator.pop(context);
                    } else {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(
                            'Dodavanje nije uspjelo. Provjerite: naziv (2-80), cijena (0-1000 KM), trajanje (1-600 min).',
                          ),
                        ),
                      );
                    }
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
                      );
                    }
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
    int? selectedCategoryId = service.categoryId;
    final formKey = GlobalKey<FormState>();

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Row(
            children: [
              Expanded(child: Text('Uredi Uslugu')),
              IconButton(
                tooltip: 'Zatvori formu',
                onPressed: () => Navigator.pop(context),
                icon: Icon(Icons.close),
              ),
            ],
          ),
          content: SingleChildScrollView(
            child: Form(
              key: formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextFormField(
                    controller: nameController,
                    decoration: InputDecoration(labelText: 'Naziv', prefixIcon: Icon(Icons.label)),
                    validator: FormValidators.serviceName,
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
                    validator: FormValidators.servicePrice,
                  ),
                  SizedBox(height: 8),
                  TextFormField(
                    controller: durationController,
                    decoration: InputDecoration(labelText: 'Trajanje (min)', prefixIcon: Icon(Icons.access_time)),
                    keyboardType: TextInputType.number,
                    validator: FormValidators.durationMinutes,
                  ),
                  SizedBox(height: 8),
                  StatefulBuilder(
                    builder: (context, setLocalState) {
                      return Consumer<ServiceCategoryProvider>(
                        builder: (context, categoryProvider, _) {
                          return DropdownButtonFormField<int?>(
                            value: selectedCategoryId,
                            decoration: InputDecoration(
                              labelText: 'Kategorija (opciono)',
                              prefixIcon: Icon(Icons.category),
                            ),
                            items: [
                              DropdownMenuItem<int?>(value: null, child: Text('-- Bez kategorije --')),
                              ...categoryProvider.categories.map(
                                (c) => DropdownMenuItem<int?>(
                                  value: c.id,
                                  child: Text(c.name),
                                ),
                              ),
                            ],
                            onChanged: (val) {
                              setLocalState(() {
                                selectedCategoryId = val;
                              });
                            },
                          );
                        },
                      );
                    },
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
                    price: double.parse(priceController.text.replaceAll(',', '.')),
                    durationMinutes: int.parse(durationController.text),
                    categoryId: selectedCategoryId,
                    isPopular: service.isPopular,
                    isActive: service.isActive,
                  );

                  try {
                    final success = await context.read<ServiceProvider>().updateService(updated);
                    if (!context.mounted) return;
                    if (success) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(
                            'Usluga "${updated.name}" je ažurirana u salonu "${_selectedSalon!.name}".',
                          ),
                        ),
                      );
                      Navigator.pop(context);
                    } else {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text(
                            'Ažuriranje nije uspjelo. Provjerite: naziv (2-80), cijena (0-1000 KM), trajanje (1-600 min).',
                          ),
                        ),
                      );
                    }
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
                      );
                    }
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
