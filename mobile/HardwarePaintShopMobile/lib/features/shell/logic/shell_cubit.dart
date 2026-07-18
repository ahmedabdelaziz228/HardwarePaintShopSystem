import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/features/shell/data/models/shell_destination.dart';

/// Holds bottom-navigation state independently from the shell widget.
class ShellCubit extends Cubit<ShellDestination> {
  ShellCubit() : super(ShellDestination.dashboard);

  void selectPage(int index) {
    final safeIndex =
        index.clamp(0, ShellDestination.values.length - 1).toInt();
    emit(ShellDestination.values[safeIndex]);
  }
}
