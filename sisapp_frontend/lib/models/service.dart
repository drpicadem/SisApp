class Service {
  final int id;
  final int salonId;
  final String name;
  final String? description;
  final int durationMinutes;
  final double price;
  final bool isPopular;
  final bool isActive;

  Service({
    required this.id,
    required this.salonId,
    required this.name,
    this.description,
    required this.durationMinutes,
    required this.price,
    this.isPopular = false,
    this.isActive = true,
  });

  factory Service.fromJson(Map<String, dynamic> json) {
    return Service(
      id: json['id'],
      salonId: json['salonId'],
      name: json['name'],
      description: json['description'],
      durationMinutes: json['durationMinutes'],
      price: (json['price'] as num).toDouble(),
      isPopular: json['isPopular'] ?? false,
      isActive: json['isActive'] ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'salonId': salonId,
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
