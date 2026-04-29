class ServiceCategory {
  final int id;
  final String name;
  final String? description;
  final String? imageId;
  final int? parentCategoryId;
  final String? parentCategoryName;
  final int displayOrder;
  final bool isActive;

  ServiceCategory({
    required this.id,
    required this.name,
    this.description,
    this.imageId,
    this.parentCategoryId,
    this.parentCategoryName,
    required this.displayOrder,
    this.isActive = true,
  });

  factory ServiceCategory.fromJson(Map<String, dynamic> json) {
    return ServiceCategory(
      id: (json['id'] as num?)?.toInt() ?? 0,
      name: json['name'] ?? '',
      description: json['description'],
      imageId: json['imageId'],
      parentCategoryId: (json['parentCategoryId'] as num?)?.toInt(),
      parentCategoryName: json['parentCategoryName'],
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
      isActive: json['isActive'] ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'description': description,
      'imageId': imageId,
      'parentCategoryId': parentCategoryId,
      'parentCategoryName': parentCategoryName,
      'displayOrder': displayOrder,
      'isActive': isActive,
    };
  }
}
