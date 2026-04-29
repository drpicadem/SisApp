import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/service_category_provider.dart';
import '../../models/service_category.dart';
import 'service_category_edit_screen.dart';

class ServiceCategoryListScreen extends StatefulWidget {
  @override
  _ServiceCategoryListScreenState createState() => _ServiceCategoryListScreenState();
}

class _ServiceCategoryListScreenState extends State<ServiceCategoryListScreen> {
  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadCategories();
    });
  }

  void _loadCategories([String? name, bool refresh = true]) {
    context.read<ServiceCategoryProvider>().loadCategories(
      refresh: refresh,
      name: name,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Kategorije Usluga'),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                labelText: 'Pretraga po nazivu',
                border: OutlineInputBorder(),
                prefixIcon: Icon(Icons.search),
                suffixIcon: IconButton(
                  icon: Icon(Icons.clear),
                  onPressed: () {
                    _searchController.clear();
                    _loadCategories();
                  },
                ),
              ),
              onSubmitted: (value) {
                _loadCategories(value);
              },
            ),
          ),
          Expanded(
            child: Consumer<ServiceCategoryProvider>(
              builder: (context, provider, child) {
                if (provider.isLoading && provider.categories.isEmpty) {
                  return Center(child: CircularProgressIndicator());
                }

                if (provider.categories.isEmpty) {
                  return Center(child: Text('Nema pronađenih kategorija.'));
                }

                return ListView.builder(
                  itemCount: provider.categories.length + ((provider.hasMore || provider.isLoadingMore) ? 1 : 0),
                  itemBuilder: (context, index) {
                    if (index == provider.categories.length) {
                      return Padding(
                        padding: const EdgeInsets.symmetric(vertical: 8),
                        child: Center(
                          child: provider.isLoadingMore
                              ? const CircularProgressIndicator()
                              : OutlinedButton(
                                  onPressed: () => _loadCategories(_searchController.text, false),
                                  child: const Text('Učitaj još'),
                                ),
                        ),
                      );
                    }
                    final category = provider.categories[index];
                    return Card(
                      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      child: ListTile(
                        isThreeLine: category.description != null && category.description!.trim().isNotEmpty,
                        leading: CircleAvatar(
                          child: Icon(Icons.category),
                          backgroundColor: Colors.blue.shade100,
                        ),
                        title: Text(category.name),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            if (category.description != null && category.description!.trim().isNotEmpty)
                              Padding(
                                padding: EdgeInsets.only(bottom: 4),
                                child: Text(
                                  category.description!.trim(),
                                  maxLines: 3,
                                  overflow: TextOverflow.ellipsis,
                                  style: TextStyle(fontSize: 13, color: Colors.grey[800]),
                                ),
                              ),
                            Text(
                              category.parentCategoryName != null
                                  ? 'Nadkategorija: ${category.parentCategoryName}'
                                  : 'Glavna kategorija',
                              style: TextStyle(fontSize: 12),
                            ),
                          ],
                        ),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              icon: Icon(Icons.edit, color: Colors.blue),
                              onPressed: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) => ServiceCategoryEditScreen(category: category),
                                  ),
                                ).then((_) => _loadCategories(_searchController.text));
                              },
                            ),
                            IconButton(
                              icon: Icon(Icons.delete, color: Colors.red),
                              onPressed: () => _confirmDelete(context, category),
                            ),
                          ],
                        ),
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
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => ServiceCategoryEditScreen(),
            ),
          ).then((_) => _loadCategories(_searchController.text));
        },
        child: Icon(Icons.add),
        tooltip: 'Dodaj Novu Kategoriju',
      ),
    );
  }

  void _confirmDelete(BuildContext context, ServiceCategory category) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('Obriši Kategoriju'),
        content: Text('Da li ste sigurni da želite obrisati "${category.name}"?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Odustani'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () async {
              Navigator.pop(ctx);
              final success = await context.read<ServiceCategoryProvider>().deleteCategory(category.id);
              if (mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(
                      success
                          ? 'Kategorija obrisana.'
                          : 'Brisanje kategorije nije uspjelo. Moguće da je povezana sa postojećim uslugama.',
                    ),
                  ),
                );
              }
            },
            child: Text('Obriši'),
          ),
        ],
      ),
    );
  }
}
