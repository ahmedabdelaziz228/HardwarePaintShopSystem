using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Inventory;

public partial class InventoryViewModel : BaseViewModel
{
    private readonly IInventoryService _inventoryService;
    [ObservableProperty] private ObservableCollection<StockBalanceItem> _stock = new();
    [ObservableProperty] private ObservableCollection<StockMovementListItem> _movements = new();
    [ObservableProperty] private ObservableCollection<InventoryCountListItem> _counts = new();
    [ObservableProperty] private ObservableCollection<InventoryCountLineEditor> _countLines = new();
    [ObservableProperty] private StockBalanceItem? _selectedProduct;
    [ObservableProperty] private string _search = string.Empty;
    [ObservableProperty] private bool _lowOnly;
    [ObservableProperty] private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _toDate = DateTime.Today;
    [ObservableProperty] private string _countNotes = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool CanCount { get; }
    public decimal TotalStockValue => Stock.Sum(i => i.StockValue);
    public int LowStockCount => Stock.Count(i => i.IsLowStock);
    public decimal TotalDifference => CountLines.Sum(i => Math.Abs(i.Difference));

    public InventoryViewModel(IInventoryService inventoryService, IPermissionService permissionService)
    {
        _inventoryService = inventoryService;
        CanCount = permissionService.Can("Inventory.Count") && permissionService.Can("Inventory.Adjust");
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var stockTask = _inventoryService.GetStockAsync(Search, LowOnly);
            var movementTask = _inventoryService.GetMovementsAsync(SelectedProduct?.ProductId, FromDate, ToDate);
            var countsTask = _inventoryService.GetCountsAsync();
            await Task.WhenAll(stockTask, movementTask, countsTask);
            Stock = new ObservableCollection<StockBalanceItem>(stockTask.Result);
            Movements = new ObservableCollection<StockMovementListItem>(movementTask.Result);
            Counts = new ObservableCollection<InventoryCountListItem>(countsTask.Result);
            OnPropertyChanged(nameof(TotalStockValue)); OnPropertyChanged(nameof(LowStockCount));
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task FilterMovementsAsync()
    {
        try { Movements = new ObservableCollection<StockMovementListItem>(await _inventoryService.GetMovementsAsync(SelectedProduct?.ProductId, FromDate, ToDate)); }
        catch (Exception ex) { SetError(ex.Message); }
    }

    [RelayCommand]
    private async Task PrepareCountAsync()
    {
        IsBusy = true;
        try
        {
            var stock = await _inventoryService.GetStockAsync(Search, false);
            var lines = stock.Select(i => new InventoryCountLineEditor(i)).ToList();
            foreach (var line in lines) line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(TotalDifference));
            CountLines = new ObservableCollection<InventoryCountLineEditor>(lines);
            CountNotes = string.Empty; StatusMessage = "تم تحميل الرصيد الحالي. أدخل الكمية الفعلية ثم رحّل الجرد.";
            OnPropertyChanged(nameof(TotalDifference));
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PostCountAsync()
    {
        if (!CanCount) { SetError("ليس لديك صلاحية الجرد والتسوية."); return; }
        if (CountLines.Count == 0) { SetError("حمّل ورقة الجرد أولًا."); return; }
        IsBusy = true;
        try
        {
            var id = await _inventoryService.PostCountAsync(new InventoryCountRequest
            {
                CountScope = string.IsNullOrWhiteSpace(Search) ? "full" : "product",
                Notes = CountNotes,
                Items = CountLines.Select(i => new InventoryCountLineInput(i.ProductId, i.SystemQuantity, i.ActualQuantity, i.Notes)).ToList()
            });
            StatusMessage = $"تم حفظ وترحيل الجرد بنجاح ({id.ToString()[..8]}).";
            CountLines.Clear();
            await LoadAsync();
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }
}

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
        ProductId = item.ProductId; ProductCode = item.ProductCode; ProductName = item.ProductName;
        BaseUnitName = item.BaseUnitName; SystemQuantity = item.QuantityBase; _actualQuantity = item.QuantityBase;
    }
    partial void OnActualQuantityChanged(decimal value) => OnPropertyChanged(nameof(Difference));
}
