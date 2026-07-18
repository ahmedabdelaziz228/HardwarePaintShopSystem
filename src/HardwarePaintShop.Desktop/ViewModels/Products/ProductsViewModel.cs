using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Application.Security;
using HardwarePaintShop.Desktop.ViewModels;
using HardwarePaintShop.Domain.Entities;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace HardwarePaintShop.Desktop.ViewModels.Products;

public partial class ProductsViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly ILookupService _lookupService;
    private readonly IPermissionService _permissionService;
    private readonly IPartyService _partyService;

    [ObservableProperty] private ObservableCollection<ProductListItem> _products = new();
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private ObservableCollection<Unit> _units = new();
    [ObservableProperty] private ObservableCollection<PriceGroup> _priceGroups = new();
    [ObservableProperty] private ObservableCollection<SupplierListItem> _suppliers = new();
    [ObservableProperty] private ObservableCollection<LookupOption> _categoryFilterOptions = new();
    [ObservableProperty] private ObservableCollection<LookupOption> _categoryFormOptions = new();
    [ObservableProperty] private ProductListItem? _selectedProduct;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Guid? _selectedCategoryId;
    [ObservableProperty] private bool _includeInactive;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private Guid? _editingId;
    [ObservableProperty] private string _formProductCode = string.Empty;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private Guid? _formCategoryId;
    [ObservableProperty] private Guid? _formMainSupplierId;
    [ObservableProperty] private Guid? _formBaseUnitId;
    [ObservableProperty] private decimal _formMinStockBaseQuantity;
    [ObservableProperty] private decimal _formOpeningQuantityBase;
    [ObservableProperty] private decimal _formOpeningCostBaseUnit;
    [ObservableProperty] private decimal _formCurrentStockBase;
    [ObservableProperty] private decimal _formLastPurchaseCostBase;
    [ObservableProperty] private decimal _formAverageCostBase;
    [ObservableProperty] private bool _formIsSerialTracked;
    [ObservableProperty] private bool _formIsActive = true;
    [ObservableProperty] private string _formImagePath = string.Empty;
    [ObservableProperty] private string _formNotes = string.Empty;
    [ObservableProperty] private ObservableCollection<ProductUnitEditorItem> _productUnits = new();
    [ObservableProperty] private ObservableCollection<ProductPriceEditorItem> _productPrices = new();
    [ObservableProperty] private ObservableCollection<ProductBarcodeEditorItem> _productBarcodes = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;

    public bool CanCreate { get; }
    public bool CanUpdate { get; }
    public bool CanDeactivate { get; }
    public bool IsNewProduct => !IsEditing;

    public ProductsViewModel(
        IProductService productService,
        ILookupService lookupService,
        IPartyService partyService,
        IPermissionService permissionService)
    {
        _productService = productService;
        _lookupService = lookupService;
        _permissionService = permissionService;
        _partyService = partyService;

        CanCreate = _permissionService.Can("Product.Create");
        CanUpdate = _permissionService.Can("Product.Update");
        CanDeactivate = _permissionService.Can("Product.Deactivate");
        _ = InitializeAsync();
    }

    partial void OnFormBaseUnitIdChanged(Guid? value)
    {
        if (!value.HasValue)
            return;

        var baseRow = ProductUnits.FirstOrDefault(u => u.UnitId == value.Value);
        if (baseRow is null)
        {
            ProductUnits.Insert(0, new ProductUnitEditorItem
            {
                UnitId = value.Value,
                ConversionFactorToBase = 1,
                IsDefaultPurchase = ProductUnits.All(u => !u.IsDefaultPurchase),
                IsDefaultSale = ProductUnits.All(u => !u.IsDefaultSale)
            });
        }
        else
        {
            baseRow.ConversionFactorToBase = 1;
        }
    }

    partial void OnIsEditingChanged(bool value)
        => OnPropertyChanged(nameof(IsNewProduct));

    [RelayCommand]
    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var categoriesTask = _lookupService.GetCategoriesAsync();
            var unitsTask = _lookupService.GetUnitsAsync();
            var priceGroupsTask = _lookupService.GetPriceGroupsAsync();
            var suppliersTask = _partyService.SearchSuppliersAsync(new PartySearchCriteria());
            await Task.WhenAll(categoriesTask, unitsTask, priceGroupsTask, suppliersTask);

            Categories = new ObservableCollection<Category>(categoriesTask.Result);
            Units = new ObservableCollection<Unit>(unitsTask.Result);
            PriceGroups = new ObservableCollection<PriceGroup>(priceGroupsTask.Result);
            Suppliers = new ObservableCollection<SupplierListItem>(suppliersTask.Result);
            BuildCategoryOptions();
            NewProduct();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _productService.SearchAsync(new ProductSearchCriteria(
                SearchText,
                SelectedCategoryId,
                IncludeInactive));
            Products = new ObservableCollection<ProductListItem>(result);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewProduct()
    {
        IsEditing = false;
        EditingId = null;
        SelectedProduct = null;
        FormProductCode = string.Empty;
        FormName = string.Empty;
        FormCategoryId = null;
        FormMainSupplierId = null;
        FormMinStockBaseQuantity = 0;
        FormOpeningQuantityBase = 0;
        FormOpeningCostBaseUnit = 0;
        FormCurrentStockBase = 0;
        FormLastPurchaseCostBase = 0;
        FormAverageCostBase = 0;
        FormIsSerialTracked = false;
        FormIsActive = true;
        FormImagePath = string.Empty;
        FormNotes = string.Empty;
        ProductUnits.Clear();
        ProductPrices.Clear();
        ProductBarcodes.Clear();
        FormBaseUnitId = null;
        FormBaseUnitId = Units.FirstOrDefault()?.Id;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task EditProductAsync(ProductListItem? item)
    {
        if (item is null)
            return;
        if (!CanUpdate)
        {
            ShowError("ليس لديك صلاحية تعديل المنتجات.");
            return;
        }

        IsBusy = true;
        try
        {
            var product = await _productService.GetAsync(item.Id);
            EditingId = product.Id;
            IsEditing = true;
            SelectedProduct = item;
            FormProductCode = product.ProductCode ?? string.Empty;
            FormName = product.Name;
            FormCategoryId = product.CategoryId;
            FormMainSupplierId = product.MainSupplierId;
            FormMinStockBaseQuantity = product.MinStockBaseQuantity;
            FormOpeningQuantityBase = 0;
            FormOpeningCostBaseUnit = 0;
            FormCurrentStockBase = product.StockBaseQuantity;
            FormLastPurchaseCostBase = product.LastPurchasePriceBaseUnit;
            FormAverageCostBase = product.AverageCostBaseUnit;
            FormIsSerialTracked = product.IsSerialTracked;
            FormIsActive = product.IsActive;
            FormImagePath = product.ImagePath ?? string.Empty;
            FormNotes = product.Notes ?? string.Empty;

            ProductUnits = new ObservableCollection<ProductUnitEditorItem>(product.Units
                .Where(u => u.IsActive)
                .Select(u => new ProductUnitEditorItem
                {
                    Id = u.Id,
                    UnitId = u.UnitId,
                    ConversionFactorToBase = u.ConversionFactorToBase,
                    IsDefaultPurchase = u.IsDefaultPurchase,
                    IsDefaultSale = u.IsDefaultSale
                }));
            ProductPrices = new ObservableCollection<ProductPriceEditorItem>(product.Prices
                .Where(p => ProductUnits.Any(u => u.UnitId == p.UnitId))
                .Select(p => new ProductPriceEditorItem
                {
                    Id = p.Id,
                    UnitId = p.UnitId,
                    PriceGroupId = p.PriceGroupId,
                    SalePrice = p.SalePrice,
                    MinSalePrice = p.MinSalePrice
                }));
            ProductBarcodes = new ObservableCollection<ProductBarcodeEditorItem>(product.Barcodes
                .Select(b => new ProductBarcodeEditorItem
                {
                    Id = b.Id,
                    UnitId = b.UnitId,
                    Barcode = b.Barcode
                }));
            FormBaseUnitId = product.BaseUnitId;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsEditing && !CanUpdate)
        {
            ShowError("ليس لديك صلاحية تعديل المنتجات.");
            return;
        }
        if (!IsEditing && !CanCreate)
        {
            ShowError("ليس لديك صلاحية إضافة المنتجات.");
            return;
        }
        if (string.IsNullOrWhiteSpace(FormName))
        {
            ShowError("اسم المنتج مطلوب.");
            return;
        }
        if (!FormBaseUnitId.HasValue)
        {
            ShowError("اختر الوحدة الأساسية.");
            return;
        }

        IsBusy = true;
        try
        {
            var request = new ProductSaveRequest
            {
                Id = EditingId,
                ProductCode = FormProductCode,
                Name = FormName,
                CategoryId = FormCategoryId,
                MainSupplierId = FormMainSupplierId,
                BaseUnitId = FormBaseUnitId.Value,
                MinStockBaseQuantity = FormMinStockBaseQuantity,
                OpeningQuantityBase = IsEditing ? 0 : FormOpeningQuantityBase,
                OpeningCostBaseUnit = IsEditing ? 0 : FormOpeningCostBaseUnit,
                IsSerialTracked = FormIsSerialTracked,
                IsActive = FormIsActive,
                ImagePath = FormImagePath,
                Notes = FormNotes,
                Units = ProductUnits.Select(u => new ProductUnitInput(
                    u.Id,
                    u.UnitId,
                    u.ConversionFactorToBase,
                    u.IsDefaultPurchase,
                    u.IsDefaultSale)).ToList(),
                Prices = ProductPrices.Select(p => new ProductPriceInput(
                    p.Id,
                    p.UnitId,
                    p.PriceGroupId,
                    p.SalePrice,
                    p.MinSalePrice)).ToList(),
                Barcodes = ProductBarcodes.Select(b => new ProductBarcodeInput(
                    b.Id,
                    b.UnitId,
                    b.Barcode)).ToList()
            };

            await _productService.SaveAsync(request);
            await LoadAsync();
            NewProduct();
            ShowSuccess("تم حفظ المنتج ووحداته وأسعاره بنجاح ✓");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(ProductListItem? item)
    {
        if (item is null)
            return;
        if (!CanDeactivate)
        {
            ShowError("ليس لديك صلاحية تعطيل أو تفعيل المنتجات.");
            return;
        }

        IsBusy = true;
        try
        {
            await _productService.SetActiveAsync(item.Id, !item.IsActive);
            ShowSuccess(item.IsActive ? "تم تعطيل المنتج." : "تم تفعيل المنتج.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddUnit()
    {
        var available = Units.FirstOrDefault(u => ProductUnits.All(pu => pu.UnitId != u.Id));
        if (available is null)
        {
            ShowError("تمت إضافة كل الوحدات المتاحة بالفعل.");
            return;
        }

        ProductUnits.Add(new ProductUnitEditorItem
        {
            UnitId = available.Id,
            ConversionFactorToBase = 1,
            IsDefaultPurchase = ProductUnits.All(u => !u.IsDefaultPurchase),
            IsDefaultSale = ProductUnits.All(u => !u.IsDefaultSale)
        });
    }

    [RelayCommand]
    private void RemoveUnit(ProductUnitEditorItem? item)
    {
        if (item is null)
            return;
        if (item.UnitId == FormBaseUnitId)
        {
            ShowError("لا يمكن حذف الوحدة الأساسية. اختر وحدة أساسية أخرى أولًا.");
            return;
        }

        ProductUnits.Remove(item);
        foreach (var price in ProductPrices.Where(p => p.UnitId == item.UnitId).ToList())
            ProductPrices.Remove(price);
        foreach (var barcode in ProductBarcodes.Where(b => b.UnitId == item.UnitId).ToList())
            ProductBarcodes.Remove(barcode);
    }

    [RelayCommand]
    private void AddPrice()
    {
        var unitId = ProductUnits.FirstOrDefault()?.UnitId;
        var groupId = PriceGroups.FirstOrDefault()?.Id;
        if (!unitId.HasValue || !groupId.HasValue)
        {
            ShowError("أضف وحدة وفئة سعر قبل إضافة السعر.");
            return;
        }

        var freePair = (from unit in ProductUnits
                        from priceGroup in PriceGroups
                        where ProductPrices.All(p =>
                            p.UnitId != unit.UnitId || p.PriceGroupId != priceGroup.Id)
                        select (unit.UnitId, priceGroup.Id)).FirstOrDefault();
        if (freePair == default)
        {
            ShowError("تمت إضافة كل تركيبات الوحدة وفئة السعر.");
            return;
        }

        ProductPrices.Add(new ProductPriceEditorItem
        {
            UnitId = freePair.UnitId,
            PriceGroupId = freePair.Id
        });
    }

    [RelayCommand]
    private void RemovePrice(ProductPriceEditorItem? item)
    {
        if (item is not null)
            ProductPrices.Remove(item);
    }

    [RelayCommand]
    private void AddBarcode()
        => ProductBarcodes.Add(new ProductBarcodeEditorItem());

    [RelayCommand]
    private void RemoveBarcode(ProductBarcodeEditorItem? item)
    {
        if (item is not null)
            ProductBarcodes.Remove(item);
    }

    [RelayCommand]
    private void BrowseImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر صورة المنتج",
            Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.webp|كل الملفات|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var imagesFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HardwarePaintShop",
                "Images",
                "Products");
            Directory.CreateDirectory(imagesFolder);
            var destination = Path.Combine(
                imagesFolder,
                $"{Guid.NewGuid():N}{Path.GetExtension(dialog.FileName).ToLowerInvariant()}");
            File.Copy(dialog.FileName, destination, overwrite: false);
            FormImagePath = destination;
            ShowSuccess("تم اختيار وحفظ صورة المنتج.");
        }
        catch (Exception ex)
        {
            ShowError($"تعذر حفظ الصورة: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearImage() => FormImagePath = string.Empty;

    private void BuildCategoryOptions()
    {
        CategoryFilterOptions.Clear();
        CategoryFormOptions.Clear();
        CategoryFilterOptions.Add(new LookupOption(null, "كل التصنيفات"));
        CategoryFormOptions.Add(new LookupOption(null, "— بدون تصنيف —"));
        foreach (var category in Categories.OrderBy(c => c.Name))
        {
            var option = new LookupOption(category.Id, category.Name);
            CategoryFilterOptions.Add(option);
            CategoryFormOptions.Add(option);
        }
    }

    private void ShowError(string message)
    {
        StatusMessage = message;
        IsSuccess = false;
    }

    private void ShowSuccess(string message)
    {
        StatusMessage = message;
        IsSuccess = true;
    }
}

public sealed record LookupOption(Guid? Id, string Name);

public partial class ProductUnitEditorItem : ObservableObject
{
    public Guid? Id { get; init; }
    [ObservableProperty] private Guid _unitId;
    [ObservableProperty] private decimal _conversionFactorToBase = 1;
    [ObservableProperty] private bool _isDefaultPurchase;
    [ObservableProperty] private bool _isDefaultSale;
}

public partial class ProductPriceEditorItem : ObservableObject
{
    public Guid? Id { get; init; }
    [ObservableProperty] private Guid _unitId;
    [ObservableProperty] private Guid _priceGroupId;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _minSalePrice;
}

public partial class ProductBarcodeEditorItem : ObservableObject
{
    public Guid? Id { get; init; }
    [ObservableProperty] private Guid? _unitId;
    [ObservableProperty] private string _barcode = string.Empty;
}
