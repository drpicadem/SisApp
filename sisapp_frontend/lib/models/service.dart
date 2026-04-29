class Service {
  final int id;
  final int salonId;
  final int? categoryId;
  final String? categoryName;
  final String? categoryDescription;
  final String name;
  final String? description;
  final int durationMinutes;
  final double price;
  final bool isPopular;
  final bool isActive;

  Service({
    required this.id,
    required this.salonId,
    this.categoryId,
    this.categoryName,
    this.categoryDescription,
    required this.name,
    this.description,
    required this.durationMinutes,
    required this.price,
    this.isPopular = false,
    this.isActive = true,
  });

  factory Service.fromJson(Map<String, dynamic> json) {
    return Service(
      id: (json['id'] as num?)?.toInt() ?? 0,
      salonId: (json['salonId'] as num?)?.toInt() ?? 0,
      categoryId: (json['categoryId'] as num?)?.toInt(),
      categoryName: json['categoryName']?.toString(),
      categoryDescription: json['categoryDescription']?.toString(),
      name: json['name'] ?? '',
      description: json['description'],
      durationMinutes: (json['durationMinutes'] as num?)?.toInt() ?? 0,
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
      isPopular: json['isPopular'] ?? false,
      isActive: json['isActive'] ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'salonId': salonId,
      'categoryId': categoryId,
      'categoryName': categoryName,
      'categoryDescription': categoryDescription,
      'name': name,
      'description': description,
      'durationMinutes': durationMinutes,
      'price': price,
      'isPopular': isPopular,
      'isActive': isActive,
    };
  }
  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Service && other.id == id;
  }

  @override
  int get hashCode => id.hashCode;
}
