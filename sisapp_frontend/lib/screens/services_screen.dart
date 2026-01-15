import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/service_provider.dart';
import '../models/service.dart';

class ServicesScreen extends StatefulWidget {
  @override
  _ServicesScreenState createState() => _ServicesScreenState();
}

class _ServicesScreenState extends State<ServicesScreen> {
  // Hardcoded salon ID for now, since we only have one main salon
  // In future we can get this from Admin Profile or selection
  final int _salonId = 1; 

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ServiceProvider>().loadServices(_salonId);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Upravljanje Uslugama'),
      ),
      body: Consumer<ServiceProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return Center(child: CircularProgressIndicator());
          }

          if (provider.services.isEmpty) {
            return Center(child: Text('Nema dodanih usluga.'));
          }

          return ListView.builder(
            itemCount: provider.services.length,
            itemBuilder: (context, index) {
              final service = provider.services[index];
              return ListTile(
                leading: CircleAvatar(
                  child: Text(service.name[0]),
                ),
                title: Text(service.name),
                subtitle: Text('${service.durationMinutes} min - ${service.price} KM'),
                trailing: Icon(Icons.arrow_forward_ios),
                onTap: () {
                  // Edit TODO
                },
              );
            },
          );
        },
      ),
      floatingActionButton: FloatingActionButton(
        child: Icon(Icons.add),
        onPressed: () => _showAddServiceDialog(context),
      ),
    );
  }

  void _showAddServiceDialog(BuildContext context) {
    final _nameController = TextEditingController();
    final _priceController = TextEditingController();
    final _durationController = TextEditingController();
    final _formKey = GlobalKey<FormState>();

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text('Nova Usluga'),
          content: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: _nameController,
                  decoration: InputDecoration(labelText: 'Naziv'),
                  validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                ),
                TextFormField(
                  controller: _priceController,
                  decoration: InputDecoration(labelText: 'Cijena (KM)'),
                  keyboardType: TextInputType.number,
                  validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                ),
                TextFormField(
                  controller: _durationController,
                  decoration: InputDecoration(labelText: 'Trajanje (min)'),
                  keyboardType: TextInputType.number,
                  validator: (v) => v!.isEmpty ? 'Obavezno' : null,
                ),
              ],
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
                  final service = Service(
                    id: 0, // Backend assigns ID
                    salonId: _salonId,
                    name: _nameController.text,
                    price: double.parse(_priceController.text),
                    durationMinutes: int.parse(_durationController.text),
                  );

                  final success = await context.read<ServiceProvider>().addService(service);
                  
                  if (!context.mounted) return;

                  if (success) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Usluga dodana!')));
                    Navigator.pop(context);
                  } else {
                     ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška!')));
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
}
