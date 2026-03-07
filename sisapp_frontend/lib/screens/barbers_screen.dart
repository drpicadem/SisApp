import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:image_picker/image_picker.dart';
import '../providers/barber_provider.dart';
import '../providers/salon_provider.dart';
import '../providers/auth_provider.dart';
import '../providers/service_provider.dart';
import '../models/barber.dart';
import '../models/salon.dart';
import '../models/service.dart';
import '../services/api_service.dart';
import '../services/image_service.dart';
import '../widgets/entity_image.dart';

class BarbersScreen extends StatefulWidget {
  @override
  _BarbersScreenState createState() => _BarbersScreenState();
}

class _BarbersScreenState extends State<BarbersScreen> {
  Salon? _selectedSalon;

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
              address: '', 
              city: '',
              phone: '',
              postalCode: '',
              country: '',
            );
          });
          context.read<BarberProvider>().loadBarbers(salonId);
          context.read<ServiceProvider>().loadServices(salonId);
        }
      } else {
        context.read<SalonProvider>().loadSalons();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Upravljanje Uposlenicima'),
      ),
      body: Column(
        children: [
          // Filter Section
          Consumer<AuthProvider>(
            builder: (context, authProvider, _) {
              if (authProvider.isBarber) {
                return SizedBox.shrink(); // Hide dropdown for barbers
              }

              return Padding(
                padding: const EdgeInsets.all(16.0),
                child: Consumer<SalonProvider>(
                  builder: (context, salonProvider, child) {
                    if (salonProvider.isLoading) return LinearProgressIndicator();
                    
                    return DropdownButtonFormField<Salon>(
                      value: _selectedSalon,
                      decoration: InputDecoration(
                        labelText: 'Odaberite salon za pregled',
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
                          context.read<BarberProvider>().loadBarbers(newValue.id);
                          context.read<ServiceProvider>().loadServices(newValue.id);
                        }
                      },
                    );
                  },
                ),
              );
            }
          ),
          
          // List Section
          Expanded(
            child: Consumer<BarberProvider>(
              builder: (context, provider, child) {
                if (_selectedSalon == null) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.person_search, size: 64, color: Colors.grey[300]),
                        SizedBox(height: 16),
                        Text('Učitavam podatke ili odaberite salon iznad.',
                            style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                      ],
                    ),
                  );
                }

                if (provider.isLoading) {
                  return Center(child: CircularProgressIndicator());
                }

                if (provider.barbers.isEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.person_off, size: 64, color: Colors.grey[300]),
                        SizedBox(height: 16),
                        Text('Nema dodanih frizera u ovom salonu.',
                            style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                      ],
                    ),
                  );
                }

                return ListView.builder(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  itemCount: provider.barbers.length,
                  itemBuilder: (context, index) {
                    final barber = provider.barbers[index];
                    return _buildBarberCard(barber);
                  },
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        icon: Icon(Icons.person_add),
        label: Text('Novi uposlenik'),
        backgroundColor: Color(0xFFE0CFA9),
        onPressed: () => _showAddBarberDialog(context),
      ),
    );
  }

  Widget _buildBarberCard(Barber barber) {
    return Card(
      margin: EdgeInsets.only(bottom: 12),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          children: [
            EntityImage(
              entityType: 'Barber',
              entityId: barber.id,
              token: context.read<AuthProvider>().tokenResponse?.token ?? '',
              isCircular: true,
              circularRadius: 28,
              placeholderIcon: Icons.person,
              placeholderIconSize: 24,
            ),
            SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${barber.firstName} ${barber.lastName}',
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                  SizedBox(height: 4),
                  Text(barber.email, style: TextStyle(color: Colors.grey[600], fontSize: 13)),
                  if (barber.bio.isNotEmpty) ...[
                    SizedBox(height: 4),
                    Text(barber.bio,
                        style: TextStyle(color: Colors.grey[500], fontSize: 12),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis),
                  ],
                  SizedBox(height: 8),
                  Row(
                    children: [
                      Icon(Icons.star, color: Colors.amber, size: 16),
                      SizedBox(width: 4),
                      Text(barber.rating.toStringAsFixed(1),
                          style: TextStyle(fontWeight: FontWeight.w600)),
                    ],
                  ),
                ],
              ),
            ),
            Row(
              children: [
                Column(
                  children: [
                    IconButton(
                      icon: Icon(Icons.edit, color: Colors.blue),
                      tooltip: 'Uredi',
                      onPressed: () => _showEditBarberDialog(context, barber),
                    ),
                    Text('Uredi', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  ],
                ),
                SizedBox(width: 8),
                Column(
                  children: [
                    IconButton(
                      icon: Icon(Icons.content_cut, color: Color(0xFFE0CFA9)),
                      tooltip: 'Dodijeli usluge',
                      onPressed: () => _showServiceAssignmentDialog(context, barber),
                    ),
                    Text('Usluge', style: TextStyle(fontSize: 10, color: Colors.grey)),
                  ],
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  void _showServiceAssignmentDialog(BuildContext context, Barber barber) async {
    final token = context.read<AuthProvider>().tokenResponse?.token;
    if (token == null) return;

    final apiService = ApiService();

    // Load barber's current services and salon services
    final barberServices = await apiService.getBarberServices(barber.id, token);
    final assignedServiceIds = barberServices.map((s) => s['serviceId'] as int).toSet();

    final salonServices = context.read<ServiceProvider>().services;
    if (salonServices.isEmpty) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Nema usluga u ovom salonu. Prvo dodajte usluge.')),
        );
      }
      return;
    }

    // Create a mutable copy of assigned ids for the dialog
    final selectedIds = Set<int>.from(assignedServiceIds);

    if (!mounted) return;

    showDialog(
      context: context,
      builder: (dialogContext) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: Text('Usluge za ${barber.firstName} ${barber.lastName}'),
              content: SizedBox(
                width: double.maxFinite,
                child: ListView.builder(
                  shrinkWrap: true,
                  itemCount: salonServices.length,
                  itemBuilder: (context, index) {
                    final service = salonServices[index];
                    final isSelected = selectedIds.contains(service.id);
                    return CheckboxListTile(
                      title: Text(service.name),
                      subtitle: Text('${service.durationMinutes} min - ${service.price.toStringAsFixed(2)} KM'),
                      value: isSelected,
                      activeColor: Color(0xFFE0CFA9),
                      onChanged: (bool? value) {
                        setDialogState(() {
                          if (value == true) {
                            selectedIds.add(service.id);
                          } else {
                            selectedIds.remove(service.id);
                          }
                        });
                      },
                    );
                  },
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(dialogContext),
                  child: Text('Odustani'),
                ),
                ElevatedButton(
                  onPressed: () async {
                    Navigator.pop(dialogContext);
                    final success = await apiService.assignBarberServices(
                      barber.id, selectedIds.toList(), token,
                    );
                    if (mounted) {
                      ScaffoldMessenger.of(this.context).showSnackBar(
                        SnackBar(content: Text(success ? 'Usluge ažurirane!' : 'Greška pri ažuriranju.')),
                      );
                    }
                  },
                  child: Text('Spremi'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  void _showAddBarberDialog(BuildContext context) {
    final _firstNameController = TextEditingController();
    final _lastNameController = TextEditingController();
    final _usernameController = TextEditingController();
    final _emailController = TextEditingController();
    final _passwordController = TextEditingController();
    final _bioController = TextEditingController();
    final _formKey = GlobalKey<FormState>();
    File? _selectedImage;
    final _picker = ImagePicker();
    
    // Default to currently selected salon if available
    Salon? _dialogSelectedSalon = _selectedSalon;

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: Text('Novi Uposlenik'),
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
                        child: CircleAvatar(
                          radius: 50,
                          backgroundColor: Colors.grey[300],
                          backgroundImage: _selectedImage != null
                              ? FileImage(_selectedImage!)
                              : null,
                          child: _selectedImage == null
                              ? Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(Icons.add_a_photo, size: 28, color: Colors.grey[600]),
                                    SizedBox(height: 4),
                                    Text('Slika', style: TextStyle(fontSize: 10, color: Colors.grey[600])),
                                  ],
                                )
                              : null,
                        ),
                      ),
                      SizedBox(height: 16),
                       Consumer<SalonProvider>(
                        builder: (context, salonProvider, _) {
                          final authProvider = context.read<AuthProvider>();
                          if (authProvider.isBarber) {
                            return SizedBox.shrink(); // Hide dropdown for barbers
                          }
                          
                          return DropdownButtonFormField<Salon>(
                            value: _dialogSelectedSalon,
                            decoration: InputDecoration(labelText: 'Salon'),
                            items: salonProvider.salons.map((salon) {
                              return DropdownMenuItem(
                                value: salon,
                                child: Text(salon.name),
                              );
                            }).toList(),
                            onChanged: (val) {
                              setDialogState(() {
                                _dialogSelectedSalon = val;
                              });
                            },
                            validator: (v) => v == null ? 'Obavezno odaberite salon' : null,
                          );
                        }
                      ),
                      TextFormField(
                        controller: _firstNameController,
                        decoration: InputDecoration(labelText: 'Ime'),
                        validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                      ),
                      TextFormField(
                        controller: _lastNameController,
                        decoration: InputDecoration(labelText: 'Prezime'),
                        validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                      ),
                      TextFormField(
                        controller: _usernameController,
                        decoration: InputDecoration(labelText: 'Korisničko ime'),
                        validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                      ),
                      TextFormField(
                        controller: _emailController,
                        decoration: InputDecoration(labelText: 'Email'),
                        validator: (v) {
                          if (v == null || v.isEmpty) return 'Obavezno';
                          if (!RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$').hasMatch(v)) return 'Neispravan email format';
                          return null;
                        },
                      ),
                      TextFormField(
                        controller: _passwordController,
                        decoration: InputDecoration(labelText: 'Lozinka'),
                        obscureText: true,
                        validator: (v) => v!.length < 6 ? 'Min 6 znakova' : null,
                      ),
                       TextFormField(
                        controller: _bioController,
                        decoration: InputDecoration(labelText: 'Kratka biografija'),
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
                    if (_formKey.currentState!.validate() && _dialogSelectedSalon != null) {
                      final dto = CreateBarberDto(
                        salonId: _dialogSelectedSalon!.id,
                        firstName: _firstNameController.text,
                        lastName: _lastNameController.text,
                         username: _usernameController.text,
                        email: _emailController.text,
                        password: _passwordController.text,
                        bio: _bioController.text,
                      );

                      final createdId = await context.read<BarberProvider>().addBarber(dto);
                      
                      if (!context.mounted) return;

                      if (createdId != null) {
                        // Upload image if selected
                        if (_selectedImage != null) {
                          final token = context.read<AuthProvider>().tokenResponse?.token;
                          if (token != null) {
                            await ImageService.uploadImage(
                              _selectedImage!,
                              token,
                              imageType: 'barber',
                              entityId: createdId,
                              entityType: 'Barber',
                            );
                          }
                        }
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Uposlenik dodan!')));
                         Navigator.pop(context);
                         
                         // Refresh list if added to current view
                         if (_selectedSalon?.id == _dialogSelectedSalon?.id) {
                           this.context.read<BarberProvider>().loadBarbers(_selectedSalon!.id);
                         }
                      } else {
                         ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Neuspješno dodavanje uposlenika. Provjerite podatke.')));
                      }
                    }
                  },
                  child: Text('Dodaj'),
                ),
              ],
            );
          }
        );
      },
    );
  }

  void _showEditBarberDialog(BuildContext context, Barber barber) {
    final _firstNameController = TextEditingController(text: barber.firstName);
    final _lastNameController = TextEditingController(text: barber.lastName);
    final _usernameController = TextEditingController(text: barber.username);
    final _emailController = TextEditingController(text: barber.email);
    final _passwordController = TextEditingController();
    final _bioController = TextEditingController(text: barber.bio);
    final _formKey = GlobalKey<FormState>();
    File? _selectedImage;
    final _picker = ImagePicker();
    
    bool isDirty = false;

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            void checkDirty() {
              final dirty = _firstNameController.text != barber.firstName ||
                  _lastNameController.text != barber.lastName ||
                  _usernameController.text != barber.username ||
                  _emailController.text != barber.email ||
                  _passwordController.text.isNotEmpty ||
                  _bioController.text != barber.bio ||
                  _selectedImage != null;
              if (dirty != isDirty) {
                setDialogState(() {
                  isDirty = dirty;
                });
              }
            }

            return AlertDialog(
              title: Text('Uredi Uposlenika'),
              content: SingleChildScrollView(
                child: Form(
                  key: _formKey,
                  onChanged: checkDirty,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      GestureDetector(
                        onTap: () async {
                          final XFile? pickedFile = await _picker.pickImage(source: ImageSource.gallery, maxWidth: 1024, maxHeight: 1024, imageQuality: 85);
                          if (pickedFile != null) {
                            setDialogState(() {
                              _selectedImage = File(pickedFile.path);
                              checkDirty();
                            });
                          }
                        },
                        child: CircleAvatar(
                          radius: 50,
                          backgroundColor: Colors.grey[300],
                          backgroundImage: _selectedImage != null ? FileImage(_selectedImage!) : null,
                          child: _selectedImage == null
                              ? EntityImage(
                                  entityType: 'Barber',
                                  entityId: barber.id,
                                  token: context.read<AuthProvider>().tokenResponse?.token ?? '',
                                  isCircular: true,
                                  circularRadius: 50,
                                  placeholderIcon: Icons.add_a_photo,
                                  placeholderIconSize: 28,
                                )
                              : null,
                        ),
                      ),
                      SizedBox(height: 16),
                      TextFormField(
                        controller: _firstNameController,
                        decoration: InputDecoration(labelText: 'Ime'),
                        validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                      ),
                      TextFormField(
                        controller: _lastNameController,
                        decoration: InputDecoration(labelText: 'Prezime'),
                        validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                      ),
                      TextFormField(
                        controller: _usernameController,
                        decoration: InputDecoration(labelText: 'Korisničko ime'),
                        validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                      ),
                      TextFormField(
                        controller: _emailController,
                        decoration: InputDecoration(labelText: 'Email'),
                        validator: (v) {
                          if (v == null || v.isEmpty) return 'Obavezno';
                          if (!RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$').hasMatch(v)) return 'Neispravan email format';
                          return null;
                        },
                      ),
                      TextFormField(
                        controller: _passwordController,
                        decoration: InputDecoration(labelText: 'Nova lozinka (opcionalno)'),
                        obscureText: true,
                        validator: (v) => v!.isNotEmpty && v.length < 6 ? 'Min 6 znakova' : null,
                      ),
                      TextFormField(
                        controller: _bioController,
                        decoration: InputDecoration(labelText: 'Kratka biografija'),
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
                  onPressed: isDirty ? () async {
                    if (_formKey.currentState!.validate()) {
                      final dto = UpdateBarberDto(
                        firstName: _firstNameController.text,
                        lastName: _lastNameController.text,
                        username: _usernameController.text,
                        email: _emailController.text,
                        password: _passwordController.text.isEmpty ? null : _passwordController.text,
                        bio: _bioController.text.isEmpty ? null : _bioController.text,
                      );

                      try {
                        final success = await context.read<BarberProvider>().updateBarber(barber.id, dto);
                        
                        if (!context.mounted) return;

                        if (success) {
                          if (_selectedImage != null) {
                            final token = context.read<AuthProvider>().tokenResponse?.token;
                            if (token != null) {
                              await ImageService.uploadImage(
                                _selectedImage!,
                                token,
                                imageType: 'barber',
                                entityId: barber.id,
                                entityType: 'Barber',
                              );
                              if (_selectedSalon != null) {
                                this.context.read<BarberProvider>().loadBarbers(_selectedSalon!.id);
                              }
                            }
                          }
                          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Uposlenik uspješno ažuriran!')));
                          Navigator.pop(context);
                        } else {
                          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Neuspješno ažuriranje uposlenika.')));
                        }
                      } catch (e) {
                         ScaffoldMessenger.of(context).showSnackBar(
                           SnackBar(content: Text(e.toString().replaceFirst('Exception: ', '')))
                         );
                      }
                    }
                  } : null,
                  child: Text('Spremi'),
                ),
              ],
            );
          }
        );
      },
    );
  }
}
