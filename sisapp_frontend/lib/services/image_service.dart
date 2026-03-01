import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import 'api_service.dart';

class ImageService {
  static String get baseUrl => ApiService.baseUrl;

  /// Returns the full URL for an image path (e.g. /uploads/profile/abc.jpg)
  static String getFullImageUrl(String relativePath) {
    // Derive server root from API base URL (strip /api suffix)
    final serverRoot = ApiService.baseUrl.replaceAll(RegExp(r'/api$'), '');
    return '$serverRoot$relativePath';
  }

  /// Upload an image file and return the Image metadata from the server
  static Future<Map<String, dynamic>?> uploadImage(
    File file,
    String token, {
    String? imageType,
    int? entityId,
    String? entityType,
  }) async {
    try {
      var uri = Uri.parse('$baseUrl/Images/upload');
      
      // Add query params
      final queryParams = <String, String>{};
      if (imageType != null) queryParams['imageType'] = imageType;
      if (entityId != null) queryParams['entityId'] = entityId.toString();
      if (entityType != null) queryParams['entityType'] = entityType;
      if (queryParams.isNotEmpty) {
        uri = uri.replace(queryParameters: queryParams);
      }

      var request = http.MultipartRequest('POST', uri);
      request.headers['Authorization'] = 'Bearer $token';

      // Determine content type from extension
      final ext = file.path.split('.').last.toLowerCase();
      final mimeType = {
        'jpg': 'image/jpeg',
        'jpeg': 'image/jpeg',
        'png': 'image/png',
        'gif': 'image/gif',
        'webp': 'image/webp',
        'bmp': 'image/bmp',
      }[ext] ?? 'application/octet-stream';

      final parts = mimeType.split('/');
      request.files.add(await http.MultipartFile.fromPath(
        'file',
        file.path,
        contentType: MediaType(parts[0], parts[1]),
      ));

      final streamedResponse = await request.send();
      final response = await http.Response.fromStream(streamedResponse);

      print('Image upload status: ${response.statusCode}');
      print('Image upload response: ${response.body}');

      if (response.statusCode == 200) {
        return jsonDecode(response.body);
      }
      return null;
    } catch (e) {
      print('Image upload error: $e');
      return null;
    }
  }

  /// Get images for a specific entity
  static Future<List<Map<String, dynamic>>> getEntityImages(
    String entityType,
    int entityId,
    String token,
  ) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Images/entity/$entityType/$entityId'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        final list = jsonDecode(response.body) as List;
        return list.map((e) => e as Map<String, dynamic>).toList();
      }
      return [];
    } catch (e) {
      print('Get entity images error: $e');
      return [];
    }
  }

  /// Delete an image by ID
  static Future<bool> deleteImage(String imageId, String token) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/Images/$imageId'),
        headers: {'Authorization': 'Bearer $token'},
      );
      return response.statusCode == 204;
    } catch (e) {
      print('Delete image error: $e');
      return false;
    }
  }
}
