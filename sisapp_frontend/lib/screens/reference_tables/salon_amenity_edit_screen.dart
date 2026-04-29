import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/salon_amenity_provider.dart';
import '../../models/salon_amenity.dart';
import '../../utils/form_validators.dart';
import '../../utils/error_mapper.dart';

class SalonAmenityEditScreen extends StatefulWidget {
  final SalonAmenity? amenity;
  final int salonId;

  SalonAmenityEditScreen({this.amenity, required this.salonId});

  @override
  _SalonAmenityEditScreenState createState() => _SalonAmenityEditScreenState();
}

class _SalonAmenityEditScreenState extends State<SalonAmenityEditScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _displayOrderController = TextEditingController();
  bool _isAvailable = true;

  @override
  void initState() {
    super.initState();
    if (widget.amenity != null) {
      _nameController.text = widget.amenity!.name;
      _descriptionController.text = widget.amenity!.description ?? '';
      _displayOrderController.text = widget.amenity!.displayOrder.toString();
      _isAvailable = widget.amenity!.isAvailable;
    } else {
      _displayOrderController.text = '0';
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final name = _nameController.text.trim();

    final provider = context.read<SalonAmenityProvider>();

    final newAmenity = SalonAmenity(
      id: widget.amenity?.id ?? 0,
      salonId: widget.salonId,
      name: name,
      description: _descriptionController.text.isEmpty ? null : _descriptionController.text,
      displayOrder: int.tryParse(_displayOrderController.text) ?? 0,
      isAvailable: _isAvailable,
    );

    try {
      bool success;
      if (widget.amenity == null) {
        success = await provider.addAmenity(newAmenity);
      } else {
        success = await provider.updateAmenity(newAmenity);
      }

      if (success && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              widget.amenity == null
                  ? 'Pogodnost "$name" je dodana salonu i sačuvana.'
                  : 'Pogodnost "$name" je uspješno ažurirana.',
            ),
          ),
        );
        Navigator.pop(context);
      } else if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Čuvanje pogodnosti nije uspjelo. Provjerite unesene podatke.'))
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
           SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isEditing = widget.amenity != null;

    return Scaffold(
      appBar: AppBar(
        title: Text(isEditing ? 'Uredi Pogodnost' : 'Nova Pogodnost'),
        actions: [
          IconButton(
            tooltip: 'Zatvori formu',
            onPressed: () => Navigator.pop(context),
            icon: Icon(Icons.close),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: ListView(
            children: [
              TextFormField(
                controller: _nameController,
                decoration: InputDecoration(labelText: 'Naziv *', border: OutlineInputBorder()),
                validator: FormValidators.serviceName,
              ),
              SizedBox(height: 16),
              TextFormField(
                controller: _descriptionController,
                decoration: InputDecoration(labelText: 'Opis', border: OutlineInputBorder()),
                maxLines: 3,
              ),
              SizedBox(height: 16),
              SwitchListTile(
                title: Text('Dostupno'),
                value: _isAvailable,
                onChanged: (val) => setState(() => _isAvailable = val),
              ),
              SizedBox(height: 16),
              TextFormField(
                controller: _displayOrderController,
                decoration: InputDecoration(labelText: 'Redoslijed Prikaza', border: OutlineInputBorder()),
                keyboardType: TextInputType.number,
                validator: FormValidators.displayOrderNonNegative,
              ),
              SizedBox(height: 32),
              ElevatedButton(
                onPressed: _save,
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text('Spasi', style: TextStyle(fontSize: 16)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
