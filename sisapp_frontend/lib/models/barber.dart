class Barber {
  final int id;
  final int userId;
  final int salonId;
  final String firstName;
  final String lastName;
  final String email;
  final String username;
  final String bio;
  final double rating;
  final String? imageIds;

  Barber({
    required this.id,
    required this.userId,
    required this.salonId,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.username,
    required this.bio,
    required this.rating,
    this.imageIds,
  });

  factory Barber.fromJson(Map<String, dynamic> json) {
    return Barber(
      id: (json['id'] as num?)?.toInt() ?? 0,
      userId: (json['userId'] as num?)?.toInt() ?? 0,
      salonId: (json['salonId'] as num?)?.toInt() ?? 0,
      firstName: json['firstName'] ?? json['user']?['firstName'] ?? '',
      lastName: json['lastName'] ?? json['user']?['lastName'] ?? '',
      email: json['email'] ?? json['user']?['email'] ?? '',
      username: json['username'] ?? json['user']?['username'] ?? '',
      bio: json['bio'] ?? '',
      rating: (json['rating'] as num?)?.toDouble() ?? 0.0,
      imageIds: json['imageIds'],
    );
  }

  Barber copyWith({
    int? id,
    int? userId,
    int? salonId,
    String? firstName,
    String? lastName,
    String? email,
    String? username,
    String? bio,
    double? rating,
    String? imageIds,
  }) {
    return Barber(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      salonId: salonId ?? this.salonId,
      firstName: firstName ?? this.firstName,
      lastName: lastName ?? this.lastName,
      email: email ?? this.email,
      username: username ?? this.username,
      bio: bio ?? this.bio,
      rating: rating ?? this.rating,
      imageIds: imageIds ?? this.imageIds,
    );
  }
}

class CreateBarberDto {
  final int salonId;
  final String firstName;
  final String lastName;
  final String username;
  final String email;
  final String password;
  final String bio;

  CreateBarberDto({
    required this.salonId,
    required this.firstName,
    required this.lastName,
    required this.username,
    required this.email,
    required this.password,
    required this.bio,
  });

  Map<String, dynamic> toJson() {
    return {
      'salonId': salonId,
      'firstName': firstName,
      'lastName': lastName,
      'username': username,
      'email': email,
      'password': password,
      'bio': bio,
    };
  }
}

class UpdateBarberDto {
  final String firstName;
  final String lastName;
  final String username;
  final String email;
  final String? password;
  final String? bio;

  UpdateBarberDto({
    required this.firstName,
    required this.lastName,
    required this.username,
    required this.email,
    this.password,
    this.bio,
  });

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {
      'firstName': firstName,
      'lastName': lastName,
      'username': username,
      'email': email,
    };
    if (password != null && password!.isNotEmpty) {
      data['password'] = password;
    }
    if (bio != null) {
      data['bio'] = bio;
    }
    return data;
  }
}
