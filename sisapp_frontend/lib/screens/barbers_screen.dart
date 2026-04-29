import 'dart:io';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/barber_provider.dart';
import '../providers/salon_provider.dart';
import '../providers/auth_provider.dart';
import '../providers/service_provider.dart';
import '../models/barber.dart';
import '../models/salon.dart';
import '../models/service.dart';
import '../services/api_service.dart';
import '../services/image_service.dart';
import '../utils/form_validators.dart';
import '../utils/error_mapper.dart';
import '../widgets/entity_image.dart';
import '../widgets/image_picker_widget.dart';

class BarbersScreen extends StatefulWidget {
  @override
  _BarbersScreenState createState() => _BarbersScreenState();
}

class _BarbersScreenState extends State<BarbersScreen> {
  Salon? _selectedSalon;
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';

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
          context.read<BarberProvider>().loadBarbers(salonId);
          context.read<ServiceProvider>().loadServices(salonId);
        }
      } else {
        context.read<SalonProvider>().loadSalons();
      }
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Upravljanje Uposlenicima'),
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


          if (_selectedSalon != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
              child: TextField(
                controller: _searchController,
                onChanged: (value) {
                  setState(() {
                    _searchQuery = value.trim().toLowerCase();
                  });
                },
                decoration: InputDecoration(
                  labelText: 'Pretraga uposlenika',
                  hintText: 'Ime, prezime ili email',
                  prefixIcon: Icon(Icons.search),
                  suffixIcon: _searchQuery.isNotEmpty
                      ? IconButton(
                          icon: Icon(Icons.clear),
                          tooltip: 'Očisti pretragu',
                          onPressed: () {
                            _searchController.clear();
                            setState(() {
                              _searchQuery = '';
                            });
                          },
                        )
                      : null,
                  border: OutlineInputBorder(),
                ),
              ),
            ),
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

                final filteredBarbers = provider.barbers.where((barber) {
                  if (_searchQuery.isEmpty) return true;
                  final fullName =
                      '${barber.firstName} ${barber.lastName}'.toLowerCase();
                  return fullName.contains(_searchQuery) ||
                      barber.email.toLowerCase().contains(_searchQuery);
                }).toList();

                if (filteredBarbers.isEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.person_off, size: 64, color: Colors.grey[300]),
                        SizedBox(height: 16),
                        Text(_searchQuery.isEmpty
                                ? 'Nema dodanih frizera u ovom salonu.'
                                : 'Nema rezultata za zadanu pretragu.',
                            style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                      ],
                    ),
                  );
                }

                return ListView.builder(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  itemCount: filteredBarbers.length,
                  itemBuilder: (context, index) {
                    final barber = filteredBarbers[index];
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


    final selectedIds = Set<int>.from(assignedServiceIds);

    if (!mounted) return;

    showDialog(
      context: context,
      builder: (dialogContext) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: Row(
                children: [
                  Expanded(child: Text('Usluge za ${barber.firstName} ${barber.lastName}')),
                  IconButton(
                    tooltip: 'Zatvori formu',
                    onPressed: () => Navigator.pop(dialogContext),
                    icon: Icon(Icons.close),
                  ),
                ],
              ),
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
                        SnackBar(
                          content: Text(
                            success
                                ? 'Dodijeljene usluge za uposlenika "${barber.firstName} ${barber.lastName}" su uspješno ažurirane.'
                                : 'Ažuriranje usluga nije uspjelo. Provjerite izbor usluga i pokušajte ponovo.',
                          ),
                        ),
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
    bool _showPassword = false;


    Salon? _dialogSelectedSalon = _selectedSalon;

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            return AlertDialog(
              title: Row(
                children: [
                  Expanded(child: Text('Novi Uposlenik')),
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
                        imageType: 'barber',
                        deferUpload: true,
                        onFileSelected: (file) {
                          setDialogState(() {
                            _selectedImage = file;
                          });
                        },
                      ),
                      SizedBox(height: 16),
                       Consumer<SalonProvider>(
                        builder: (context, salonProvider, _) {
                          final authProvider = context.read<AuthProvider>();
                          if (authProvider.isBarber) {
                            return SizedBox.shrink();
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
                        validator: FormValidators.personName,
                      ),
                      TextFormField(
                        controller: _lastNameController,
                        decoration: InputDecoration(labelText: 'Prezime'),
                        validator: FormValidators.personName,
                      ),
                      TextFormField(
                        controller: _usernameController,
                        decoration: InputDecoration(labelText: 'Korisničko ime'),
                        validator: FormValidators.username,
                      ),
                      TextFormField(
                        controller: _emailController,
                        decoration: InputDecoration(labelText: 'Email'),
                        validator: FormValidators.email,
                      ),
                      TextFormField(
                        controller: _passwordController,
                        obscureText: !_showPassword,
                        decoration: InputDecoration(labelText: 'Lozinka').copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(_showPassword ? Icons.visibility_off : Icons.visibility),
                            onPressed: () {
                              setDialogState(() {
                                _showPassword = !_showPassword;
                              });
                            },
                          ),
                        ),
                        validator: FormValidators.password,
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
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(
                            content: Text(
                              'Uposlenik "${_firstNameController.text.trim()} ${_lastNameController.text.trim()}" je dodan u salon "${_dialogSelectedSalon!.name}".',
                            ),
                          ),
                        );
                         Navigator.pop(context);


                         if (_selectedSalon?.id == _dialogSelectedSalon?.id) {
                           this.context.read<BarberProvider>().loadBarbers(_selectedSalon!.id);
                         }
                      } else {
                         ScaffoldMessenger.of(context).showSnackBar(
                           SnackBar(
                             content: Text(
                               'Dodavanje nije uspjelo. Provjerite: korisničko ime (3-30, bez razmaka), email format, lozinka (4-100).',
                             ),
                           ),
                         );
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
    final _confirmPasswordController = TextEditingController();
    final _bioController = TextEditingController(text: barber.bio);
    final _formKey = GlobalKey<FormState>();
    File? _selectedImage;
    bool _showNewPassword = false;
    bool _showConfirmPassword = false;

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
                  _confirmPasswordController.text.isNotEmpty ||
                  _bioController.text != barber.bio ||
                  _selectedImage != null;
              if (dirty != isDirty) {
                setDialogState(() {
                  isDirty = dirty;
                });
              }
            }

            return AlertDialog(
              title: Row(
                children: [
                  Expanded(child: Text('Uredi Uposlenika')),
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
                  onChanged: checkDirty,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      ImagePickerWidget(
                        token: context.read<AuthProvider>().tokenResponse?.token,
                        imageType: 'barber',
                        deferUpload: true,
                        isCircular: true,
                        size: 100,
                        currentImageUrl: null,
                        onFileSelected: (file) {
                          setDialogState(() {
                            _selectedImage = file;
                            checkDirty();
                          });
                        },
                      ),
                      SizedBox(height: 16),
                      TextFormField(
                        controller: _firstNameController,
                        decoration: InputDecoration(labelText: 'Ime'),
                        validator: FormValidators.personName,
                      ),
                      TextFormField(
                        controller: _lastNameController,
                        decoration: InputDecoration(labelText: 'Prezime'),
                        validator: FormValidators.personName,
                      ),
                      TextFormField(
                        controller: _usernameController,
                        decoration: InputDecoration(labelText: 'Korisničko ime'),
                        validator: FormValidators.username,
                      ),
                      TextFormField(
                        controller: _emailController,
                        decoration: InputDecoration(labelText: 'Email'),
                        validator: FormValidators.email,
                      ),
                      TextFormField(
                        controller: _passwordController,
                        obscureText: !_showNewPassword,
                        decoration: InputDecoration(labelText: 'Nova lozinka (opcionalno)').copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(_showNewPassword ? Icons.visibility_off : Icons.visibility),
                            onPressed: () {
                              setDialogState(() {
                                _showNewPassword = !_showNewPassword;
                              });
                            },
                          ),
                        ),
                        validator: (v) => FormValidators.password(v, optional: true),
                      ),
                      TextFormField(
                        controller: _confirmPasswordController,
                        obscureText: !_showConfirmPassword,
                        decoration: InputDecoration(labelText: 'Potvrdi novu lozinku').copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(_showConfirmPassword ? Icons.visibility_off : Icons.visibility),
                            onPressed: () {
                              setDialogState(() {
                                _showConfirmPassword = !_showConfirmPassword;
                              });
                            },
                          ),
                        ),
                        validator: (value) {
                          if (_passwordController.text.trim().isEmpty &&
                              (value == null || value.trim().isEmpty)) {
                            return null;
                          }
                          final base = FormValidators.password(value, optional: true);
                          if (base != null) return base;
                          if ((value ?? '') != _passwordController.text) {
                            return 'Potvrda lozinke mora biti ista kao nova lozinka';
                          }
                          return null;
                        },
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
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(
                                'Podaci uposlenika "${_firstNameController.text.trim()} ${_lastNameController.text.trim()}" su uspješno ažurirani.',
                              ),
                            ),
                          );
                          Navigator.pop(context);
                        } else {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(
                                'Ažuriranje nije uspjelo. Provjerite: korisničko ime (3-30), email format, nova lozinka (4-100) ako je unesena.',
                              ),
                            ),
                          );
                        }
                      } catch (e) {
                         ScaffoldMessenger.of(context).showSnackBar(
                           SnackBar(content: Text(ErrorMapper.toUserMessage(e)))
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
