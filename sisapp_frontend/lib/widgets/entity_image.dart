import 'package:flutter/material.dart';
import '../services/image_service.dart';



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

  @override
  void didUpdateWidget(covariant EntityImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    _loadImage();
  }

  Future<void> _loadImage() async {
    try {
      if (mounted) {
        setState(() {
          _isLoading = true;
        });
      }
      final images = await ImageService.getEntityImages(
        widget.entityType,
        widget.entityId,
        widget.token,
      );
      if (mounted && images.isNotEmpty) {
        images.sort((a, b) {
          DateTime parseCreatedAt(Map<String, dynamic> image) {
            final raw = image['createdAt']?.toString();
            if (raw == null) return DateTime.fromMillisecondsSinceEpoch(0);
            return DateTime.tryParse(raw) ?? DateTime.fromMillisecondsSinceEpoch(0);
          }

          final byCreated = parseCreatedAt(b).compareTo(parseCreatedAt(a));
          if (byCreated != 0) return byCreated;

          final aOrder = (a['displayOrder'] as num?)?.toInt() ?? 0;
          final bOrder = (b['displayOrder'] as num?)?.toInt() ?? 0;
          return bOrder.compareTo(aOrder);
        });

        final latest = images.first;
        final imageId = latest['id']?.toString() ?? '';
        setState(() {
          _imageUrl = imageId.isNotEmpty
              ? '${ImageService.getProtectedImageUrl(imageId)}?v=$imageId'
              : null;
          _isLoading = false;
        });
      } else {
        if (mounted) {
          setState(() {
            _imageUrl = null;
            _isLoading = false;
          });
        }
      }
    } catch (e) {
      debugPrint('EntityImage ERROR [${widget.entityType}/${widget.entityId}]: $e');
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (widget.isCircular) {
      return CircleAvatar(
        radius: widget.circularRadius ?? 30,
        backgroundColor: Colors.grey[300],
        backgroundImage: _imageUrl != null
            ? NetworkImage(
                _imageUrl!,
                headers: {'Authorization': 'Bearer ${widget.token}'},
              )
            : null,
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
                headers: {'Authorization': 'Bearer ${widget.token}'},
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
