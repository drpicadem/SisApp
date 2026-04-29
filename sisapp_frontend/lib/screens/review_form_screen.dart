import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/review_provider.dart';
import '../models/appointment.dart';
import '../models/review.dart';

class ReviewFormScreen extends StatefulWidget {
  final Appointment appointment;
  final Review? existingReview;

  const ReviewFormScreen({
    Key? key,
    required this.appointment,
    this.existingReview,
  }) : super(key: key);

  @override
  _ReviewFormScreenState createState() => _ReviewFormScreenState();
}

class _ReviewFormScreenState extends State<ReviewFormScreen> {
  final _formKey = GlobalKey<FormState>();
  int _rating = 0;
  String? _ratingError;
  final TextEditingController _commentController = TextEditingController();
  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    if (widget.existingReview != null) {
      _rating = widget.existingReview!.rating;
      _commentController.text = widget.existingReview!.comment;
    }
  }

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  bool get _isEditing => widget.existingReview != null;

  Future<void> _submitReview() async {
    final isValid = _formKey.currentState?.validate() ?? false;
    if (!isValid) {
      setState(() {
        _ratingError = _rating == 0 ? 'Odaberite ocjenu od 1 do 5 zvjezdica.' : null;
      });
      return;
    }

    setState(() => _ratingError = null);
    setState(() => _isSubmitting = true);

    try {
      final provider = Provider.of<ReviewProvider>(context, listen: false);
      Review? result;

      if (_isEditing) {
        result = await provider.updateReview(
          reviewId: widget.existingReview!.id!,
          appointmentId: widget.appointment.id!,
          barberId: widget.appointment.barberId,
          rating: _rating,
          comment: _commentController.text.trim(),
        );
      } else {
        result = await provider.createReview(
          appointmentId: widget.appointment.id!,
          barberId: widget.appointment.barberId,
          rating: _rating,
          comment: _commentController.text.trim(),
        );
      }

      if (result != null && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              _isEditing
                  ? 'Recenzija za uslugu "${widget.appointment.service?.name ?? 'Usluga'}" je uspješno ažurirana.'
                  : 'Recenzija za uslugu "${widget.appointment.service?.name ?? 'Usluga'}" je uspješno kreirana.',
            ),
            backgroundColor: Colors.green,
          ),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString().replaceAll('Exception: ', '')),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_isEditing ? 'Uredi recenziju' : 'Ostavi recenziju'),
      ),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [

            Card(
              elevation: 2,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: Padding(
                padding: EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      widget.appointment.service?.name ?? 'Usluga',
                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                    ),
                    SizedBox(height: 8),
                    Row(
                      children: [
                        Icon(Icons.store, size: 16, color: Colors.grey),
                        SizedBox(width: 4),
                        Text(widget.appointment.salon?.name ?? 'Salon'),
                      ],
                    ),
                    SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(Icons.person, size: 16, color: Colors.grey),
                        SizedBox(width: 4),
                        Text(widget.appointment.barber?.username ?? 'Frizer'),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            SizedBox(height: 24),


            Text(
              'Vaša ocjena',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(5, (index) {
                  return GestureDetector(
                    onTap: () => setState(() {
                      _rating = index + 1;
                      _ratingError = null;
                    }),
                    child: Padding(
                      padding: EdgeInsets.symmetric(horizontal: 4),
                      child: Icon(
                        index < _rating ? Icons.star : Icons.star_border,
                        size: 44,
                        color: index < _rating ? Colors.amber : Colors.grey[400],
                      ),
                    ),
                  );
                }),
              ),
              if (_ratingError != null) ...[
                SizedBox(height: 8),
                Center(
                  child: Text(
                    _ratingError!,
                    style: TextStyle(color: Theme.of(context).colorScheme.error, fontSize: 12),
                  ),
                ),
              ],
            SizedBox(height: 8),
            Center(
              child: Text(
                _rating == 0
                    ? 'Dodirnite zvjezdicu za ocjenu'
                    : _getRatingText(_rating),
                style: TextStyle(
                  color: _rating == 0 ? Colors.grey : Colors.amber.shade800,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
            SizedBox(height: 24),


            Text(
              'Vaš komentar',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 8),
            TextFormField(
              controller: _commentController,
              maxLines: 5,
              maxLength: 500,
              decoration: InputDecoration(
                hintText: 'Opišite vaše iskustvo (min. 10 znakova)...',
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                filled: true,
                fillColor: Colors.grey[50],
              ),
              validator: (value) {
                final comment = value?.trim() ?? '';
                if (comment.length < 10) {
                  return 'Komentar mora imati najmanje 10 znakova.';
                }
                return null;
              },
            ),
            SizedBox(height: 24),


            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton(
                onPressed: _isSubmitting ? null : _submitReview,
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.blue,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                child: _isSubmitting
                    ? SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                        ),
                      )
                    : Text(
                        _isEditing ? 'Ažuriraj recenziju' : 'Pošalji recenziju',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                      ),
              ),
            ),
            ],
          ),
        ),
      ),
    );
  }

  String _getRatingText(int rating) {
    switch (rating) {
      case 1: return 'Loše';
      case 2: return 'Ispod prosjeka';
      case 3: return 'Prosječno';
      case 4: return 'Dobro';
      case 5: return 'Odlično!';
      default: return '';
    }
  }
}
