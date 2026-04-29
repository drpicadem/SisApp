import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../services/image_service.dart';
import '../utils/error_mapper.dart';

class ImagePickerWidget extends StatefulWidget {
  final String? currentImageUrl;
  final String? token;
  final String? imageType;
  final int? entityId;
  final String? entityType;
  final bool deferUpload;
  final Function(File file)? onFileSelected;
  final Future<Map<String, dynamic>?> Function(File file)? customUpload;
  final Function(Map<String, dynamic> imageData)? onImageUploaded;
  final double size;
  final bool isCircular;

  const ImagePickerWidget({
    super.key,
    this.currentImageUrl,
    this.token,
    this.imageType,
    this.entityId,
    this.entityType,
    this.deferUpload = false,
    this.onFileSelected,
    this.customUpload,
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
      final imageUrl = ImageService.getFullImageUrl(url);
      return NetworkImage(
        imageUrl,
        headers: {'Authorization': 'Bearer ${widget.token}'},
      );
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
      });

      if (widget.deferUpload) {
        widget.onFileSelected?.call(_selectedFile!);
        return;
      }

      setState(() => _isUploading = true);

      final result = widget.customUpload != null
          ? await widget.customUpload!(_selectedFile!)
          : await ImageService.uploadImage(
              _selectedFile!,
              widget.token ?? '',
              imageType: widget.imageType,
              entityId: widget.entityId,
              entityType: widget.entityType,
            );

      if (result != null) {
        final uploadedId = result['id']?.toString();
        setState(() {
          _uploadedUrl = uploadedId != null && uploadedId.isNotEmpty
              ? '/api/Images/file/$uploadedId'
              : result['url']?.toString();
          _isUploading = false;
        });
        widget.onImageUploaded?.call(result);
      } else {
        setState(() => _isUploading = false);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Upload slike nije uspio. Provjerite format slike i pokušajte ponovo.')),
          );
        }
      }
    } catch (e) {
      setState(() => _isUploading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(ErrorMapper.toUserMessage(e, fallback: 'Odabir slike nije uspio. Pokušajte ponovo.'))),
        );
      }
    }
  }
}
