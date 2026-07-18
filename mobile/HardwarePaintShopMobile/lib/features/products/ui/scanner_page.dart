import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

class ScannerPage extends StatefulWidget {
  const ScannerPage({super.key});

  @override
  State<ScannerPage> createState() => _ScannerPageState();
}

class _ScannerPageState extends State<ScannerPage> {
  bool _returned = false;

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('مسح الباركود')),
        body: Stack(
          children: [
            MobileScanner(
              onDetect: (capture) {
                if (_returned || capture.barcodes.isEmpty) return;
                final value = capture.barcodes.first.rawValue;
                if (value != null && value.isNotEmpty) {
                  _returned = true;
                  Navigator.pop(context, value);
                }
              },
            ),
            Center(
              child: Container(
                width: 260,
                height: 170,
                decoration: BoxDecoration(
                  border: Border.all(color: Colors.white, width: 3),
                  borderRadius: BorderRadius.circular(18),
                ),
              ),
            ),
          ],
        ),
      );
}
