import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/app_message.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/repositories/products_repository.dart';
import 'package:hardware_paint_shop_mobile/features/products/logic/product_form_cubit.dart';
import 'package:image_picker/image_picker.dart';

class AddProductPage extends StatelessWidget {
  const AddProductPage({super.key});

  @override
  Widget build(BuildContext context) => BlocProvider(
        create: (context) => ProductFormCubit(
          context.read<ProductsRepository>(),
        )..loadLookups(),
        child: const _AddProductView(),
      );
}

class _AddProductView extends StatefulWidget {
  const _AddProductView();

  @override
  State<_AddProductView> createState() => _AddProductViewState();
}

class _AddProductViewState extends State<_AddProductView> {
  final _nameController = TextEditingController();
  final _codeController = TextEditingController();
  final _barcodeController = TextEditingController();
  final _priceController = TextEditingController();
  final _minimumController = TextEditingController(text: '0');
  String? _unitId;
  String? _categoryId;
  String? _priceGroupId;
  XFile? _image;
  bool _serialTracked = false;

  @override
  void dispose() {
    _nameController.dispose();
    _codeController.dispose();
    _barcodeController.dispose();
    _priceController.dispose();
    _minimumController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      BlocConsumer<ProductFormCubit, ProductFormState>(
        listenWhen: (previous, current) =>
            previous.message != current.message && current.message != null,
        listener: (context, state) {
          showAppMessage(
            context,
            state.message!,
            isError: state.status == ViewStatus.failure,
          );
          if (state.saved) Navigator.pop(context, true);
        },
        builder: (context, state) {
          final lookups = state.lookups;
          _unitId ??= lookups.units.isEmpty ? null : lookups.units.first.id;
          _priceGroupId ??= lookups.priceGroups.isEmpty
              ? null
              : lookups.priceGroups.first.id;
          final isBusy = state.status == ViewStatus.loading;
          return Scaffold(
            appBar: AppBar(title: const Text('إضافة منتج')),
            body: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                if (state.status == ViewStatus.failure && state.message != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: ErrorBanner(state.message!),
                  ),
                TextField(
                  controller: _nameController,
                  decoration: const InputDecoration(labelText: 'اسم المنتج *'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: _codeController,
                  decoration: const InputDecoration(labelText: 'الكود الداخلي'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: _barcodeController,
                  textDirection: TextDirection.ltr,
                  decoration: const InputDecoration(labelText: 'الباركود'),
                ),
                const SizedBox(height: 10),
                DropdownButtonFormField<String>(
                  initialValue: _unitId,
                  decoration: const InputDecoration(
                    labelText: 'الوحدة الأساسية *',
                  ),
                  items: lookups.units
                      .map(
                        (item) => DropdownMenuItem(
                          value: item.id,
                          child: Text(item.name),
                        ),
                      )
                      .toList(),
                  onChanged: (value) => setState(() => _unitId = value),
                ),
                const SizedBox(height: 10),
                DropdownButtonFormField<String>(
                  initialValue: _categoryId,
                  decoration: const InputDecoration(labelText: 'التصنيف'),
                  items: lookups.categories
                      .map(
                        (item) => DropdownMenuItem(
                          value: item.id,
                          child: Text(item.name),
                        ),
                      )
                      .toList(),
                  onChanged: (value) => setState(() => _categoryId = value),
                ),
                const SizedBox(height: 10),
                DropdownButtonFormField<String>(
                  initialValue: _priceGroupId,
                  decoration: const InputDecoration(labelText: 'فئة السعر'),
                  items: lookups.priceGroups
                      .map(
                        (item) => DropdownMenuItem(
                          value: item.id,
                          child: Text(item.name),
                        ),
                      )
                      .toList(),
                  onChanged: (value) => setState(() => _priceGroupId = value),
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _priceController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'سعر البيع'),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: TextField(
                        controller: _minimumController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'حد المخزون'),
                      ),
                    ),
                  ],
                ),
                SwitchListTile(
                  value: _serialTracked,
                  onChanged: (value) =>
                      setState(() => _serialTracked = value),
                  title: const Text('المنتج له سيريال نمبر'),
                ),
                OutlinedButton.icon(
                  onPressed: _pickImage,
                  icon: const Icon(Icons.camera_alt_outlined),
                  label: Text(
                    _image == null ? 'تصوير المنتج' : 'تم اختيار الصورة',
                  ),
                ),
                const SizedBox(height: 18),
                FilledButton(
                  onPressed: isBusy || _unitId == null ? null : _save,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 13),
                    child: Text(isBusy ? 'جارٍ الحفظ...' : 'حفظ المنتج'),
                  ),
                ),
              ],
            ),
          );
        },
      );

  Future<void> _pickImage() async {
    try {
      final picked = await ImagePicker().pickImage(
        source: ImageSource.camera,
        imageQuality: 78,
        maxWidth: 1600,
      );
      if (picked != null && mounted) setState(() => _image = picked);
    } catch (error) {
      if (mounted) showAppMessage(context, '$error', isError: true);
    }
  }

  void _save() {
    context.read<ProductFormCubit>().create(
          name: _nameController.text,
          code: _codeController.text,
          barcode: _barcodeController.text,
          baseUnitId: _unitId!,
          categoryId: _categoryId,
          priceGroupId: _priceGroupId,
          salePrice: double.tryParse(_priceController.text),
          minimumStock: double.tryParse(_minimumController.text) ?? 0,
          serialTracked: _serialTracked,
          imagePath: _image?.path,
        );
  }
}
