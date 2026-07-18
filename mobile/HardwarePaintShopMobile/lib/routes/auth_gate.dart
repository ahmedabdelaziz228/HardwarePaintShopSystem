import 'package:flutter/material.dart';
import 'package:hardware_paint_shop_mobile/app_state.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_controller.dart';
import 'package:hardware_paint_shop_mobile/features/auth/presentation/login_screen.dart';
import 'package:hardware_paint_shop_mobile/features/shell/presentation/home_shell.dart';
import 'package:provider/provider.dart';

/// Selects the startup screen from the current authentication state.
class AuthGate extends StatelessWidget {
  const AuthGate({super.key});

  @override
  Widget build(BuildContext context) => Consumer<AuthController>(
        builder: (context, auth, _) {
          if (!auth.initialized) return const SplashScreen();
          return auth.authenticated ? const _AuthenticatedShell() : const LoginScreen();
        },
      );
}

class _AuthenticatedShell extends StatefulWidget {
  const _AuthenticatedShell();

  @override
  State<_AuthenticatedShell> createState() => _AuthenticatedShellState();
}

class _AuthenticatedShellState extends State<_AuthenticatedShell> {
  @override
  void initState() {
    super.initState();
    final appState = context.read<AppState>();
    Future.microtask(() async {
      try {
        await appState.refreshHome();
      } catch (_) {
        // AppState exposes the failure while cached feature data remains usable.
      }
    });
  }

  @override
  Widget build(BuildContext context) => const HomeShell();
}

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) => const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
}
