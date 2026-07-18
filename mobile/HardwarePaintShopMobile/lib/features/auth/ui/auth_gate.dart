import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/auth/ui/login_page.dart';
import 'package:hardware_paint_shop_mobile/features/shell/ui/home_shell.dart';

/// Switches the root UI from the immutable authentication state.
class AuthGate extends StatelessWidget {
  const AuthGate({super.key});

  @override
  Widget build(BuildContext context) => BlocBuilder<AuthCubit, AuthState>(
        builder: (context, state) {
          if (!state.initialized || state.busy && state.userName.isEmpty) {
            return const SplashPage();
          }
          return state.authenticated ? const HomeShell() : const LoginPage();
        },
      );
}

class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) => const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
}
