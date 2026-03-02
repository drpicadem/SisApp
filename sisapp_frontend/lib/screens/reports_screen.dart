import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:printing/printing.dart';
import 'package:pdf/pdf.dart';
import '../services/api_service.dart';
import '../providers/auth_provider.dart';

class ReportsScreen extends StatefulWidget {
  @override
  _ReportsScreenState createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  Map<String, dynamic>? _stats;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadStats();
  }

  Future<void> _loadStats() async {
    final token = context.read<AuthProvider>().tokenResponse?.token;
    if (token != null) {
       final stats = await ApiService().getReports(token);
       if (mounted) {
         setState(() {
           _stats = stats;
           _isLoading = false;
         });
       }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Izvještaji i Statistika'),
        actions: [
          IconButton(
            icon: Icon(Icons.print),
            tooltip: 'Preuzmi / Isprintaj PDF',
            onPressed: _isLoading ? null : () async {
              final token = context.read<AuthProvider>().tokenResponse?.token;
              if (token != null) {
                try {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Generišem PDF...')),
                  );
                  final pdfBytes = await ApiService().getReportsPdf(token);
                  if (pdfBytes != null) {
                    await Printing.layoutPdf(
                        onLayout: (PdfPageFormat format) async => pdfBytes);
                  } else {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text('Greška pri dohvaćanju PDF-a.')),
                    );
                  }
                } catch (e) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Došlo je do greške.')),
                  );
                }
              }
            },
          )
        ],
      ),
      body: _isLoading 
          ? Center(child: CircularProgressIndicator())
          : _stats == null 
              ? Center(child: Text('Greška pri učitavanju podataka.'))
              : SingleChildScrollView(
                  padding: EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Summary Cards
                      Row(
                        children: [
                          _buildStatCard('Ukupno Korisnika', _stats!['totalUsers'].toString(), Icons.people, Colors.blue),
                          SizedBox(width: 16),
                          _buildStatCard('Frizerski Saloni', _stats!['totalSalons'].toString(), Icons.store, Colors.orange),
                          SizedBox(width: 16),
                          _buildStatCard('Ukupno Frizeri', _stats!['totalBarbers'].toString(), Icons.content_cut, Colors.purple),
                        ],
                      ),
                      SizedBox(height: 32),
                      
                      // Chart Section
                      Text('Rast broja korisnika (po mjesecima)', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                      SizedBox(height: 16),
                      Container(
                        height: 300,
                        padding: EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          boxShadow: [BoxShadow(color: Colors.grey.withOpacity(0.1), blurRadius: 10)],
                        ),
                        child: LineChart(
                          LineChartData(
                             gridData: FlGridData(show: true),
                             titlesData: FlTitlesData(
                               bottomTitles: AxisTitles(
                                 sideTitles: SideTitles(showTitles: true, getTitlesWidget: (val, meta) {
                                   switch (val.toInt()) {
                                     case 1: return Text('Jan');
                                     case 2: return Text('Feb');
                                     case 3: return Text('Mar');
                                     case 6: return Text('Jun');
                                     case 12: return Text('Dec');
                                   }
                                   return Text('');
                                 }),
                               ),
                               leftTitles: AxisTitles(sideTitles: SideTitles(showTitles: true, reservedSize: 30)),
                               topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                               rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                             ),
                             borderData: FlBorderData(show: true, border: Border.all(color: const Color(0xff37434d))),
                             lineBarsData: [
                               LineChartBarData(
                                 spots: _getSpots(),
                                 isCurved: true,
                                 color: Colors.blue,
                                 barWidth: 4,
                                 isStrokeCapRound: true,
                                 dotData: FlDotData(show: true),
                                 belowBarData: BarAreaData(show: true, color: Colors.blue.withOpacity(0.2)),
                               ),
                             ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
    );
  }
  
  List<FlSpot> _getSpots() {
    List<dynamic> monthly = _stats!['monthlyRegistrations'] ?? [];
    List<FlSpot> spots = [];
    
    // Default 0 spots if empty
    if (monthly.isEmpty) {
       return [FlSpot(1, 0), FlSpot(12, 0)];
    }

    for (var m in monthly) {
      spots.add(FlSpot(m['month'].toDouble(), m['count'].toDouble()));
    }
    return spots;
  }

  Widget _buildStatCard(String title, String value, IconData icon, Color color) {
    return Expanded(
      child: Container(
        padding: EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          boxShadow: [BoxShadow(color: Colors.grey.withOpacity(0.1), blurRadius: 10)],
        ),
        child: Column(
          children: [
            Icon(icon, size: 40, color: color),
            SizedBox(height: 10),
            Text(value, style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
            Text(title, style: TextStyle(color: Colors.grey)),
          ],
        ),
      ),
    );
  }
}
