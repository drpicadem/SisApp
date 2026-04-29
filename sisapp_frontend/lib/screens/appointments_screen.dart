import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'dart:async';
import '../providers/appointment_provider.dart';
import '../providers/auth_provider.dart';
import '../models/appointment.dart';
import 'appointment_details_screen.dart';
import 'review_form_screen.dart';
import '../utils/error_mapper.dart';

class AppointmentsScreen extends StatefulWidget {
  final int? initialTab;
  final int? focusAppointmentId;

  const AppointmentsScreen({Key? key, this.initialTab, this.focusAppointmentId}) : super(key: key);

  @override
  _AppointmentsScreenState createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final ScrollController _scrollController = ScrollController();
  final Map<int, GlobalKey> _appointmentCardKeys = {};
  bool? _isPaidFilter;
  bool _initialTabApplied = false;
  bool _initialFocusApplied = false;
  bool _focusHighlightVisible = true;
  bool _focusHighlightTimerStarted = false;
  bool _didAutoScrollToFocus = false;

  @override
  void initState() {
    super.initState();
    initializeDateFormatting('bs');
    _tabController = TabController(length: 2, vsync: this);
    _tabController.addListener(_onTabChanged);
    _scrollController.addListener(_onScroll);
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_initialTabApplied) return;

    final args = ModalRoute.of(context)?.settings.arguments;
    int initialTab = widget.initialTab ?? 0;
    if (widget.initialTab == null && args is Map && args['initialTab'] is int) {
      initialTab = args['initialTab'] as int;
    }
    if (initialTab < 0 || initialTab > 1) {
      initialTab = 0;
    }

