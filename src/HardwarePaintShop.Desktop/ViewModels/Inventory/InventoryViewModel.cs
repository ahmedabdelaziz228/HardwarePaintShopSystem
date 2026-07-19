using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace HardwarePaintShop.Desktop.ViewModels.Inventory;

/// <summary>
/// Coordinates the single-warehouse inventory workspace: balances, opening stock,
/// manual movements, movement history, and physical counts.
/// </summary>
public partial class InventoryViewModel : BaseViewModel
{
    private const string AllCategoriesLabel = "كل الفئات";
    private const string AllStatusesLabel = "كل الحالات";

    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;
    private readonly ILookupService _lookupService;
    private readonly IPartyService _partyService;

    [ObservableProperty] private ObservableCollection<StockBalanceItem> _allStock = new();
    [ObservableProperty] private ObservableCollection<StockBalanceItem> _stock = new();
    [ObservableProperty] private ObservableCollection<StockBalanceItem> _pagedStock = new();
    [ObservableProperty] private ObservableCollection<StockMovementListItem> _movements = new();
    [ObservableProperty] private ObservableCollection<InventoryCountListItem> _counts = new();
    [ObservableProperty] private ObservableCollection<InventoryCountLineEditor> _countLines = new();
    [ObservableProperty] private ObservableCollection<Category> _categories = new();
    [ObservableProperty] private ObservableCollection<Unit> _units = new();
    [ObservableProperty] private ObservableCollection<PriceGroup> _priceGroups = new();
    [ObservableProperty] private ObservableCollection<SupplierListItem> _suppliers = new();
    [ObservableProperty] private ObservableCollection<InventoryFilterOption> _categoryFilterOptions = new();
    [ObservableProperty] private ObservableCollection<string> _stockStatusOptions = new()
    {
        AllStatusesLabel, "متوفر", "منخفض", "نفد المخزون"
    };
    [ObservableProperty] private ObservableCollection<int> _pageSizeOptions = new() { 10, 20, 50, 100 };
    [ObservableProperty] private ObservableCollection<string> _adjustmentReasons = new()
    {
        "استلام رصيد بدون فاتورة", "تصحيح رصيد", "تالف أو فاقد", "مرتجع", "رصيد افتتاحي", "سبب آخر"
    };

    [ObservableProperty] private StockBalanceItem? _selectedProduct;
    [ObservableProperty] private string _search = string.Empty;
    [ObservableProperty] private Guid? _selectedCategoryFilterId;
    [ObservableProperty] private string _selectedStockStatus = AllStatusesLabel;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _selectedPageSize = 10;
    [ObservableProperty] private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _toDate = DateTime.Today;
    [ObservableProperty] private string _countNotes = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private string _currentMode = "list";

    // New product editor.
    [ObservableProperty] private string _newProductName = string.Empty;
    [ObservableProperty] private string _newProductCode = string.Empty;
    [ObservableProperty] private string _newBarcode = string.Empty;
    [ObservableProperty] private Guid? _newCategoryId;
    [ObservableProperty] private Guid? _newBaseUnitId;
    [ObservableProperty] private Guid? _newSupplierId;
    [ObservableProperty] private Guid? _newPriceGroupId;
    [ObservableProperty] private decimal _newOpeningQuantity;
    [ObservableProperty] private decimal _newMinimumQuantity = 5;
    [ObservableProperty] private decimal _newPurchaseCost;
    [ObservableProperty] private decimal _newSalePrice;
    [ObservableProperty] private string _newProductImagePath = string.Empty;
    [ObservableProperty] private bool _newProductIsActive = true;
    [ObservableProperty] private bool _newProductIsSerialTracked;
    [ObservableProperty] private bool _isProductSuccessVisible;
    [ObservableProperty] private Guid? _lastCreatedProductId;
    [ObservableProperty] private string _lastCreatedProductName = string.Empty;
    [ObservableProperty] private string _lastCreatedProductCode = string.Empty;
    [ObservableProperty] private decimal _lastCreatedOpeningQuantity;
    [ObservableProperty] private string _lastCreatedUnitName = string.Empty;

