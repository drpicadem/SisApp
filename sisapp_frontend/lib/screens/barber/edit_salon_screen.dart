import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../../providers/barber_provider.dart';
import '../../providers/salon_provider.dart';
import '../../models/salon.dart';
import '../../services/api_service.dart';

class EditSalonScreen extends StatefulWidget {
  @override
  _EditSalonScreenState createState() => _EditSalonScreenState();
}

class _EditSalonScreenState extends State<EditSalonScreen> {
  final _formKey = GlobalKey<FormState>();
  bool _isLoading = true;

  final _nameController = TextEditingController();
  final _addressController = TextEditingController();
  final _cityController = TextEditingController();
  final _phoneController = TextEditingController();
  final _postalCodeController = TextEditingController();
  final _countryController = TextEditingController();
  final _websiteController = TextEditingController();

  Salon? _currentSalon;

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

    // Znamo da je korisnik Barber kad pristupa ovom ekranu
    await barberProvider.loadMyBarberProfile();

    if (barberProvider.myBarberProfile != null) {
      final salonId = barberProvider.myBarberProfile!.salonId;
      
      // Fetch full salon data directly by ID (includes PostalCode, Country, Website)
      try {
        final apiService = ApiService();
        _currentSalon = await apiService.getSalonById(salonId, token);
        
        if (_currentSalon != null) {
          // Puni kontrole podacima
          _nameController.text = _currentSalon!.name;
          _addressController.text = _currentSalon!.address;
          _cityController.text = _currentSalon!.city;
          _phoneController.text = _currentSalon!.phone;
          _postalCodeController.text = _currentSalon!.postalCode;
          _countryController.text = _currentSalon!.country;
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
      city: _cityController.text,
      phone: _phoneController.text,
      postalCode: _postalCodeController.text,
      country: _countryController.text,
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
        SnackBar(content: Text(success ? 'Postavke salona sačuvane!' : 'Greška pri spašavanju.')),
      );
      if (success) {
         Navigator.pop(context); // Povratak na dashboard nakon edita
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
                          validator: (v) => v!.isEmpty ? 'Obavezno polje' : null,
                        ),
                        SizedBox(height: 16),
                        TextFormField(
                          controller: _phoneController,
                          decoration: InputDecoration(labelText: 'Telefon', border: OutlineInputBorder(), prefixIcon: Icon(Icons.phone)),
                          validator: (v) => v!.isEmpty ? 'Obavezno polje' : null,
                        ),
                        SizedBox(height: 16),
                        TextFormField(
                          controller: _websiteController,
                          decoration: InputDecoration(labelText: 'Web stranica (opciono)', border: OutlineInputBorder(), prefixIcon: Icon(Icons.language)),
                        ),
                        SizedBox(height: 24),
                        Text('Lokacija', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Colors.blue[900])),
                        Divider(),
                        SizedBox(height: 8),
                        TextFormField(
                          controller: _addressController,
                          decoration: InputDecoration(labelText: 'Adresa', border: OutlineInputBorder(), prefixIcon: Icon(Icons.location_on)),
                          validator: (v) => v!.isEmpty ? 'Obavezno polje' : null,
                        ),
                        SizedBox(height: 16),
                        Row(
                          children: [
                            Expanded(
                              child: TextFormField(
                                controller: _cityController,
                                decoration: InputDecoration(labelText: 'Grad', border: OutlineInputBorder()),
                                validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                              ),
                            ),
                            SizedBox(width: 16),
                            Expanded(
                              child: TextFormField(
                                controller: _postalCodeController,
                                decoration: InputDecoration(labelText: 'Poštanski broj', border: OutlineInputBorder()),
                                validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                              ),
                            ),
                          ],
                        ),
                        SizedBox(height: 16),
                        TextFormField(
                          controller: _countryController,
                          decoration: InputDecoration(labelText: 'Država', border: OutlineInputBorder(), prefixIcon: Icon(Icons.public)),
                          validator: (v) => v!.isEmpty ? 'Obavezno polje' : null,
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
