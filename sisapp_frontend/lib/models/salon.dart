class Salon {
  final int id;
  final String name;
  final int cityId;
  final String city;
  final String address;
  final String phone;
  final int employeeCount;
  final double rating;

  final String postalCode;
  final String? website;
  final String? imageIds;
  final double? latitude;
  final double? longitude;
  final List<String>? services;
  bool isActive;

  Salon({
    required this.id,
    required this.name,
    required this.cityId,
    required this.city,
    required this.address,
    required this.phone,
    required this.postalCode,
    this.website,
    this.imageIds,
    this.latitude,
    this.longitude,
    this.services,
    this.employeeCount = 0,
    this.rating = 0.0,
    this.isActive = true,
  });

  factory Salon.fromJson(Map<String, dynamic> json) {
    return Salon(
      id: (json['id'] as num?)?.toInt() ?? 0,
      name: json['name'] ?? '',
      cityId: (json['cityId'] as num?)?.toInt() ?? 0,
      city: json['city'] ?? '',
      address: json['address'] ?? '',
      phone: json['phone'] ?? '',
      postalCode: json['postalCode'] ?? '',
      website: json['website'],
      imageIds: json['imageIds'],
      employeeCount: (json['employeeCount'] as num?)?.toInt() ?? 0,
      rating: (json['rating'] as num?)?.toDouble() ?? 0.0,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      services: json['services'] != null ? List<String>.from(json['services']) : null,
      isActive: json['isActive'] ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'cityId': cityId,
      'city': city,
      'address': address,
      'phone': phone,
      'postalCode': postalCode,
      'website': website,
      'imageIds': imageIds,

      'rating': rating,
      'latitude': latitude,
      'longitude': longitude,
      'services': services,
      'employeeCount': employeeCount,
      'isActive': isActive,
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
