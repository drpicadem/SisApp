class ErrorMapper {
  static String toUserMessage(
    Object error, {
    String fallback = 'Operacija nije uspjela. Pokušajte ponovo.',
  }) {
    final raw = error.toString().replaceAll('Exception: ', '').trim();
    if (raw.isEmpty) return fallback;

    final lower = raw.toLowerCase();
    if (lower.contains('socketexception') || lower.contains('connection refused')) {
      return 'Nije moguće povezati se na server. Provjerite internet i pokušajte ponovo.';
    }
    if (lower.contains('timeout')) {
      return 'Zahtjev je istekao. Pokušajte ponovo.';
    }
    if (lower.contains('unauthorized') || lower.contains('401')) {
      return 'Sesija je istekla. Prijavite se ponovo.';
    }
    if (lower.contains('forbidden') || lower.contains('403')) {
      return 'Nemate dozvolu za ovu akciju.';
    }
    if (lower.contains('404') || lower.contains('not found')) {
      return 'Traženi resurs nije pronađen.';
    }
    if (lower.contains('500') || lower.contains('internal server error')) {
      return 'Došlo je do greške na serveru. Pokušajte ponovo kasnije.';
    }

    return raw;
  }
}
