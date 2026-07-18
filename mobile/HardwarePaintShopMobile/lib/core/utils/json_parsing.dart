/// Defensive JSON number parsing shared by feature models.
double asDouble(dynamic value) =>
    value is num ? value.toDouble() : double.tryParse('$value') ?? 0;
