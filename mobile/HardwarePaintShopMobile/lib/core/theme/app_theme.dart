import 'package:flutter/material.dart';

/// Central application theme. Feature-specific design tokens move here in Phase 4.
abstract final class AppTheme {
  static final light = ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(
      seedColor: const Color(0xff2563eb),
      brightness: Brightness.light,
    ),
    scaffoldBackgroundColor: const Color(0xfff5f7fb),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide.none,
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: Color(0xffe2e8f0)),
      ),
    ),
  );
}
