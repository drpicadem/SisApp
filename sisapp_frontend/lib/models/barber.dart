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
  });

  factory Barber.fromJson(Map<String, dynamic> json) {
    return Barber(
      id: json['id'],
      userId: json['userId'],
      salonId: json['salonId'],
      firstName: json['firstName'],
      lastName: json['lastName'],
      email: json['email'],
      username: json['username'],
      bio: json['bio'] ?? '',
      rating: (json['rating'] as num).toDouble(),
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
