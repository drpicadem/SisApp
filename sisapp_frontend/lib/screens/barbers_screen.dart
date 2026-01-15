import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/barber_provider.dart';
import '../providers/salon_provider.dart';
import '../models/barber.dart';
import '../models/salon.dart';

class BarbersScreen extends StatefulWidget {
  @override
  _BarbersScreenState createState() => _BarbersScreenState();
}

class _BarbersScreenState extends State<BarbersScreen> {
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
        title: Text('Upravljanje Uposlenicima'),
      ),
      body: Column(
        children: [
          // Filter Section
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Consumer<SalonProvider>(
              builder: (context, salonProvider, child) {
                 if (salonProvider.isLoading) return LinearProgressIndicator();
                 
                 return DropdownButtonFormField<Salon>(
                   value: _selectedSalon,
                   decoration: InputDecoration(
                     labelText: 'Odaberite salon za pregled',
                     border: OutlineInputBorder(),
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
                     }
                   },
                 );
              },
            ),
          ),
          
          // List Section
          Expanded(
            child: Consumer<BarberProvider>(
              builder: (context, provider, child) {
                if (_selectedSalon == null) {
                  return Center(child: Text('Molimo odaberite salon iznad.'));
                }

                if (provider.isLoading) {
                  return Center(child: CircularProgressIndicator());
                }

                if (provider.barbers.isEmpty) {
                  return Center(child: Text('Nema dodanih frizera u ovom salonu.'));
                }

                return ListView.builder(
                  itemCount: provider.barbers.length,
                  itemBuilder: (context, index) {
                    final barber = provider.barbers[index];
                    return ListTile(
                      leading: CircleAvatar(
                        child: Text(barber.firstName.isNotEmpty ? barber.firstName[0] : '?'),
                      ),
                      title: Text('${barber.firstName} ${barber.lastName}'),
                      subtitle: Text(barber.bio),
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.star, color: Colors.amber, size: 18),
                          Text(barber.rating.toString()),
                        ],
                      ),
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        child: Icon(Icons.person_add),
        onPressed: () => _showAddBarberDialog(context),
      ),
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
    
    // Default to currently selected salon if available
    Salon? _dialogSelectedSalon = _selectedSalon;

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setState) {
            return AlertDialog(
              title: Text('Novi Uposlenik'),
              content: SingleChildScrollView(
                child: Form(
                  key: _formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                       Consumer<SalonProvider>(
                        builder: (context, salonProvider, _) {
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
                              setState(() {
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
                        validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                      ),
                      TextFormField(
                        controller: _passwordController,
                        decoration: InputDecoration(labelText: 'Lozinka'),
                        obscureText: true,
                        validator: (v) => v!.length < 4 ? 'Min 4 znaka' : null,
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

                      final success = await context.read<BarberProvider>().addBarber(dto);
                      
                      if (!context.mounted) return;

                      if (success) {
                        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Uposlenik dodan!')));
                         Navigator.pop(context);
                         
                         // Refresh list if added to current view
                         if (_selectedSalon?.id == _dialogSelectedSalon?.id) {
                           context.read<BarberProvider>().loadBarbers(_selectedSalon!.id);
                         }
                      } else {
                         ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška!')));
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
}
