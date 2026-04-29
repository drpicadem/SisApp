import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/user_provider.dart';
import '../models/user.dart';
import 'package:intl/intl.dart';
import '../utils/error_mapper.dart';
import '../utils/form_validators.dart';

class UsersScreen extends StatefulWidget {
  @override
  _UsersScreenState createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  String? _selectedRole;
  bool _isDeleting = false;
  bool _showDeletedOnly = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _reloadUsers();
    });
  }

  Future<void> _reloadUsers() {
    return context.read<UserProvider>().loadUsers(
          refresh: true,
          role: _selectedRole,
          isDeleted: _showDeletedOnly ? true : false,
        );
  }

  Color _roleColor(String role) {
    switch (role) {
      case 'Admin':
        return Colors.red;
      case 'Barber':
        return Colors.purple;
      case 'User':
        return Colors.blue;
      default:
        return Colors.grey;
    }
  }

  String _roleLabel(String role) {
    switch (role) {
      case 'Admin':
        return 'Admin';
      case 'Barber':
        return 'Frizer';
      case 'User':
        return 'Korisnik';
      default:
        return role;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Pregled Korisnika'),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _isDeleting ? null : () => _showCreateUserDialog(context),
        icon: Icon(Icons.person_add),
        label: Text('Novi korisnik'),
      ),
      body: Stack(
        children: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              children: [
                Row(
                  children: [
                    Expanded(
                      flex: 3,
                      child: TextField(
                        controller: _searchController,
                        decoration: InputDecoration(
                          labelText: 'Pretraži po imenu...',
                          prefixIcon: Icon(Icons.search),
                          border: OutlineInputBorder(),
                        ),
                        onChanged: (value) {
                          setState(() {
                            _searchQuery = value.toLowerCase();
                          });
                        },
                      ),
                    ),
                    SizedBox(width: 16),
                    Expanded(
                      flex: 1,
                      child: DropdownButtonFormField<String?>(
                        value: _selectedRole,
                        decoration: InputDecoration(
                          labelText: 'Uloga',
                          border: OutlineInputBorder(),
                          prefixIcon: Icon(Icons.filter_list),
                        ),
                        items: [
                          DropdownMenuItem(value: null, child: Text('Sve')),
                          DropdownMenuItem(value: 'Admin', child: Text('Admin')),
                          DropdownMenuItem(value: 'Barber', child: Text('Frizer')),
                          DropdownMenuItem(value: 'User', child: Text('Korisnik')),
                        ],
                        onChanged: (value) {
                          setState(() {
                            _selectedRole = value;
                          });
                          _reloadUsers();
                        },
                      ),
                    ),
                    SizedBox(width: 16),
                    Expanded(
                      flex: 1,
                      child: DropdownButtonFormField<bool>(
                        value: _showDeletedOnly,
                        decoration: InputDecoration(
                          labelText: 'Status',
                          border: OutlineInputBorder(),
                          prefixIcon: Icon(Icons.manage_accounts),
                        ),
                        items: const [
                          DropdownMenuItem(value: false, child: Text('Aktivni')),
                          DropdownMenuItem(value: true, child: Text('Obrisani')),
                        ],
                        onChanged: (value) {
                          if (value == null) return;
                          setState(() {
                            _showDeletedOnly = value;
                          });
                          _reloadUsers();
                        },
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 16),
                Expanded(
                  child: Consumer<UserProvider>(
                    builder: (context, provider, child) {
                      if (provider.isLoading) {
                        return Center(child: CircularProgressIndicator());
                      }

                      final filteredUsers = provider.users.where((user) {
                        final fullName = '${user.firstName} ${user.lastName}'.toLowerCase();
                        final matchesSearch = fullName.contains(_searchQuery);
                        final matchesRole = _selectedRole == null || user.role == _selectedRole;
                        return matchesSearch && matchesRole;
                      }).toList();

                      if (filteredUsers.isEmpty) {
                        return Center(
                          child: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(Icons.people_outline, size: 64, color: Colors.grey[300]),
                              SizedBox(height: 16),
                              Text('Nema pronađenih korisnika.',
                                  style: TextStyle(color: Colors.grey[500], fontSize: 16)),
                            ],
                          ),
                        );
                      }

                      return Card(
                        elevation: 2,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Padding(
                              padding: const EdgeInsets.all(16.0),
                              child: Row(
                                children: [
                                  Icon(Icons.people, color: Color(0xFFE0CFA9)),
                                  SizedBox(width: 8),
                                  Text('${_showDeletedOnly ? 'Obrisani korisnici' : 'Korisnici'} (${filteredUsers.length})',
                                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                                ],
                              ),
                            ),
                            Divider(height: 1),
                            Expanded(
                              child: SingleChildScrollView(
                                scrollDirection: Axis.vertical,
                                child: SingleChildScrollView(
                                  scrollDirection: Axis.horizontal,
                                  child: DataTable(
                                    columns: const [
                                      DataColumn(label: Text('Ime')),
                                      DataColumn(label: Text('Prezime')),
                                      DataColumn(label: Text('Uloga')),
                                      DataColumn(label: Text('Datum registracije')),
                                      DataColumn(label: Text('Email')),
                                      DataColumn(label: Text('Telefon')),
                                      DataColumn(label: Text('Akcija')),
                                    ],
                                    rows: filteredUsers.map((user) {
                                      return DataRow(cells: [
                                        DataCell(Text(user.firstName)),
                                        DataCell(Text(user.lastName)),
                                        DataCell(
                                          Chip(
                                            label: Text(_roleLabel(user.role),
                                                style: TextStyle(color: Colors.white, fontSize: 12)),
                                            backgroundColor: _roleColor(user.role),
                                            padding: EdgeInsets.symmetric(horizontal: 4),
                                            materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                                          ),
                                        ),
                                        DataCell(Text(DateFormat('dd.MM.yyyy').format(user.createdAt))),
                                        DataCell(Text(user.email)),
                                        DataCell(Text((user.phoneNumber == null || user.phoneNumber!.trim().isEmpty) ? '-' : user.phoneNumber!)),
                                        DataCell(
                                          Row(
                                            children: [
                                              IconButton(
                                                icon: Icon(Icons.edit, color: Colors.blue),
                                                tooltip: 'Uredi korisnika',
                                                onPressed: (_isDeleting || _showDeletedOnly)
                                                    ? null
                                                    : () => _showEditUserDialog(context, user),
                                              ),
                                              if (!_showDeletedOnly)
                                                IconButton(
                                                  icon: Icon(Icons.delete_forever, color: Colors.red),
                                                  tooltip: 'Obriši korisnika',
                                                  onPressed: _isDeleting ? null : () => _confirmDelete(context, user),
                                                )
                                              else
                                                IconButton(
                                                  icon: Icon(Icons.restore, color: Colors.green),
                                                  tooltip: 'Vrati korisnika',
                                                  onPressed: _isDeleting
                                                      ? null
                                                      : () => _restoreUserFromList(context, user),
                                                ),
                                            ],
                                          ),
                                        ),
                                      ]);
                                    }).toList(),
                                  ),
                                ),
                              ),
                            ),
                            if (provider.hasMore || provider.isLoadingMore)
                              Padding(
                                padding: const EdgeInsets.all(12.0),
                                child: Center(
                                  child: provider.isLoadingMore
                                      ? const CircularProgressIndicator()
                                      : OutlinedButton(
                                          onPressed: () {
                                            context.read<UserProvider>().loadUsers(
                                                  refresh: false,
                                                  role: _selectedRole,
                                                  isDeleted: _showDeletedOnly ? true : false,
                                                );
                                          },
                                          child: const Text('Učitaj još'),
                                        ),
                                ),
                              ),
                          ],
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
          if (_isDeleting)
            Container(
              color: Colors.black26,
              child: Center(child: CircularProgressIndicator()),
            ),
        ],
      ),
    );
  }

  void _showCreateUserDialog(BuildContext context) {
    final formKey = GlobalKey<FormState>();
    final firstNameController = TextEditingController();
    final lastNameController = TextEditingController();
    final usernameController = TextEditingController();
    final emailController = TextEditingController();
    final phoneController = TextEditingController();
    final passwordController = TextEditingController();
    final confirmPasswordController = TextEditingController();

    bool isSubmitting = false;
    bool showPassword = false;
    bool showConfirmPassword = false;

    showDialog(
      context: context,
      builder: (dialogCtx) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            Future<void> submit() async {
              if (isSubmitting) return;
              if (!(formKey.currentState?.validate() ?? false)) return;

              setDialogState(() => isSubmitting = true);
              try {
                await context.read<UserProvider>().createUser({
                  'username': usernameController.text.trim(),
                  'email': emailController.text.trim(),
                  'firstName': firstNameController.text.trim(),
                  'lastName': lastNameController.text.trim(),
                  'phoneNumber': phoneController.text.trim().isEmpty ? null : phoneController.text.trim(),
                  'password': passwordController.text,
                });

                if (!mounted) return;
                Navigator.pop(dialogCtx);
                ScaffoldMessenger.of(this.context).showSnackBar(
                  SnackBar(
                    content: Text(
                      'Korisnik "${firstNameController.text.trim()} ${lastNameController.text.trim()}" je kreiran sa korisničkim imenom "${usernameController.text.trim()}".',
                    ),
                  ),
                );
              } catch (e) {
                if (!mounted) return;
                ScaffoldMessenger.of(this.context).showSnackBar(
                  SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
                );
              } finally {
                if (mounted) {
                  setDialogState(() => isSubmitting = false);
                }
              }
            }

            return AlertDialog(
              title: Row(
                children: [
                  Expanded(child: Text('Novi korisnik')),
                  IconButton(
                    tooltip: 'Zatvori formu',
                    onPressed: isSubmitting ? null : () => Navigator.pop(dialogCtx),
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
                        controller: firstNameController,
                        decoration: InputDecoration(labelText: 'Ime'),
                        validator: FormValidators.personName,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: lastNameController,
                        decoration: InputDecoration(labelText: 'Prezime'),
                        validator: FormValidators.personName,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: usernameController,
                        decoration: InputDecoration(labelText: 'Korisničko ime'),
                        validator: FormValidators.username,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: emailController,
                        decoration: InputDecoration(labelText: 'Email'),
                        validator: FormValidators.email,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: phoneController,
                        decoration: InputDecoration(labelText: 'Telefon (opciono)'),
                        validator: (value) {
                          final trimmed = value?.trim() ?? '';
                          if (trimmed.isEmpty) return null;
                          return FormValidators.phone(trimmed);
                        },
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: passwordController,
                        obscureText: !showPassword,
                        decoration: InputDecoration(labelText: 'Lozinka').copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(showPassword ? Icons.visibility_off : Icons.visibility),
                            onPressed: () {
                              setDialogState(() {
                                showPassword = !showPassword;
                              });
                            },
                          ),
                        ),
                        validator: (v) => FormValidators.password(v),
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: confirmPasswordController,
                        obscureText: !showConfirmPassword,
                        decoration: InputDecoration(labelText: 'Potvrda lozinke').copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(showConfirmPassword ? Icons.visibility_off : Icons.visibility),
                            onPressed: () {
                              setDialogState(() {
                                showConfirmPassword = !showConfirmPassword;
                              });
                            },
                          ),
                        ),
                        validator: (value) {
                          final base = FormValidators.password(value);
                          if (base != null) return base;
                          if (value != passwordController.text) {
                            return 'Potvrda lozinke mora biti ista kao lozinka';
                          }
                          return null;
                        },
                      ),
                    ],
                  ),
                ),
              ),
              actions: [
                TextButton(
                  onPressed: isSubmitting ? null : () => Navigator.pop(dialogCtx),
                  child: Text('Odustani'),
                ),
                ElevatedButton(
                  onPressed: isSubmitting ? null : submit,
                  child: isSubmitting
                      ? SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : Text('Kreiraj'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  void _confirmDelete(BuildContext context, User user) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Row(
          children: [
            Expanded(child: Text('Opasna radnja')),
            IconButton(
              tooltip: 'Zatvori formu',
              onPressed: () => Navigator.pop(ctx),
              icon: Icon(Icons.close),
            ),
          ],
        ),
        content: Text('Da li ste sigurni da želite obrisati korisnika ${user.firstName} ${user.lastName}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('Odustani'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () async {
              Navigator.pop(ctx);
              setState(() => _isDeleting = true);
              try {
                final success = await context.read<UserProvider>().deleteUser(user.id);
                if (mounted) {
                  setState(() => _isDeleting = false);
                  if (success) {
                    final messenger = ScaffoldMessenger.of(context);
                    messenger.hideCurrentSnackBar();
                    messenger.showSnackBar(
                      SnackBar(
                        content: Text('Korisnik "${user.firstName} ${user.lastName}" je obrisan iz sistema.'),
                        duration: const Duration(seconds: 3),
                      ),
                    );
                  }
                }
              } catch (e) {
                if (mounted) {
                  setState(() => _isDeleting = false);
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

  Future<void> _restoreUserFromList(BuildContext context, User user) async {
    setState(() => _isDeleting = true);
    try {
      final success = await context.read<UserProvider>().restoreUser(user.id);
      if (!mounted) return;
      setState(() => _isDeleting = false);
      if (success) {
        await _reloadUsers();
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Korisnik "${user.firstName} ${user.lastName}" je uspješno vraćen.'),
          ),
        );
      }
    } catch (e) {
      if (!mounted) return;
      setState(() => _isDeleting = false);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
      );
    }
  }

  void _showEditUserDialog(BuildContext context, User user) {
    final formKey = GlobalKey<FormState>();
    final firstNameController = TextEditingController(text: user.firstName);
    final lastNameController = TextEditingController(text: user.lastName);
    final usernameController = TextEditingController(text: user.username);
    final emailController = TextEditingController(text: user.email);
    final phoneController = TextEditingController(text: user.phoneNumber ?? '');
    final newPasswordController = TextEditingController();
    final confirmPasswordController = TextEditingController();

    bool isSubmitting = false;
    bool changePassword = false;
    bool showNewPassword = false;
    bool showConfirmNewPassword = false;

    showDialog(
      context: context,
      builder: (dialogCtx) {
        return StatefulBuilder(
          builder: (context, setDialogState) {
            Future<void> submit() async {
              if (isSubmitting) return;
              if (!(formKey.currentState?.validate() ?? false)) return;

              setDialogState(() => isSubmitting = true);
              try {
                await context.read<UserProvider>().updateUser(user.id, {
                  'username': usernameController.text.trim(),
                  'email': emailController.text.trim(),
                  'firstName': firstNameController.text.trim(),
                  'lastName': lastNameController.text.trim(),
                  'phoneNumber': phoneController.text.trim().isEmpty ? null : phoneController.text.trim(),
                  'isActive': user.isActive,
                });

                if (changePassword) {
                  await context.read<UserProvider>().adminSetUserPassword(
                    userId: user.id,
                    newPassword: newPasswordController.text,
                    confirmPassword: confirmPasswordController.text,
                  );
                }

                if (!mounted) return;
                Navigator.pop(dialogCtx);
                ScaffoldMessenger.of(this.context).showSnackBar(
                  SnackBar(
                    content: Text(
                      changePassword
                          ? 'Korisnik "${firstNameController.text.trim()} ${lastNameController.text.trim()}" je ažuriran i lozinka je promijenjena.'
                          : 'Podaci korisnika "${firstNameController.text.trim()} ${lastNameController.text.trim()}" su uspješno ažurirani.',
                    ),
                  ),
                );
              } catch (e) {
                if (!mounted) return;
                ScaffoldMessenger.of(this.context).showSnackBar(
                  SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
                );
              } finally {
                if (mounted) {
                  setDialogState(() => isSubmitting = false);
                }
              }
            }

            return AlertDialog(
              title: Row(
                children: [
                  Expanded(child: Text('Uredi korisnika')),
                  IconButton(
                    tooltip: 'Zatvori formu',
                    onPressed: isSubmitting ? null : () => Navigator.pop(dialogCtx),
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
                        controller: firstNameController,
                        decoration: InputDecoration(labelText: 'Ime'),
                        validator: FormValidators.personName,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: lastNameController,
                        decoration: InputDecoration(labelText: 'Prezime'),
                        validator: FormValidators.personName,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: usernameController,
                        decoration: InputDecoration(labelText: 'Korisničko ime'),
                        validator: FormValidators.username,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: emailController,
                        decoration: InputDecoration(labelText: 'Email'),
                        validator: FormValidators.email,
                      ),
                      SizedBox(height: 10),
                      TextFormField(
                        controller: phoneController,
                        decoration: InputDecoration(labelText: 'Telefon (opciono)'),
                        validator: (value) {
                          final trimmed = value?.trim() ?? '';
                          if (trimmed.isEmpty) return null;
                          return FormValidators.phone(trimmed);
                        },
                      ),
                      SizedBox(height: 10),
                      SwitchListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text('Izmijeni lozinku'),
                        value: changePassword,
                        onChanged: (value) {
                          setDialogState(() {
                            changePassword = value;
                            if (!value) {
                              newPasswordController.clear();
                              confirmPasswordController.clear();
                            }
                          });
                        },
                      ),
                      if (changePassword) ...[
                        TextFormField(
                          controller: newPasswordController,
                          obscureText: !showNewPassword,
                          decoration: InputDecoration(labelText: 'Nova lozinka').copyWith(
                            suffixIcon: IconButton(
                              icon: Icon(showNewPassword ? Icons.visibility_off : Icons.visibility),
                              onPressed: () {
                                setDialogState(() {
                                  showNewPassword = !showNewPassword;
                                });
                              },
                            ),
                          ),
                          validator: (v) => FormValidators.password(v),
                        ),
                        SizedBox(height: 10),
                        TextFormField(
                          controller: confirmPasswordController,
                          obscureText: !showConfirmNewPassword,
                          decoration: InputDecoration(labelText: 'Potvrda nove lozinke').copyWith(
                            suffixIcon: IconButton(
                              icon: Icon(showConfirmNewPassword ? Icons.visibility_off : Icons.visibility),
                              onPressed: () {
                                setDialogState(() {
                                  showConfirmNewPassword = !showConfirmNewPassword;
                                });
                              },
                            ),
                          ),
                          validator: (value) {
                            final base = FormValidators.password(value);
                            if (base != null) return base;
                            if (value != newPasswordController.text) {
                              return 'Potvrda lozinke mora biti ista kao nova lozinka';
                            }
                            return null;
                          },
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              actions: [
                TextButton(
                  onPressed: isSubmitting ? null : () => Navigator.pop(dialogCtx),
                  child: Text('Odustani'),
                ),
                ElevatedButton(
                  onPressed: isSubmitting ? null : submit,
                  child: isSubmitting
                      ? SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : Text('Spremi'),
                ),
              ],
            );
          },
        );
      },
    );
  }
}

