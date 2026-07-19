import 'package:flutter/material.dart';

void showAppMessage(
  BuildContext context,
  String text, {
  bool isError = false,
}) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(text),
      backgroundColor:
          isError ? const Color(0xffbe123c) : const Color(0xff059669),
    ),
  );
}
