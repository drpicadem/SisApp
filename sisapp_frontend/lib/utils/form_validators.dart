class FormValidators {
  static String? requiredField(String? value, {String message = 'Polje je obavezno'}) {
    if (value == null || value.trim().isEmpty) return message;
    return null;
  }

  static String? personName(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final trimmed = value!.trim();
    if (trimmed.length < 2) return 'Minimalno 2 znaka';
    if (trimmed.length > 50) return 'Maksimalno 50 znakova';
    if (!RegExp(r"^[A-Za-zČčĆćŠšĐđŽž\s\-']+$").hasMatch(trimmed)) {
      return "Dozvoljena su slova (A-Ž), razmak, crtica (-) i apostrof (')";
    }
    return null;
  }

  static String? username(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final trimmed = value!.trim();
    if (trimmed.length < 3) return 'Minimalno 3 znaka';
    if (trimmed.length > 30) return 'Maksimalno 30 znakova';
    if (!RegExp(r'^[a-zA-Z0-9._-]+$').hasMatch(trimmed)) {
      return 'Dozvoljena su slova, brojevi i znakovi . _ - (bez razmaka)';
    }
    return null;
  }

  static String? email(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final trimmed = value!.trim();
    if (!RegExp(r'^[\w\.-]+@([\w-]+\.)+[A-Za-z]{2,}$').hasMatch(trimmed)) {
      return 'Unesite email u formatu naziv@domena.com';
    }
    return null;
  }

  static String? password(String? value, {bool optional = false}) {
    final trimmed = value?.trim() ?? '';
    if (optional && trimmed.isEmpty) return null;
    if (trimmed.isEmpty) return 'Obavezno polje';
    if (trimmed.length < 4) return 'Minimalno 4 znaka';
    if (trimmed.length > 100) return 'Maksimalno 100 znakova';
    return null;
  }

  static String? phone(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final normalized = value!.trim().replaceAll(' ', '');
    if (!RegExp(r'^\+?[0-9]{6,15}$').hasMatch(normalized)) {
      return 'Unesite telefon u formatu +38761111222 ili 061111222 (6-15 cifara)';
    }
    return null;
  }

  static String? salonName(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final trimmed = value!.trim();
    if (trimmed.length < 2) return 'Minimalno 2 znaka';
    if (trimmed.length > 100) return 'Maksimalno 100 znakova';
    return null;
  }

  static String? address(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final trimmed = value!.trim();
    if (trimmed.length < 5) return 'Minimalno 5 znakova';
    if (trimmed.length > 120) return 'Maksimalno 120 znakova';
    return null;
  }

  static String? postalCode(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final trimmed = value!.trim();
    if (!RegExp(r'^[A-Za-z0-9\- ]{3,10}$').hasMatch(trimmed)) {
      return 'Poštanski broj mora imati 3-10 znakova (slova, brojevi, razmak ili -)';
    }
    return null;
  }

  static String? websiteOptional(String? value) {
    final trimmed = value?.trim() ?? '';
    if (trimmed.isEmpty) return null;
    final uri = Uri.tryParse(trimmed);
    if (uri == null || uri.host.isEmpty) return 'Unesite URL u formatu https://domena.com';
    final lower = trimmed.toLowerCase();
    if (!(lower.startsWith('http://') || lower.startsWith('https://'))) {
      return 'URL mora početi sa http:// ili https://';
    }
    return null;
  }

  static String? serviceName(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final trimmed = value!.trim();
    if (trimmed.length < 2) return 'Minimalno 2 znaka';
    if (trimmed.length > 80) return 'Maksimalno 80 znakova';
    return null;
  }

  static String? servicePrice(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final parsed = double.tryParse(value!.trim().replaceAll(',', '.'));
    if (parsed == null) return 'Unesite broj u formatu 10 ili 10.50';
    if (parsed <= 0) return 'Cijena mora biti veća od 0 KM';
    if (parsed > 1000) return 'Cijena mora biti manja ili jednaka 1000 KM';
    return null;
  }

  static String? durationMinutes(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final parsed = int.tryParse(value!.trim());
    if (parsed == null) return 'Unesite cijeli broj minuta (npr. 30)';
    if (parsed <= 0) return 'Trajanje mora biti najmanje 1 minuta';
    if (parsed > 600) return 'Trajanje može biti najviše 600 minuta';
    return null;
  }

  static String? displayOrderNonNegative(String? value) {
    final required = requiredField(value);
    if (required != null) return required;
    final parsed = int.tryParse(value!.trim());
    if (parsed == null) return 'Unesite cijeli broj (0-10000)';
    if (parsed < 0) return 'Vrijednost mora biti 0 ili veća';
    if (parsed > 10000) return 'Vrijednost može biti najviše 10000';
    return null;
  }
}
