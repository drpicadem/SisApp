import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/user.dart';
import '../../providers/auth_provider.dart';
import '../../services/api_service.dart';
import '../../services/image_service.dart';
import '../../utils/form_validators.dart';
import '../../widgets/entity_image.dart';
import '../../widgets/image_picker_widget.dart';

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key});

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _emailController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _currentPasswordController = TextEditingController();
  final _newPasswordController = TextEditingController();
  final _confirmNewPasswordController = TextEditingController();

  final ApiService _apiService = ApiService();
  bool _isLoading = true;
  bool _isSaving = false;
  int _imageRefreshTick = 0;
  bool _changePassword = false;
  bool _showCurrentPassword = false;
  bool _showNewPassword = false;
  bool _showConfirmNewPassword = false;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _emailController.dispose();
    _firstNameController.dispose();
    _lastNameController.dispose();
    _phoneController.dispose();
    _currentPasswordController.dispose();
    _newPasswordController.dispose();
    _confirmNewPasswordController.dispose();
    super.dispose();
  }

  Future<void> _loadProfile() async {
    final token = context.read<AuthProvider>().tokenResponse?.token;
    if (token == null) {
      if (mounted) Navigator.pop(context, false);
      return;
    }

    final user = await _apiService.getMyProfile(token);
    if (!mounted) return;

    if (user == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Ne mogu učitati profil.')),
      );
      Navigator.pop(context, false);
      return;
    }

    _fillFromUser(user);
    setState(() => _isLoading = false);
  }

  void _fillFromUser(User user) {
    _usernameController.text = user.username;
    _emailController.text = user.email;
    _firstNameController.text = user.firstName;
    _lastNameController.text = user.lastName;
    _phoneController.text = user.phoneNumber ?? '';
  }

  String? _optionalPhoneValidator(String? value) {
    final trimmed = value?.trim() ?? '';
    if (trimmed.isEmpty) return null;
    return FormValidators.phone(trimmed);
  }

  Future<void> _save() async {
    if (_isSaving) return;
    if (!_formKey.currentState!.validate()) return;

    final token = context.read<AuthProvider>().tokenResponse?.token;
    if (token == null) return;

    setState(() => _isSaving = true);
    try {
      final updated = await _apiService.updateMyProfile({
        'username': _usernameController.text.trim(),
        'email': _emailController.text.trim(),
        'firstName': _firstNameController.text.trim(),
        'lastName': _lastNameController.text.trim(),
        'phoneNumber': _phoneController.text.trim().isEmpty ? null : _phoneController.text.trim(),
      }, token);

      if (_changePassword) {
        await _apiService.changeMyPassword(
          currentPassword: _currentPasswordController.text,
          newPassword: _newPasswordController.text,
          confirmPassword: _confirmNewPasswordController.text,
          token: token,
        );
      }

      if (!mounted) return;
      context.read<AuthProvider>().updateProfileClaims(
            username: updated.username,
            email: updated.email,
          );
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            _changePassword
                ? 'Profil korisnika "${updated.firstName} ${updated.lastName}" je ažuriran i lozinka je promijenjena. Prijavite se ponovo.'
                : 'Profil korisnika "${updated.firstName} ${updated.lastName}" je uspješno ažuriran.',
          ),
        ),
      );
      if (_changePassword) {
        await context.read<AuthProvider>().logout();
        if (!mounted) return;
        Navigator.pushNamedAndRemoveUntil(context, '/login', (route) => false);
      } else {
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.toString().replaceAll('Exception: ', ''))),
      );
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.read<AuthProvider>();
    final userId = auth.userId;
    final token = auth.tokenResponse?.token ?? '';

    return Scaffold(
      appBar: AppBar(title: const Text('Uredi profil')),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                child: Column(
                  children: [
                    if (userId != null) ...[
                      EntityImage(
                        key: ValueKey('profile-$userId-$_imageRefreshTick'),
                        entityType: 'User',
                        entityId: userId,
                        token: token,
                        isCircular: true,
                        circularRadius: 45,
                        placeholderIcon: Icons.person,
                        placeholderIconSize: 40,
                      ),
                      const SizedBox(height: 8),
                      ImagePickerWidget(
                        token: token,
                        imageType: 'profile',
                        entityId: userId,
                        entityType: 'User',
                        size: 72,
                        customUpload: (file) => ImageService.uploadMyProfileImage(file, token),
                        onImageUploaded: (_) {
                          if (!mounted) return;
                          setState(() => _imageRefreshTick++);
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('Profilna slika je uspješno ažurirana za vaš korisnički račun.'),
                            ),
                          );
                        },
                      ),
                      const SizedBox(height: 12),
                    ],
                    TextFormField(
                      controller: _usernameController,
                      decoration: const InputDecoration(
                        labelText: 'Korisničko ime',
                        prefixIcon: Icon(Icons.person_outline),
                      ),
                      validator: FormValidators.username,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _emailController,
                      decoration: const InputDecoration(
                        labelText: 'Email',
                        prefixIcon: Icon(Icons.email_outlined),
                      ),
                      validator: FormValidators.email,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _firstNameController,
                      decoration: const InputDecoration(
                        labelText: 'Ime',
                        prefixIcon: Icon(Icons.badge_outlined),
                      ),
                      validator: FormValidators.personName,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _lastNameController,
                      decoration: const InputDecoration(
                        labelText: 'Prezime',
                        prefixIcon: Icon(Icons.badge_outlined),
                      ),
                      validator: FormValidators.personName,
                    ),
                    const SizedBox(height: 12),
                    TextFormField(
                      controller: _phoneController,
                      decoration: const InputDecoration(
                        labelText: 'Telefon (opciono)',
                        prefixIcon: Icon(Icons.phone_outlined),
                      ),
                      validator: _optionalPhoneValidator,
                    ),
                    const SizedBox(height: 12),
                    SwitchListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Izmijeni lozinku'),
                      subtitle: const Text('Za promjenu lozinke unesite trenutnu i novu lozinku.'),
                      value: _changePassword,
                      onChanged: (value) {
                        setState(() {
                          _changePassword = value;
                          if (!value) {
                            _currentPasswordController.clear();
                            _newPasswordController.clear();
                            _confirmNewPasswordController.clear();
                          }
                        });
                      },
                    ),
                    if (_changePassword) ...[
                      const SizedBox(height: 8),
                      TextFormField(
                        controller: _currentPasswordController,
                        obscureText: !_showCurrentPassword,
                        decoration: const InputDecoration(
                          labelText: 'Trenutna lozinka',
                          prefixIcon: Icon(Icons.lock_clock_outlined),
                        ).copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(
                              _showCurrentPassword ? Icons.visibility_off : Icons.visibility,
                            ),
                            onPressed: () {
                              setState(() {
                                _showCurrentPassword = !_showCurrentPassword;
                              });
                            },
                          ),
                        ),
                        validator: (value) => FormValidators.password(value),
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: _newPasswordController,
                        obscureText: !_showNewPassword,
                        decoration: const InputDecoration(
                          labelText: 'Nova lozinka',
                          prefixIcon: Icon(Icons.lock_outline),
                        ).copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(
                              _showNewPassword ? Icons.visibility_off : Icons.visibility,
                            ),
                            onPressed: () {
                              setState(() {
                                _showNewPassword = !_showNewPassword;
                              });
                            },
                          ),
                        ),
                        validator: (value) => FormValidators.password(value),
                      ),
                      const SizedBox(height: 12),
                      TextFormField(
                        controller: _confirmNewPasswordController,
                        obscureText: !_showConfirmNewPassword,
                        decoration: const InputDecoration(
                          labelText: 'Potvrda nove lozinke',
                          prefixIcon: Icon(Icons.lock_reset_outlined),
                        ).copyWith(
                          suffixIcon: IconButton(
                            icon: Icon(
                              _showConfirmNewPassword ? Icons.visibility_off : Icons.visibility,
                            ),
                            onPressed: () {
                              setState(() {
                                _showConfirmNewPassword = !_showConfirmNewPassword;
                              });
                            },
                          ),
                        ),
                        validator: (value) {
                          final base = FormValidators.password(value);
                          if (base != null) return base;
                          if (value != _newPasswordController.text) {
                            return 'Potvrda lozinke mora biti ista kao nova lozinka';
                          }
                          return null;
                        },
                      ),
                    ],
                    const SizedBox(height: 20),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: _isSaving ? null : _save,
                        icon: _isSaving
                            ? const SizedBox(
                                width: 16,
                                height: 16,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Icon(Icons.save_outlined),
                        label: Text(_isSaving ? 'Spremam...' : 'Spremi izmjene'),
                      ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