    // Manual movement editor.
    [ObservableProperty] private string _selectedAdjustmentType = "add";
    [ObservableProperty] private decimal _adjustmentQuantity;
    [ObservableProperty] private string _selectedAdjustmentReason = "استلام رصيد بدون فاتورة";
    [ObservableProperty] private string _adjustmentReferenceNo = string.Empty;
    [ObservableProperty] private string _adjustmentNotes = string.Empty;

    public bool CanCount { get; }
    public bool CanAdjust { get; }
    public bool CanCreateProduct { get; }
    public bool CanExport { get; }

    public bool IsListMode => CurrentMode == "list";
    public bool IsAddProductMode => CurrentMode == "add-product";
    public bool IsAdjustmentMode => CurrentMode == "adjustment";
    public bool IsMovementsMode => CurrentMode == "movements";
    public bool IsCountMode => CurrentMode == "count";
    public bool IsCountHistoryMode => CurrentMode == "count-history";

    public decimal TotalStockValue => AllStock.Sum(i => i.StockValue);
    public int LowStockCount => AllStock.Count(i => i.IsLowStock);
    public int ProductCount => AllStock.Count;
    public int CategoryCount => AllStock.Select(i => i.CategoryName).Distinct().Count();
    public decimal TotalDifference => CountLines.Sum(i => Math.Abs(i.Difference));
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)Stock.Count / SelectedPageSize));
    public string PageSummary => Stock.Count == 0
        ? "لا توجد نتائج"
        : $"عرض {(CurrentPage - 1) * SelectedPageSize + 1}–{Math.Min(CurrentPage * SelectedPageSize, Stock.Count)} من {Stock.Count}";
    public string PageNumberText => $"{CurrentPage} / {TotalPages}";
    public decimal NewProfitMargin => NewSalePrice <= 0
        ? 0
        : Math.Round((NewSalePrice - NewPurchaseCost) / NewSalePrice * 100, 1);
    public decimal AdjustmentPreviousBalance => SelectedProduct?.QuantityBase ?? 0;
    public bool IsAddAdjustment => SelectedAdjustmentType == "add";
    public bool IsRemoveAdjustment => SelectedAdjustmentType == "remove";
    public bool IsSetAdjustment => SelectedAdjustmentType == "set";
    public decimal AdjustmentDifference => SelectedAdjustmentType switch
    {
        "remove" => -AdjustmentQuantity,
        "set" => AdjustmentQuantity - AdjustmentPreviousBalance,
        _ => AdjustmentQuantity
    };
    public decimal AdjustmentNewBalance => AdjustmentPreviousBalance + AdjustmentDifference;
    public string AdjustmentUnitName => SelectedProduct?.BaseUnitName ?? string.Empty;

    public InventoryViewModel(
        IInventoryService inventoryService,
        IProductService productService,
        ILookupService lookupService,
        IPartyService partyService,
        IPermissionService permissionService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
        _lookupService = lookupService;
        _partyService = partyService;
        CanCount = permissionService.Can("Inventory.Count") && permissionService.Can("Inventory.Adjust");
        CanAdjust = permissionService.Can("Inventory.Adjust");
        CanCreateProduct = permissionService.Can("Product.Create");
        CanExport = permissionService.Can("Report.Export");
        _ = InitializeAsync();
    }

    partial void OnCurrentModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsListMode));
        OnPropertyChanged(nameof(IsAddProductMode));
        OnPropertyChanged(nameof(IsAdjustmentMode));
        OnPropertyChanged(nameof(IsMovementsMode));
        OnPropertyChanged(nameof(IsCountMode));
        OnPropertyChanged(nameof(IsCountHistoryMode));
    }

    partial void OnSearchChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryFilterIdChanged(Guid? value) => ApplyFilters();
    partial void OnSelectedStockStatusChanged(string value) => ApplyFilters();
    partial void OnSelectedPageSizeChanged(int value)
    {
        CurrentPage = 1;
        UpdatePage();
    }
    partial void OnNewPurchaseCostChanged(decimal value) => OnPropertyChanged(nameof(NewProfitMargin));
    partial void OnNewSalePriceChanged(decimal value) => OnPropertyChanged(nameof(NewProfitMargin));
    partial void OnSelectedAdjustmentTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsAddAdjustment));
        OnPropertyChanged(nameof(IsRemoveAdjustment));
        OnPropertyChanged(nameof(IsSetAdjustment));
        NotifyAdjustmentPreview();
    }
    partial void OnAdjustmentQuantityChanged(decimal value) => NotifyAdjustmentPreview();
    partial void OnSelectedProductChanged(StockBalanceItem? value) => NotifyAdjustmentPreview();

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
            BuildCategoryFilterOptions();
            ResetNewProductForm();
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
            var stockTask = _inventoryService.GetStockAsync();
            var movementTask = _inventoryService.GetMovementsAsync(SelectedProduct?.ProductId, FromDate, ToDate);
            var countsTask = _inventoryService.GetCountsAsync();
            await Task.WhenAll(stockTask, movementTask, countsTask);
            AllStock = new ObservableCollection<StockBalanceItem>(stockTask.Result);
            Movements = new ObservableCollection<StockMovementListItem>(movementTask.Result);
            Counts = new ObservableCollection<InventoryCountListItem>(countsTask.Result);
            ApplyFilters();
            NotifyInventorySummary();
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
    private void ShowList()
    {
        CurrentMode = "list";
        IsProductSuccessVisible = false;
    }

    [RelayCommand]
    private void ShowAddProduct()
    {
        if (!CanCreateProduct)
        {
            ShowError("ليس لديك صلاحية إضافة المنتجات.");
            return;
        }
        ResetNewProductForm();
        CurrentMode = "add-product";
    }

    [RelayCommand]
    private async Task ShowMovementsAsync()
    {
        CurrentMode = "movements";
        await FilterMovementsAsync();
    }

    [RelayCommand]
    private async Task OpenMovementsAsync(StockBalanceItem? item)
    {
        SelectedProduct = item;
        CurrentMode = "movements";
        await FilterMovementsAsync();
    }

    [RelayCommand]
    private void ShowCount() => CurrentMode = "count";

    [RelayCommand]
    private void ShowCountHistory() => CurrentMode = "count-history";

    [RelayCommand]
    private void OpenAdjustment(StockBalanceItem? item)
    {
        if (item is null)
            return;
        if (!CanAdjust)
        {
            ShowError("ليس لديك صلاحية تعديل المخزون.");
            return;
        }
        SelectedProduct = item;
        SelectedAdjustmentType = "add";
        AdjustmentQuantity = 0;
        SelectedAdjustmentReason = AdjustmentReasons.First();
        AdjustmentReferenceNo = string.Empty;
        AdjustmentNotes = string.Empty;
        StatusMessage = string.Empty;
        CurrentMode = "adjustment";
    }

    [RelayCommand]
    private void SetAdjustmentType(string type)
    {
        if (type is "add" or "remove" or "set")
            SelectedAdjustmentType = type;
    }

    [RelayCommand]
    private async Task SaveAdjustmentAsync()
    {
        if (!CanAdjust || SelectedProduct is null)
        {
            ShowError("اختر منتجًا ولديك صلاحية تعديل المخزون.");
            return;
        }
        if (AdjustmentNewBalance < 0)
        {
            ShowError($"لا يمكن أن يصبح الرصيد سالبًا. المتاح {AdjustmentPreviousBalance:0.###} {AdjustmentUnitName}.");
            return;
        }

        IsBusy = true;
        try
        {
            var productId = SelectedProduct.ProductId;
            var productName = SelectedProduct.ProductName;
            var unitName = SelectedProduct.BaseUnitName;
            var result = await _inventoryService.AdjustStockAsync(new StockAdjustmentRequest
            {
                ProductId = productId,
                AdjustmentType = SelectedAdjustmentType,
                QuantityBase = AdjustmentQuantity,
                Reason = SelectedAdjustmentReason,
                ReferenceNo = AdjustmentReferenceNo,
                Notes = AdjustmentNotes
            });
            await LoadAsync();
            SelectedProduct = AllStock.FirstOrDefault(i => i.ProductId == productId);
            ShowSuccess($"تم حفظ حركة {productName} بنجاح. الرصيد الجديد {result.NewBalance:0.###} {unitName}.");
            CurrentMode = "list";
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
    private async Task SaveNewProductAsync()
    {
        if (!CanCreateProduct)
        {
            ShowError("ليس لديك صلاحية إضافة المنتجات.");
            return;
        }
        if (string.IsNullOrWhiteSpace(NewProductName))
        {
            ShowError("اسم المنتج مطلوب.");
            return;
        }
        if (!NewBaseUnitId.HasValue)
        {
            ShowError("اختر الوحدة الأساسية.");
            return;
        }
        if (NewOpeningQuantity < 0 || NewMinimumQuantity < 0 || NewPurchaseCost < 0 || NewSalePrice < 0)
        {
            ShowError("الكميات والأسعار لا يمكن أن تكون سالبة.");
            return;
        }
        if (NewProductIsSerialTracked && NewOpeningQuantity > 0)
        {
            ShowError("المنتج المتتبع بالسيريال يجب إنشاؤه برصيد صفر.");
            return;
        }
        if (NewSalePrice > 0 && !NewPriceGroupId.HasValue)
        {
            ShowError("اختر فئة السعر لحفظ سعر البيع.");
            return;
        }

        IsBusy = true;
        try
        {
            var units = new[]
            {
                new ProductUnitInput(null, NewBaseUnitId.Value, 1, true, true)
            };
            var prices = NewSalePrice > 0 && NewPriceGroupId.HasValue
                ? new[]
                {
                    new ProductPriceInput(null, NewBaseUnitId.Value, NewPriceGroupId.Value,
                        NewSalePrice, NewSalePrice)
                }
                : Array.Empty<ProductPriceInput>();
            var barcodes = string.IsNullOrWhiteSpace(NewBarcode)
                ? Array.Empty<ProductBarcodeInput>()
                : new[]
                {
                    new ProductBarcodeInput(null, NewBaseUnitId.Value, NewBarcode.Trim())
                };
            var productId = await _productService.SaveAsync(new ProductSaveRequest
            {
                ProductCode = NewProductCode,
                Name = NewProductName,
                CategoryId = NewCategoryId,
                MainSupplierId = NewSupplierId,
                BaseUnitId = NewBaseUnitId.Value,
                ImagePath = NewProductImagePath,
                MinStockBaseQuantity = NewMinimumQuantity,
                IsSerialTracked = NewProductIsSerialTracked,
                IsActive = NewProductIsActive,
                OpeningQuantityBase = NewOpeningQuantity,
                OpeningCostBaseUnit = NewPurchaseCost,
                Units = units,
                Prices = prices,
                Barcodes = barcodes
            });

            LastCreatedProductId = productId;
            LastCreatedProductName = NewProductName.Trim();
            LastCreatedProductCode = string.IsNullOrWhiteSpace(NewProductCode) ? "بدون كود" : NewProductCode.Trim();
            LastCreatedOpeningQuantity = NewOpeningQuantity;
            LastCreatedUnitName = Units.FirstOrDefault(u => u.Id == NewBaseUnitId)?.Name ?? string.Empty;
            await LoadAsync();
            IsProductSuccessVisible = true;
            IsSuccess = true;
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
    private void AddAnotherProduct()
    {
        IsProductSuccessVisible = false;
        ResetNewProductForm();
        CurrentMode = "add-product";
    }

    [RelayCommand]
    private void ViewCreatedProduct()
    {
        IsProductSuccessVisible = false;
        CurrentMode = "list";
        if (LastCreatedProductId.HasValue)
            SelectedProduct = AllStock.FirstOrDefault(i => i.ProductId == LastCreatedProductId.Value);
    }

    [RelayCommand]
    private void CloseProductSuccess()
    {
        IsProductSuccessVisible = false;
        CurrentMode = "list";
    }

    [RelayCommand]
    private void BrowseNewProductImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر صورة المنتج",
            Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HardwarePaintShop", "Images", "Products");
            Directory.CreateDirectory(folder);
            var destination = Path.Combine(folder,
                $"{Guid.NewGuid():N}{Path.GetExtension(dialog.FileName).ToLowerInvariant()}");
            File.Copy(dialog.FileName, destination, false);
            NewProductImagePath = destination;
        }
        catch (Exception ex)
        {
            ShowError($"تعذر حفظ الصورة: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearNewProductImage() => NewProductImagePath = string.Empty;

    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage >= TotalPages)
            return;
        CurrentPage++;
        UpdatePage();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage <= 1)
            return;
        CurrentPage--;
        UpdatePage();
    }

    [RelayCommand]
    private void ExportStock()
    {
        if (!CanExport)
        {
            ShowError("ليس لديك صلاحية تصدير التقارير.");
            return;
        }
        var dialog = new SaveFileDialog
        {
            Title = "تصدير قائمة المخزون",
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"inventory-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            using var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true));
            writer.WriteLine("الكود,المنتج,التصنيف,الوحدة,الكمية,الحد الأدنى,الحالة,متوسط التكلفة,القيمة,سعر البيع,المورد");
            foreach (var item in Stock)
            {
                writer.WriteLine(string.Join(",", new[]
                {
                    Csv(item.ProductCode), Csv(item.ProductName), Csv(item.CategoryName), Csv(item.BaseUnitName),
                    item.QuantityBase.ToString("0.###"), item.MinimumQuantity.ToString("0.###"), Csv(item.StockStatus),
                    item.AverageCost.ToString("0.00"), item.StockValue.ToString("0.00"),
                    item.SalePrice?.ToString("0.00") ?? string.Empty, Csv(item.MainSupplierName)
                }));
            }
            ShowSuccess($"تم تصدير {Stock.Count} صنف بنجاح.");
        }
        catch (Exception ex)
        {
            ShowError($"تعذر تصدير الملف: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task FilterMovementsAsync()
    {
        try
        {
            Movements = new ObservableCollection<StockMovementListItem>(
                await _inventoryService.GetMovementsAsync(SelectedProduct?.ProductId, FromDate, ToDate));
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PrepareCountAsync()
    {
        IsBusy = true;
        try
        {
            var source = Stock.Count > 0 ? Stock : AllStock;
            var lines = source.Select(i => new InventoryCountLineEditor(i)).ToList();
            foreach (var line in lines)
                line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(TotalDifference));
            CountLines = new ObservableCollection<InventoryCountLineEditor>(lines);
            CountNotes = string.Empty;
            ShowSuccess("تم تحميل الرصيد الحالي. أدخل الكمية الفعلية ثم رحّل الجرد.");
            OnPropertyChanged(nameof(TotalDifference));
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
    private async Task PostCountAsync()
    {
        if (!CanCount)
        {
            ShowError("ليس لديك صلاحية الجرد والتسوية.");
            return;
        }
        if (CountLines.Count == 0)
        {
            ShowError("حمّل ورقة الجرد أولًا.");
            return;
        }
        IsBusy = true;
        try
        {
            var id = await _inventoryService.PostCountAsync(new InventoryCountRequest
            {
                CountScope = string.IsNullOrWhiteSpace(Search) ? "full" : "product",
                Notes = CountNotes,
                Items = CountLines.Select(i => new InventoryCountLineInput(
                    i.ProductId, i.SystemQuantity, i.ActualQuantity, i.Notes)).ToList()
            });
            ShowSuccess($"تم حفظ وترحيل الجرد بنجاح ({id.ToString()[..8]}).");
            CountLines.Clear();
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

    private void ApplyFilters()
    {
        IEnumerable<StockBalanceItem> result = AllStock;
        var term = Search.Trim();
        if (term.Length > 0)
        {
            result = result.Where(i =>
                i.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (i.ProductCode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.PrimaryBarcode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (SelectedCategoryFilterId.HasValue)
            result = result.Where(i => i.CategoryId == SelectedCategoryFilterId.Value);
        if (!string.IsNullOrWhiteSpace(SelectedStockStatus) && SelectedStockStatus != AllStatusesLabel)
            result = result.Where(i => i.StockStatus == SelectedStockStatus);

        Stock = new ObservableCollection<StockBalanceItem>(result.OrderBy(i => i.ProductName));
        CurrentPage = 1;
        UpdatePage();
    }

    private void UpdatePage()
    {
        if (CurrentPage > TotalPages)
            CurrentPage = TotalPages;
        PagedStock = new ObservableCollection<StockBalanceItem>(Stock
            .Skip((CurrentPage - 1) * SelectedPageSize)
            .Take(SelectedPageSize));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageSummary));
        OnPropertyChanged(nameof(PageNumberText));
    }

    private void BuildCategoryFilterOptions()
    {
        CategoryFilterOptions.Clear();
        CategoryFilterOptions.Add(new InventoryFilterOption(null, AllCategoriesLabel));
        foreach (var category in Categories.OrderBy(c => c.Name))
            CategoryFilterOptions.Add(new InventoryFilterOption(category.Id, category.Name));
    }

    private void ResetNewProductForm()
    {
        NewProductName = string.Empty;
        NewProductCode = string.Empty;
        NewBarcode = string.Empty;
        NewCategoryId = null;
        NewBaseUnitId = Units.FirstOrDefault()?.Id;
        NewSupplierId = null;
        NewPriceGroupId = PriceGroups.FirstOrDefault()?.Id;
        NewOpeningQuantity = 0;
        NewMinimumQuantity = 5;
        NewPurchaseCost = 0;
        NewSalePrice = 0;
        NewProductImagePath = string.Empty;
        NewProductIsActive = true;
        NewProductIsSerialTracked = false;
        StatusMessage = string.Empty;
    }

    private void NotifyInventorySummary()
    {
        OnPropertyChanged(nameof(TotalStockValue));
        OnPropertyChanged(nameof(LowStockCount));
        OnPropertyChanged(nameof(ProductCount));
        OnPropertyChanged(nameof(CategoryCount));
    }

    private void NotifyAdjustmentPreview()
    {
        OnPropertyChanged(nameof(AdjustmentPreviousBalance));
        OnPropertyChanged(nameof(AdjustmentDifference));
        OnPropertyChanged(nameof(AdjustmentNewBalance));
        OnPropertyChanged(nameof(AdjustmentUnitName));
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

    private static string Csv(string? value)
        => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}

public sealed record InventoryFilterOption(Guid? Id, string Name);

public partial class InventoryCountLineEditor : ObservableObject
{
    [ObservableProperty] private decimal _actualQuantity;
    [ObservableProperty] private string _notes = string.Empty;
    public Guid ProductId { get; }
    public string? ProductCode { get; }
    public string ProductName { get; }
    public string BaseUnitName { get; }
    public decimal SystemQuantity { get; }
    public decimal Difference => ActualQuantity - SystemQuantity;

    public InventoryCountLineEditor(StockBalanceItem item)
    {
        ProductId = item.ProductId;
        ProductCode = item.ProductCode;
        ProductName = item.ProductName;
        BaseUnitName = item.BaseUnitName;
        SystemQuantity = item.QuantityBase;
        _actualQuantity = item.QuantityBase;
    }

    partial void OnActualQuantityChanged(decimal value) => OnPropertyChanged(nameof(Difference));
}
