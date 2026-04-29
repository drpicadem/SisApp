class SalonAmenity {
  final int id;
  final int salonId;
  final String name;
  final String? description;
  final String? imageId;
  final bool isAvailable;
  final int displayOrder;

  SalonAmenity({
    required this.id,
    required this.salonId,
    required this.name,
    this.description,
    this.imageId,
    this.isAvailable = true,
    required this.displayOrder,
  });

  factory SalonAmenity.fromJson(Map<String, dynamic> json) {
    return SalonAmenity(
      id: (json['id'] as num?)?.toInt() ?? 0,
      salonId: (json['salonId'] as num?)?.toInt() ?? 0,
      name: json['name'] ?? '',
      description: json['description'],
      imageId: json['imageId'],
      isAvailable: json['isAvailable'] ?? true,
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'salonId': salonId,
      'name': name,
      'description': description,
      'imageId': imageId,
      'isAvailable': isAvailable,
      'displayOrder': displayOrder,
    };
  }
}
