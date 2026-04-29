import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/service_category_provider.dart';
import '../../models/service_category.dart';
import '../../utils/form_validators.dart';
import '../../utils/error_mapper.dart';

class ServiceCategoryEditScreen extends StatefulWidget {
  final ServiceCategory? category;

  ServiceCategoryEditScreen({this.category});

  @override
  _ServiceCategoryEditScreenState createState() => _ServiceCategoryEditScreenState();
}

class _ServiceCategoryEditScreenState extends State<ServiceCategoryEditScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _displayOrderController = TextEditingController();
  int? _selectedParentCategoryId;

  @override
  void initState() {
    super.initState();
    if (widget.category != null) {
      _nameController.text = widget.category!.name;
      _descriptionController.text = widget.category!.description ?? '';
      _displayOrderController.text = widget.category!.displayOrder.toString();
      _selectedParentCategoryId = widget.category!.parentCategoryId;
    } else {
      _displayOrderController.text = '0';
    }


    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ServiceCategoryProvider>().loadCategories();
    });
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final name = _nameController.text.trim();
    if (name.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Naziv je obavezan')));
        return;
    }

    final provider = context.read<ServiceCategoryProvider>();

    final newCategory = ServiceCategory(
      id: widget.category?.id ?? 0,
      name: name,
      description: _descriptionController.text.isEmpty ? null : _descriptionController.text,
      displayOrder: int.tryParse(_displayOrderController.text) ?? 0,
      parentCategoryId: _selectedParentCategoryId,
      isActive: widget.category?.isActive ?? true,
    );

    try {
      bool success;
      if (widget.category == null) {
        success = await provider.addCategory(newCategory);
      } else {
        success = await provider.updateCategory(newCategory);
      }

      if (success && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              widget.category == null
                  ? 'Kategorija "$name" je kreirana i sačuvana.'
                  : 'Kategorija "$name" je uspješno ažurirana.',
            ),
          ),
        );
        Navigator.pop(context);
      } else if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Čuvanje kategorije nije uspjelo. Provjerite unesene podatke.'))
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
    final isEditing = widget.category != null;

    return Scaffold(
      appBar: AppBar(
        title: Text(isEditing ? 'Uredi Kategoriju' : 'Nova Kategorija'),
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
              Consumer<ServiceCategoryProvider>(
                builder: (context, provider, _) {
                  if (provider.isLoading && provider.categories.isEmpty) {
                    return CircularProgressIndicator();
                  }


                  final parentOptions = provider.categories.where((c) => c.id != widget.category?.id).toList();

                  return DropdownButtonFormField<int?>(
                    value: _selectedParentCategoryId,
                    decoration: InputDecoration(
                      labelText: 'Nadkategorija (Opcionalno)',
                      border: OutlineInputBorder(),
                    ),
                    items: [
                      DropdownMenuItem<int?>(
                        value: null,
                        child: Text('-- Nema Nadkategorije (Glavna) --'),
                      ),
                      ...parentOptions.map((c) => DropdownMenuItem<int?>(
                        value: c.id,
                        child: Text(c.name),
                      )).toList(),
                    ],
                    onChanged: (val) {
                      setState(() {
                        _selectedParentCategoryId = val;
                      });
                    },
                  );
                },
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
