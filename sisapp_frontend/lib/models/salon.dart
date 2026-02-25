class Salon {
  final int id;
  final String name;
  final String city;
  final String address;
  final String phone;
  final int employeeCount;
  final double rating;

  final String postalCode;
  final String country;
  final String? website;
  final String? imageIds;
  bool isActive;

  Salon({
    required this.id,
    required this.name,
    required this.city,
    required this.address,
    required this.phone,
    required this.postalCode,
    required this.country,
    this.website,
    this.imageIds,
    this.employeeCount = 0,
    this.rating = 0.0,
    this.isActive = true,
  });

  factory Salon.fromJson(Map<String, dynamic> json) {
    return Salon(
      id: (json['id'] as num?)?.toInt() ?? 0,
      name: json['name'] ?? '',
      city: json['city'] ?? '',
      address: json['address'] ?? '',
      phone: json['phone'] ?? '',
      postalCode: json['postalCode'] ?? '',
      country: json['country'] ?? '',
      website: json['website'],
      imageIds: json['imageIds'],
      employeeCount: (json['employeeCount'] as num?)?.toInt() ?? 0,
      rating: (json['rating'] as num?)?.toDouble() ?? 0.0,
      isActive: json['isActive'] ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'name': name,
      'city': city,
      'address': address,
      'phone': phone,
      'postalCode': postalCode,
      'country': country,
      'website': website,
      // Default values for backend
      'rating': 0,
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Salon && other.id == id;
  }

  @override
  int get hashCode => id.hashCode;
}
