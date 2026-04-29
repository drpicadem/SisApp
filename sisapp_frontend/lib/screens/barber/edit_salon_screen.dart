import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../../providers/barber_provider.dart';
import '../../providers/salon_provider.dart';
import '../../models/salon.dart';
import '../../models/city.dart';
import '../../services/api_service.dart';
import '../../utils/form_validators.dart';

class EditSalonScreen extends StatefulWidget {
  @override
  _EditSalonScreenState createState() => _EditSalonScreenState();
}

class _EditSalonScreenState extends State<EditSalonScreen> {
  final _formKey = GlobalKey<FormState>();
  bool _isLoading = true;

  final _nameController = TextEditingController();
  final _addressController = TextEditingController();
  final _phoneController = TextEditingController();
  final _postalCodeController = TextEditingController();
  final _websiteController = TextEditingController();

  Salon? _currentSalon;
  List<City> _cities = [];
  int? _selectedCityId;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadSalonData();
    });
  }

  Future<void> _loadSalonData() async {
    final barberProvider = context.read<BarberProvider>();
    final authProvider = context.read<AuthProvider>();
    final token = authProvider.tokenResponse?.token ?? '';


    await barberProvider.loadMyBarberProfile();

    if (barberProvider.myBarberProfile != null) {
      final salonId = barberProvider.myBarberProfile!.salonId;

      try {
        final apiService = ApiService();
        _cities = await apiService.getCities(token);
        _currentSalon = await apiService.getSalonById(salonId, token);

        if (_currentSalon != null) {

          _nameController.text = _currentSalon!.name;
          _addressController.text = _currentSalon!.address;
          _selectedCityId = _currentSalon!.cityId > 0 ? _currentSalon!.cityId : null;
          _phoneController.text = _currentSalon!.phone;
          _postalCodeController.text = _currentSalon!.postalCode;
          _websiteController.text = _currentSalon?.website ?? '';
        }
      } catch (e) {
        print("Salon nije pronađen: $e");
      }
    }

    setState(() {
      _isLoading = false;
    });
  }

  Future<void> _saveSalon() async {
    if (!_formKey.currentState!.validate() || _currentSalon == null) return;

    setState(() {
      _isLoading = true;
    });

    final updatedSalon = Salon(
      id: _currentSalon!.id,
      name: _nameController.text,
      address: _addressController.text,
      cityId: _selectedCityId ?? 0,
      city: _cities.firstWhere((c) => c.id == (_selectedCityId ?? 0)).name,
      phone: _phoneController.text,
      postalCode: _postalCodeController.text,
      website: _websiteController.text.isEmpty ? null : _websiteController.text,
      imageIds: _currentSalon!.imageIds,
      employeeCount: _currentSalon!.employeeCount,
      rating: _currentSalon!.rating,
      isActive: _currentSalon!.isActive,
    );

    final success = await context.read<SalonProvider>().updateSalon(updatedSalon);

    setState(() {
      _isLoading = false;
    });

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            success
                ? 'Postavke salona sačuvane!'
                : 'Spašavanje postavki salona nije uspjelo. Provjerite unesene podatke.',
          ),
        ),
      );
      if (success) {
         Navigator.pop(context);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Postavke Salona'),
        backgroundColor: Colors.blue[800],
      ),
      body: _isLoading
        ? Center(child: CircularProgressIndicator())
        : _currentSalon == null
          ? Center(child: Text('Nije moguće učitati podatke o salonu.'))
          : SingleChildScrollView(
              padding: EdgeInsets.all(16.0),
              child: Card(
                elevation: 4,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Osnovne Informacije', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.blue[900])),
                        Divider(),
                        SizedBox(height: 8),
                        TextFormField(
                          controller: _nameController,
                          decoration: InputDecoration(labelText: 'Naziv Salona', border: OutlineInputBorder(), prefixIcon: Icon(Icons.store)),
                          validator: FormValidators.salonName,
                        ),
                        SizedBox(height: 16),
                        TextFormField(
                          controller: _phoneController,
                          decoration: InputDecoration(labelText: 'Telefon', border: OutlineInputBorder(), prefixIcon: Icon(Icons.phone)),
                          validator: FormValidators.phone,
                        ),
                        SizedBox(height: 16),
                        TextFormField(
                          controller: _websiteController,
                          decoration: InputDecoration(labelText: 'Web stranica (opciono)', border: OutlineInputBorder(), prefixIcon: Icon(Icons.language)),
                          validator: FormValidators.websiteOptional,
                        ),
                        SizedBox(height: 24),
                        Text('Lokacija', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.blue[900])),
                        Divider(),
                        SizedBox(height: 8),
                        TextFormField(
                          controller: _addressController,
                          decoration: InputDecoration(labelText: 'Adresa', border: OutlineInputBorder(), prefixIcon: Icon(Icons.location_on)),
                          validator: FormValidators.address,
                        ),
                        SizedBox(height: 16),
                        Row(
                          children: [
                            Expanded(
                              child: DropdownButtonFormField<int>(
                                value: _selectedCityId,
                                isExpanded: true,
                                decoration: InputDecoration(labelText: 'Grad', border: OutlineInputBorder()),
                                items: _cities
                                    .map(
                                      (city) => DropdownMenuItem<int>(
                                        value: city.id,
                                        child: Text(city.name, overflow: TextOverflow.ellipsis),
                                      ),
                                    )
                                    .toList(),
                                onChanged: (value) {
                                  setState(() {
                                    _selectedCityId = value;
                                  });
                                },
                                validator: (value) => value == null ? 'Odaberite grad' : null,
                              ),
                            ),
                            SizedBox(width: 16),
                            Expanded(
                              child: TextFormField(
                                controller: _postalCodeController,
                                decoration: InputDecoration(labelText: 'Poštanski broj', border: OutlineInputBorder()),
                                validator: FormValidators.postalCode,
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: 32),
                        SizedBox(
                          width: double.infinity,
                          child: ElevatedButton(
                            onPressed: _saveSalon,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: Colors.blue[800],
                              padding: EdgeInsets.symmetric(vertical: 16),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                            ),
                            child: Text('SAČUVAJ IZMJENE', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white)),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
    );
  }
}