    _tabController.index = initialTab;
    _initialTabApplied = true;
    _startFocusHighlightTimerIfNeeded();
    Future.microtask(() => _fetchAppointments());
  }

  int? _resolveFocusAppointmentId() {
    if (widget.focusAppointmentId != null) {
      return widget.focusAppointmentId;
    }

    final args = ModalRoute.of(context)?.settings.arguments;
    if (args is Map && args['focusAppointmentId'] is int) {
      return args['focusAppointmentId'] as int;
    }

    return null;
  }

  GlobalKey _getAppointmentCardKey(int appointmentId) {
    return _appointmentCardKeys.putIfAbsent(appointmentId, () => GlobalKey());
  }

  void _startFocusHighlightTimerIfNeeded() {
    final focusId = _resolveFocusAppointmentId();
    if (focusId == null || _focusHighlightTimerStarted) {
      return;
    }

    _focusHighlightTimerStarted = true;
    Timer(const Duration(seconds: 3), () {
      if (!mounted) return;
      setState(() {
        _focusHighlightVisible = false;
      });
    });
  }

  void _onTabChanged() {
    if (_tabController.indexIsChanging) {
      _fetchAppointments();
    }
  }

  void _onScroll() {
    if (_scrollController.position.pixels >= _scrollController.position.maxScrollExtent - 200) {
      final provider = Provider.of<AppointmentProvider>(context, listen: false);
      if (provider.hasMore && !provider.isLoadingMore) {
        _fetchAppointments(refresh: false);
      }
    }
  }

  void _fetchAppointments({bool refresh = true}) {
    final isActive = _tabController.index == 0;
    int? prioritizeAppointmentId;
    if (refresh && !_initialFocusApplied) {
      final args = ModalRoute.of(context)?.settings.arguments;
      if (widget.focusAppointmentId != null) {
        prioritizeAppointmentId = widget.focusAppointmentId;
      } else if (args is Map && args['focusAppointmentId'] is int) {
        prioritizeAppointmentId = args['focusAppointmentId'] as int;
      }
      if (prioritizeAppointmentId != null) {
        _initialFocusApplied = true;
      }
    }

    Provider.of<AppointmentProvider>(context, listen: false).fetchAppointments(
      refresh: refresh,
      isActive: isActive,
      isPaid: _isPaidFilter,
      prioritizeAppointmentId: prioritizeAppointmentId,
    );
  }

  @override
  void dispose() {
    _tabController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  Color _getStatusColor(String? status) {
    switch (status) {
      case 'Confirmed': return Colors.green.shade800;
      case 'Pending': return Colors.orange.shade800;
      case 'Cancelled': return Colors.red.shade800;
      case 'Completed': return Colors.blue.shade800;
      case 'Paid': return Colors.green.shade800;
      default: return Colors.grey.shade800;
    }
  }

  Color _getStatusBgColor(String? status) {
    switch (status) {
      case 'Confirmed': return Colors.green.shade100;
      case 'Pending': return Colors.orange.shade100;
      case 'Cancelled': return Colors.red.shade100;
      case 'Completed': return Colors.blue.shade100;
      case 'Paid': return Colors.green.shade100;
      default: return Colors.grey.shade200;
    }
  }

  Future<void> _showCancelDialog(BuildContext context, int appointmentId) async {
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (BuildContext context) {
        return AlertDialog(
          title: Text('Otkazivanje termina'),
          content: SingleChildScrollView(
            child: ListBody(
              children: <Widget>[
                Text('Da li ste sigurni da želite otkazati ovaj termin?'),
                Text('Ova radnja se ne može poništiti.'),
              ],
            ),
          ),
          actions: <Widget>[
            TextButton(
              child: Text('Ne'),
              onPressed: () {
                Navigator.of(context).pop();
              },
            ),
            TextButton(
              child: Text('Da, otkaži', style: TextStyle(color: Colors.red)),
              onPressed: () async {
                Navigator.of(context).pop();
                try {
                  final success = await Provider.of<AppointmentProvider>(context, listen: false)
                      .cancelAppointment(appointmentId);
                  if (mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text(
                          success
                              ? 'Termin je uspješno otkazan.'
                              : 'Otkazivanje termina nije uspjelo. Provjerite status termina i pokušajte ponovo.',
                        ),
                      ),
                    );
                    if (success) _fetchAppointments();
                  }
                } catch (e) {
                  if (mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text(ErrorMapper.toUserMessage(e))),
                    );
                  }
                }
              },
            ),
          ],
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Moje Rezervacije'),
        bottom: TabBar(
          controller: _tabController,
          tabs: [
            Tab(text: 'Aktivne'),
            Tab(text: 'Historija'),
          ],
        ),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                   Text('Filter: ', style: TextStyle(fontWeight: FontWeight.bold)),
                   SizedBox(width: 8),
                   FilterChip(
                     label: Text('Sve'),
                     selected: _isPaidFilter == null,
                     onSelected: (selected) {
                       setState(() {
                         _isPaidFilter = null;
                       });
                       _fetchAppointments();
                     },
                   ),
                   SizedBox(width: 8),
                   FilterChip(
                     label: Text('Plaćeno'),
                     selected: _isPaidFilter == true,
                     onSelected: (selected) {
                       setState(() {
                         _isPaidFilter = selected ? true : null;
                       });
                       _fetchAppointments();
                     },
                   ),
                   SizedBox(width: 8),
                   FilterChip(
                     label: Text('Neplaćeno'),
                     selected: _isPaidFilter == false,
                     onSelected: (selected) {
                       setState(() {
                         _isPaidFilter = selected ? false : null;
                       });
                       _fetchAppointments();
                     },
                   ),
                ],
              ),
            ),
          ),

          Expanded(
            child: Consumer<AppointmentProvider>(
              builder: (context, provider, child) {
                if (provider.isLoading && provider.appointments.isEmpty) {
                  return Center(child: CircularProgressIndicator());
                }

                if (provider.appointments.isEmpty) {
                  return Center(child: Text('Nema rezervacija za odabrane filtere.'));
                }

                return ListView.builder(
                  controller: _scrollController,
                  itemCount: provider.appointments.length + (provider.hasMore ? 1 : 0),
                  itemBuilder: (context, index) {
                    if (index == provider.appointments.length) {
                      return Center(child: CircularProgressIndicator());
                    }

                    final appointment = provider.appointments[index];
                    final currencyFormatter = NumberFormat.currency(locale: 'bs', symbol: 'KM');

                    bool showHeader = false;
                    if (index == 0) {
                      showHeader = true;
                    } else {
                      final prevApp = provider.appointments[index - 1];
                      if (!_isSameDay(appointment.appointmentDateTime, prevApp.appointmentDateTime)) {
                        showHeader = true;
                      }
                    }

                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (showHeader)
                          _buildDateHeader(appointment.appointmentDateTime),

                        _buildAppointmentCard(appointment, currencyFormatter),
                      ],
                    );
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  bool _isSameDay(DateTime d1, DateTime d2) {
    return d1.year == d2.year && d1.month == d2.month && d1.day == d2.day;
  }

  Widget _buildDateHeader(DateTime date) {
    String dateStr;
    try {
      dateStr = DateFormat('EEEE, d. MMMM', 'bs').format(date);
    } catch (e) {
      dateStr = DateFormat('EEEE, d. MMMM').format(date);
    }
    if (dateStr.isNotEmpty) {
      dateStr = dateStr[0].toUpperCase() + dateStr.substring(1);
    }

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Text(
        dateStr,
        style: TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.bold,
          color: Colors.grey[800],
        ),
      ),
    );
  }

  Widget _buildAppointmentCard(Appointment appointment, NumberFormat currencyFormatter) {
    final focusAppointmentId = _resolveFocusAppointmentId();
    final isFocusedAppointment = focusAppointmentId != null && appointment.id == focusAppointmentId;
    final cardKey = appointment.id != null ? _getAppointmentCardKey(appointment.id!) : null;

    if (isFocusedAppointment && !_didAutoScrollToFocus && cardKey != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!mounted || _didAutoScrollToFocus) return;
        final ctx = cardKey.currentContext;
        if (ctx == null) return;
        _didAutoScrollToFocus = true;
        Scrollable.ensureVisible(
          ctx,
          duration: const Duration(milliseconds: 450),
          curve: Curves.easeOut,
          alignment: 0.1,
        );
      });
    }

    return AnimatedContainer(
      key: cardKey,
      duration: const Duration(milliseconds: 350),
      curve: Curves.easeInOut,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(12),
        border: isFocusedAppointment && _focusHighlightVisible
            ? Border.all(color: const Color(0xFF7B5EA7), width: 2)
            : null,
        boxShadow: isFocusedAppointment && _focusHighlightVisible
            ? [
                BoxShadow(
                  color: const Color(0xFF7B5EA7).withOpacity(0.22),
                  blurRadius: 14,
                  spreadRadius: 1,
                  offset: const Offset(0, 3),
                ),
              ]
            : null,
      ),
      child: Card(
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => AppointmentDetailsScreen(appointment: appointment),
            ),
          );
        },
        child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Column(
          children: [
            Row(
              children: [
                Container(
                  width: 60,
                  decoration: BoxDecoration(
                    color: Colors.grey[100],
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.grey.shade300),
                  ),
                  padding: EdgeInsets.symmetric(vertical: 8),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        DateFormat('MMM').format(appointment.appointmentDateTime).toUpperCase(),
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                          color: Colors.grey[600],
                        ),
                      ),
                      SizedBox(height: 4),
                      Text(
                        DateFormat('dd').format(appointment.appointmentDateTime),
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: Colors.black87,
                        ),
                      ),
                    ],
                  ),
                ),
                SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        appointment.service?.name ?? 'Usluga',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                      ),
                      SizedBox(height: 4),
                      Row(
                        children: [
                          Icon(Icons.store, size: 16, color: Colors.grey),
                          SizedBox(width: 4),
                          Flexible(
                            child: Text(
                              appointment.salon?.name ?? 'Salon',
                              style: TextStyle(color: Colors.grey[700]),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                      SizedBox(height: 4),
                      Row(
                        children: [
                          Icon(Icons.person, size: 16, color: Colors.grey),
                          SizedBox(width: 4),
                          Text(
                            appointment.barber?.username ?? "Nepoznato",
                            style: TextStyle(color: Colors.grey[700]),
                          ),
                        ],
                      ),
                      SizedBox(height: 4),
                      Row(
                        children: [
                          Icon(Icons.access_time, size: 16, color: Colors.grey),
                          SizedBox(width: 4),
                          Text(
                            DateFormat('HH:mm').format(appointment.appointmentDateTime),
                             style: TextStyle(color: Colors.grey[700]),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: _getStatusBgColor(appointment.status),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        appointment.status ?? 'Pending',
                        style: TextStyle(
                          color: _getStatusColor(appointment.status),
                          fontWeight: FontWeight.bold,
                          fontSize: 12,
                        ),
                      ),
                    ),
                    SizedBox(height: 4),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: _getStatusBgColor(appointment.paymentStatus),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Text(
                        appointment.paymentStatus == 'Paid' ? 'Plaćeno' : 'Neplaćeno',
                        style: TextStyle(
                          color: _getStatusColor(appointment.paymentStatus),
                          fontWeight: FontWeight.bold,
                          fontSize: 11,
                        ),
                      ),
                    ),
                    SizedBox(height: 8),
                    Text(
                      currencyFormatter.format(appointment.service?.price ?? 0),
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 14,
                      ),
                    ),
                  ],
                ),
              ],
            ),

            if (appointment.status == 'Pending' || appointment.status == 'Confirmed')
              Padding(
                padding: const EdgeInsets.only(top: 12.0),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    OutlinedButton.icon(
                      icon: Icon(Icons.cancel, color: Colors.red, size: 16),
                      label: Text('Otkaži', style: TextStyle(color: Colors.red)),
                      style: OutlinedButton.styleFrom(
                        side: BorderSide(color: Colors.red.shade200),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                      onPressed: () {
                         _showCancelDialog(context, appointment.id!);
                      },
                    ),
                  ],
                ),
              ),

            if ((appointment.status ?? '').toLowerCase() == 'completed' &&
                !context.read<AuthProvider>().isBarber)
              Padding(
                padding: const EdgeInsets.only(top: 8.0),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    OutlinedButton.icon(
                      icon: Icon(Icons.rate_review, color: Colors.amber.shade700, size: 16),
                      label: Text('Ostavi recenziju', style: TextStyle(color: Colors.amber.shade700)),
                      style: OutlinedButton.styleFrom(
                        side: BorderSide(color: Colors.amber.shade300),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                      onPressed: () async {
                        final result = await Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (context) => ReviewFormScreen(appointment: appointment),
                          ),
                        );
                        if (result == true) {
                          _fetchAppointments();
                        }
                      },
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    ),
  ),
);
  }
}
