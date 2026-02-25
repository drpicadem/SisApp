import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../services/image_service.dart';

class ImagePickerWidget extends StatefulWidget {
  final String? currentImageUrl;
  final String token;
  final String imageType;
  final int? entityId;
  final String? entityType;
  final Function(Map<String, dynamic> imageData)? onImageUploaded;
  final double size;
  final bool isCircular;

  const ImagePickerWidget({
    super.key,
    this.currentImageUrl,
    required this.token,
    required this.imageType,
    this.entityId,
    this.entityType,
    this.onImageUploaded,
    this.size = 120,
    this.isCircular = true,
  });

  @override
  State<ImagePickerWidget> createState() => _ImagePickerWidgetState();
}

class _ImagePickerWidgetState extends State<ImagePickerWidget> {
  File? _selectedFile;
  bool _isUploading = false;
  String? _uploadedUrl;
  final ImagePicker _picker = ImagePicker();

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        GestureDetector(
          onTap: _isUploading ? null : _showPickerOptions,
          child: Stack(
            children: [
              // Image display
              widget.isCircular
                  ? CircleAvatar(
                      radius: widget.size / 2,
                      backgroundColor: Colors.grey[300],
                      backgroundImage: _getImageProvider(),
                      child: _getImageProvider() == null
                          ? Icon(Icons.person, size: widget.size / 2, color: Colors.grey[600])
                          : null,
                    )
                  : Container(
                      width: widget.size,
                      height: widget.size,
                      decoration: BoxDecoration(
                        color: Colors.grey[300],
                        borderRadius: BorderRadius.circular(12),
                        image: _getImageProvider() != null
                            ? DecorationImage(
                                image: _getImageProvider()!,
                                fit: BoxFit.cover,
                              )
                            : null,
                      ),
                      child: _getImageProvider() == null
                          ? Icon(Icons.image, size: widget.size / 2, color: Colors.grey[600])
                          : null,
                    ),
              // Camera icon overlay
              Positioned(
                bottom: 0,
                right: 0,
                child: Container(
                  padding: const EdgeInsets.all(6),
                  decoration: BoxDecoration(
                    color: Theme.of(context).primaryColor,
                    shape: BoxShape.circle,
                    border: Border.all(color: Colors.white, width: 2),
                  ),
                  child: Icon(
                    Icons.camera_alt,
                    size: widget.size / 6,
                    color: Colors.white,
                  ),
                ),
              ),
              // Loading overlay
              if (_isUploading)
                Positioned.fill(
                  child: Container(
                    decoration: BoxDecoration(
                      color: Colors.black45,
                      shape: widget.isCircular ? BoxShape.circle : BoxShape.rectangle,
                      borderRadius: widget.isCircular ? null : BorderRadius.circular(12),
                    ),
                    child: const Center(
                      child: CircularProgressIndicator(color: Colors.white),
                    ),
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'Dodirni za promjenu slike',
          style: TextStyle(fontSize: 12, color: Colors.grey[600]),
        ),
      ],
    );
  }

  ImageProvider? _getImageProvider() {
    if (_selectedFile != null) {
      return FileImage(_selectedFile!);
    }
    final url = _uploadedUrl ?? widget.currentImageUrl;
    if (url != null && url.isNotEmpty) {
      return NetworkImage(ImageService.getFullImageUrl(url));
    }
    return null;
  }

  void _showPickerOptions() {
    showModalBottomSheet(
      context: context,
      builder: (ctx) => SafeArea(
        child: Wrap(
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library),
              title: const Text('Galerija'),
              onTap: () {
                Navigator.pop(ctx);
                _pickImage(ImageSource.gallery);
              },
            ),
            ListTile(
              leading: const Icon(Icons.camera_alt),
              title: const Text('Kamera'),
              onTap: () {
                Navigator.pop(ctx);
                _pickImage(ImageSource.camera);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _pickImage(ImageSource source) async {
    try {
      final XFile? pickedFile = await _picker.pickImage(
        source: source,
        maxWidth: 1024,
        maxHeight: 1024,
        imageQuality: 85,
      );

      if (pickedFile == null) return;

      setState(() {
        _selectedFile = File(pickedFile.path);
        _isUploading = true;
      });

      final result = await ImageService.uploadImage(
        _selectedFile!,
        widget.token,
        imageType: widget.imageType,
        entityId: widget.entityId,
        entityType: widget.entityType,
      );

      if (result != null) {
        setState(() {
          _uploadedUrl = result['url'];
          _isUploading = false;
        });
        widget.onImageUploaded?.call(result);
      } else {
        setState(() => _isUploading = false);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Greška pri uploadu slike')),
          );
        }
      }
    } catch (e) {
      setState(() => _isUploading = false);
      print('Image picker error: $e');
    }
  }
}
