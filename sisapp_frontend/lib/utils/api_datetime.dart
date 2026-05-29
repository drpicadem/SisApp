import 'package:timezone/data/latest.dart' as tz_data;
import 'package:timezone/timezone.dart' as tz;

/// UTC in API/DB; salon wall-clock (Europe/Sarajevo) in UI and slot picker.
class ApiDateTime {
  static const salonTimeZoneId = 'Europe/Sarajevo';
  static bool _initialized = false;

  static void ensureInitialized() {
    if (_initialized) return;
    tz_data.initializeTimeZones();
    _initialized = true;
  }

  static tz.Location get _salonLocation {
    ensureInitialized();
    return tz.getLocation(salonTimeZoneId);
  }

  static DateTime salonToday() {
    final now = tz.TZDateTime.now(_salonLocation);
    return DateTime(now.year, now.month, now.day);
  }

  static DateTime salonLocalDateTime({
    required int year,
    required int month,
    required int day,
    required int hour,
    required int minute,
    int second = 0,
  }) {
    return DateTime(year, month, day, hour, minute, second);
  }

  static DateTime utcToSalonLocal(DateTime utc) {
    final salon = tz.TZDateTime.from(utc.toUtc(), _salonLocation);
    return DateTime(
      salon.year,
      salon.month,
      salon.day,
      salon.hour,
      salon.minute,
      salon.second,
      salon.millisecond,
      salon.microsecond,
    );
  }

  static DateTime salonLocalToUtc(DateTime salonLocal) {
    final salon = tz.TZDateTime(
      _salonLocation,
      salonLocal.year,
      salonLocal.month,
      salonLocal.day,
      salonLocal.hour,
      salonLocal.minute,
      salonLocal.second,
      salonLocal.millisecond,
      salonLocal.microsecond,
    );
    return salon.toUtc();
  }

  static DateTime parse(dynamic value, {DateTime? fallbackUtc}) {
    final fallback = fallbackUtc ?? DateTime.fromMillisecondsSinceEpoch(0, isUtc: true);
    if (value == null) return utcToSalonLocal(fallback);

    final raw = value.toString().trim();
    if (raw.isEmpty) return utcToSalonLocal(fallback);

    final normalized = _ensureTimezone(raw);
    return utcToSalonLocal(DateTime.parse(normalized).toUtc());
  }

  static DateTime? parseNullable(dynamic value) {
    if (value == null) return null;
    final raw = value.toString().trim();
    if (raw.isEmpty) return null;
    return utcToSalonLocal(DateTime.parse(_ensureTimezone(raw)).toUtc());
  }

  /// Naive [value] = salon wall-clock; already-UTC [value] stays UTC on wire.
  static String toUtcIso(DateTime value) {
    final utc = value.isUtc ? value : salonLocalToUtc(value);
    return utc.toIso8601String();
  }

  static bool _hasTimezone(String s) {
    if (s.endsWith('Z')) return true;
    if (s.contains('+')) return true;
    return RegExp(r'-\d\d:\d\d$').hasMatch(s);
  }

  static String _ensureTimezone(String s) => _hasTimezone(s) ? s : '${s}Z';
}
