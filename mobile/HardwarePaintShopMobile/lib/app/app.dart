import 'package:flutter/material.dart';
import 'package:hardware_paint_shop_mobile/core/theme/app_theme.dart';
import 'package:hardware_paint_shop_mobile/routes/auth_gate.dart';

/// Root Material application and global RTL/theme configuration.
class ShopMobileApp extends StatelessWidget {
  const ShopMobileApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
        debugShowCheckedModeBanner: false,
        title: 'إدارة المحل',
        locale: const Locale('ar'),
        theme: AppTheme.light,
        builder: (context, child) => Directionality(
          textDirection: TextDirection.rtl,
          child: child ?? const SizedBox.shrink(),
        ),
        home: const AuthGate(),
      );
}
