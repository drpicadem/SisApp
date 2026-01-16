import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../providers/salon_provider.dart';
import '../providers/barber_provider.dart';
import '../providers/service_provider.dart';
import '../providers/booking_provider.dart';
import '../providers/auth_provider.dart';
import '../models/salon.dart';
import '../models/barber.dart';
import '../models/service.dart';
import '../models/appointment.dart';

class BookingScreen extends StatefulWidget {
  @override
  _BookingScreenState createState() => _BookingScreenState();
}

class _BookingScreenState extends State<BookingScreen> {
  Salon? _selectedSalon;
  Barber? _selectedBarber;
  Service? _selectedService;
  DateTime _selectedDate = DateTime.now();
  String? _selectedTimeSlot;

  bool _isInit = true;

  @override
  void didChangeDependencies() {
    if (_isInit) {
      final args = ModalRoute.of(context)?.settings.arguments as Map<String, dynamic>?;
      if (args != null && args.containsKey('salon')) {
        _selectedSalon = args['salon'] as Salon;
        // Load barbers/services for this salon immediately
        Future.microtask(() {
           if (mounted) {
             context.read<BarberProvider>().loadBarbers(_selectedSalon!.id);
             context.read<ServiceProvider>().loadServices(_selectedSalon!.id);
           }
        });
      }
      Future.microtask(() {
         if (mounted) context.read<SalonProvider>().loadSalons();
      });
      _isInit = false;
    }
    super.didChangeDependencies();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Nova Rezervacija')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
             _buildSalonDropdown(),
             SizedBox(height: 16),
             if (_selectedSalon != null) ...[
                _buildServiceDropdown(),
                SizedBox(height: 16),
                _buildBarberDropdown(),
                SizedBox(height: 16),
             ],
             if (_selectedBarber != null) ...[
                _buildDatePicker(),
                SizedBox(height: 16),
                _buildTimeSlots(),
                SizedBox(height: 32),
                _buildBookButton(),
             ]
          ],
        ),
      ),
    );
  }

  Widget _buildSalonDropdown() {
    return Consumer<SalonProvider>(
      builder: (context, salonProvider, child) {
        if (salonProvider.isLoading) return CircularProgressIndicator();
        return DropdownButtonFormField<Salon>(
          decoration: InputDecoration(labelText: 'Odaberite Salon', border: OutlineInputBorder()),
          value: _selectedSalon,
          items: salonProvider.salons.map((salon) {
            return DropdownMenuItem(value: salon, child: Text(salon.name));
          }).toList(),
          onChanged: (value) {
            if (value == null) return;
            
            setState(() {
              _selectedSalon = value;
              _selectedBarber = null;
              _selectedService = null;
              _selectedTimeSlot = null;
            });
            
             context.read<BarberProvider>().loadBarbers(value.id);
             context.read<ServiceProvider>().loadServices(value.id); 
            
          },
        );
      },
    );
  }

  Widget _buildServiceDropdown() {
      return Consumer<ServiceProvider>(
      builder: (context, serviceProvider, child) {
        return DropdownButtonFormField<Service>(
          decoration: InputDecoration(labelText: 'Odaberite Uslugu', border: OutlineInputBorder()),
          value: _selectedService,
          // IMPORTANT: Ensure _selectedService is actually in the list or null
          items: serviceProvider.services.map((service) {
            return DropdownMenuItem(value: service, child: Text('${service.name} (${service.price} KM)'));
          }).toList(),
          onChanged: (value) {
            setState(() {
              _selectedService = value;
            });
          },
        );
      },
    );
  }

  Widget _buildBarberDropdown() {
    return Consumer<BarberProvider>(
      builder: (context, barberProvider, child) {
        return DropdownButtonFormField<Barber>(
          decoration: InputDecoration(labelText: 'Odaberite Frizer', border: OutlineInputBorder()),
          value: _selectedBarber,
          items: barberProvider.barbers.map((barber) {
             return DropdownMenuItem(value: barber, child: Text('${barber.firstName} ${barber.lastName}'));
          }).toList(),
          onChanged: (value) {
             setState(() {
               _selectedBarber = value;
               _selectedTimeSlot = null;
             });
             if (value != null) {
                context.read<BookingProvider>().fetchAvailableSlots(value.id, _selectedDate);
             }
          },
        );
      },
    );
  }

  Widget _buildDatePicker() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Datum: ${DateFormat('dd.MM.yyyy').format(_selectedDate)}', style: TextStyle(fontSize: 16)),
        SizedBox(height: 8),
        ElevatedButton(
          child: Text('Promijeni datum'),
          onPressed: () async {
            final picked = await showDatePicker(
              context: context,
              initialDate: _selectedDate,
              firstDate: DateTime.now(),
              lastDate: DateTime.now().add(Duration(days: 30)),
            );
            if (picked != null && picked != _selectedDate) {
              setState(() {
                _selectedDate = picked;
                _selectedTimeSlot = null;
              });
              if (_selectedBarber != null) {
                 context.read<BookingProvider>().fetchAvailableSlots(_selectedBarber!.id, picked);
              }
            }
          },
        ),
      ],
    );
  }

  Widget _buildTimeSlots() {
     return Consumer<BookingProvider>(
       builder: (context, bookingProvider, child) {
         if (bookingProvider.isLoading) return Center(child: CircularProgressIndicator());
         
         if (bookingProvider.availableSlots.isEmpty) {
           return Text('Nema slobodnih termina za odabrani datum.');
         }

         return Wrap(
           spacing: 8.0,
           runSpacing: 8.0,
           children: bookingProvider.availableSlots.map((slot) {
             final isSelected = _selectedTimeSlot == slot;
             return ChoiceChip(
               label: Text(slot),
               selected: isSelected,
               onSelected: (selected) {
                 setState(() {
                   _selectedTimeSlot = selected ? slot : null;
                 });
               },
             );
           }).toList(),
         );
       },
     );
  }

  Widget _buildBookButton() {
     return SizedBox(
       width: double.infinity,
       child: ElevatedButton(
         child: Text('POTVRDI REZERVACIJU'),
         style: ElevatedButton.styleFrom(padding: EdgeInsets.symmetric(vertical: 16)),
         onPressed: (_selectedSalon != null && _selectedBarber != null && _selectedService != null && _selectedTimeSlot != null) 
          ? _confirmBooking 
          : null,
       ),
     );
  }

  void _confirmBooking() async {
    // Parse time
    final timeParts = _selectedTimeSlot!.split(':');
    final hour = int.parse(timeParts[0]);
    final minute = int.parse(timeParts[1]);
    
    final appointmentDateTime = DateTime(
      _selectedDate.year,
      _selectedDate.month,
      _selectedDate.day,
      hour,
      minute,
    );

    // Get current user ID (decode from token again or store in AuthProvider)
    // For now assuming AuthProvider has userId or we decode it. 
    // Actually AuthProvider doesn't have ID yet. We need it.
    // Let's check token decoding in AuthProvider. We only got role. 
    // We need 'nameid' or 'sub' for User ID.
    
    // TEMPORARY: I will fetch user Id from AuthProvider next.
    // For now, let's assume we can get it.
    
    final auth = context.read<AuthProvider>();
    if (auth.userId == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška: Korisnik nije prepoznat.')));
      return;
    }

    final appointment = Appointment(
      userId: auth.userId!,
      barberId: _selectedBarber!.id,
      serviceId: _selectedService!.id,
      salonId: _selectedSalon!.id,
      appointmentDateTime: appointmentDateTime,
      status: 'Pending',
    );

    final error = await context.read<BookingProvider>().createAppointment(appointment);

    if (error == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Rezervacija uspješna!')));
      Navigator.pop(context); // Return to home
    } else {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Greška: $error')));
    }
  }
}
