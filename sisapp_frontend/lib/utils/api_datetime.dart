class ApiDateTime {
  static DateTime parse(dynamic value, {DateTime? fallbackUtc}) {
    final fallback = fallbackUtc ?? DateTime.fromMillisecondsSinceEpoch(0, isUtc: true);
    if (value == null) return fallback.toLocal();

    final raw = value.toString().trim();
    if (raw.isEmpty) return fallback.toLocal();

    final normalized = _ensureTimezone(raw);
    return DateTime.parse(normalized).toLocal();
  }

  static DateTime? parseNullable(dynamic value) {
    if (value == null) return null;
    final raw = value.toString().trim();
    if (raw.isEmpty) return null;
    return DateTime.parse(_ensureTimezone(raw)).toLocal();
  }

  static String toUtcIso(DateTime value) => value.toUtc().toIso8601String();

  static bool _hasTimezone(String s) {
    if (s.endsWith('Z')) return true;
    if (s.contains('+')) return true;
    return RegExp(r'-\d\d:\d\d$').hasMatch(s);
  }

  static String _ensureTimezone(String s) => _hasTimezone(s) ? s : '${s}Z';
}

