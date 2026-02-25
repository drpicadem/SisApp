import 'package:flutter/material.dart';
import '../services/image_service.dart';

/// Reusable widget that loads and displays the first image for an entity.
/// Falls back to a placeholder icon if no image is found.
class EntityImage extends StatefulWidget {
  final String entityType;
  final int entityId;
  final String token;
  final double? width;
  final double? height;
  final BoxFit fit;
  final IconData placeholderIcon;
  final double placeholderIconSize;
  final BorderRadius? borderRadius;
  final bool isCircular;
  final double? circularRadius;

  const EntityImage({
    super.key,
    required this.entityType,
    required this.entityId,
    required this.token,
    this.width,
    this.height,
    this.fit = BoxFit.cover,
    this.placeholderIcon = Icons.image,
    this.placeholderIconSize = 50,
    this.borderRadius,
    this.isCircular = false,
    this.circularRadius,
  });

  @override
  State<EntityImage> createState() => _EntityImageState();
}

class _EntityImageState extends State<EntityImage> {
  String? _imageUrl;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadImage();
  }

  Future<void> _loadImage() async {
    try {
      final images = await ImageService.getEntityImages(
        widget.entityType,
        widget.entityId,
        widget.token,
      );
      if (mounted && images.isNotEmpty) {
        setState(() {
          _imageUrl = ImageService.getFullImageUrl(images.first['url']);
          _isLoading = false;
        });
      } else {
        if (mounted) setState(() => _isLoading = false);
      }
    } catch (e) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (widget.isCircular) {
      return CircleAvatar(
        radius: widget.circularRadius ?? 30,
        backgroundColor: Colors.grey[300],
        backgroundImage: _imageUrl != null ? NetworkImage(_imageUrl!) : null,
        child: _imageUrl == null
            ? (_isLoading
                ? SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Icon(widget.placeholderIcon,
                    size: widget.placeholderIconSize,
                    color: Colors.grey[600]))
            : null,
      );
    }

    return Container(
      width: widget.width,
      height: widget.height,
      decoration: BoxDecoration(
        color: Colors.grey[300],
        borderRadius: widget.borderRadius,
      ),
      child: ClipRRect(
        borderRadius: widget.borderRadius ?? BorderRadius.zero,
        child: _imageUrl != null
            ? Image.network(
                _imageUrl!,
                fit: widget.fit,
                width: widget.width,
                height: widget.height,
                errorBuilder: (context, error, stackTrace) => Center(
                  child: Icon(widget.placeholderIcon,
                      size: widget.placeholderIconSize,
                      color: Colors.grey[500]),
                ),
              )
            : Center(
                child: _isLoading
                    ? CircularProgressIndicator()
                    : Icon(widget.placeholderIcon,
                        size: widget.placeholderIconSize,
                        color: Colors.grey[500]),
              ),
      ),
    );
  }
}
