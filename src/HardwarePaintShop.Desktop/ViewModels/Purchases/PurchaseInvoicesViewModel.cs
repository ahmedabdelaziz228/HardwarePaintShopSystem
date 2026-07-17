using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Enums;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Purchases;

public partial class PurchaseInvoicesViewModel : BaseViewModel
{
    private readonly IPurchaseService _purchaseService;
    private readonly IPartyService _partyService;
    private readonly ICashboxAdminService _cashboxService;

    [ObservableProperty] private ObservableCollection<PurchaseInvoiceListItem> _invoices = new();
    [ObservableProperty] private PurchaseInvoiceListItem? _selectedInvoice;
    [ObservableProperty] private ObservableCollection<SupplierListItem> _suppliers = new();
    [ObservableProperty] private SupplierListItem? _selectedSupplier;
    [ObservableProperty] private ObservableCollection<CashboxListItem> _cashboxes = new();
    [ObservableProperty] private CashboxListItem? _selectedCashbox;
    [ObservableProperty] private ObservableCollection<PurchaseProductOption> _productResults = new();
    [ObservableProperty] private PurchaseProductOption? _selectedProduct;
    [ObservableProperty] private ObservableCollection<PurchaseLineEditor> _items = new();

    [ObservableProperty] private Guid? _editingId;
    [ObservableProperty] private string _internalInvoiceNo = "تلقائي عند الحفظ";
    [ObservableProperty] private string _supplierInvoiceNo = string.Empty;
    [ObservableProperty] private DateTime _invoiceDate = DateTime.Today;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private decimal _extraCosts;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _invoiceSearch = string.Empty;
    [ObservableProperty] private string _productSearch = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private InvoiceStatus _currentStatus = InvoiceStatus.Draft;
    [ObservableProperty] private PaymentMethodOption _selectedPaymentMethod;

    public ObservableCollection<PaymentMethodOption> PaymentMethods { get; } = new(
        new[]
        {
            new PaymentMethodOption(PaymentMethod.Cash, "نقدي"),
            new PaymentMethodOption(PaymentMethod.Card, "بطاقة"),
            new PaymentMethodOption(PaymentMethod.BankTransfer, "تحويل بنكي"),
            new PaymentMethodOption(PaymentMethod.MobileWallet, "محفظة إلكترونية"),
            new PaymentMethodOption(PaymentMethod.InstaPay, "InstaPay"),
            new PaymentMethodOption(PaymentMethod.Other, "أخرى")
        });

    public bool CanCreate { get; }
    public bool CanUpdate { get; }
    public bool CanPost { get; }
    public bool CanVoid { get; }
    public bool CanViewCost { get; }
    public bool IsDraft => CurrentStatus == InvoiceStatus.Draft;
    public bool CanEditCurrent => IsDraft && (EditingId.HasValue ? CanUpdate : CanCreate);
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal TotalAmount => Subtotal + ExtraCosts;
    public decimal RemainingPreview => Math.Max(0, TotalAmount - PaidAmount);

    public PurchaseInvoicesViewModel(
        IPurchaseService purchaseService,
        IPartyService partyService,
        ICashboxAdminService cashboxService,
        IPermissionService permissionService)
    {
        _purchaseService = purchaseService;
        _partyService = partyService;
        _cashboxService = cashboxService;
        CanCreate = permissionService.Can("Purchase.Create");
        CanUpdate = permissionService.Can("Purchase.Update");
        CanPost = permissionService.Can("Purchase.Post");
        CanVoid = permissionService.Can("Purchase.Void");
        CanViewCost = permissionService.Can("Purchase.ViewCost");
        _selectedPaymentMethod = PaymentMethods[0];
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var suppliersTask = _partyService.SearchSuppliersAsync(new PartySearchCriteria());
            var cashboxesTask = _cashboxService.GetAsync();
            var invoicesTask = _purchaseService.SearchAsync();
            await Task.WhenAll(suppliersTask, cashboxesTask, invoicesTask);
            Suppliers = new ObservableCollection<SupplierListItem>(suppliersTask.Result);
            Cashboxes = new ObservableCollection<CashboxListItem>(cashboxesTask.Result);
            Invoices = new ObservableCollection<PurchaseInvoiceListItem>(invoicesTask.Result);
            SelectedCashbox = Cashboxes.FirstOrDefault();
            NewInvoice();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        try
        {
            Invoices = new ObservableCollection<PurchaseInvoiceListItem>(
                await _purchaseService.SearchAsync(InvoiceSearch));
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    private void NewInvoice()
    {
        EditingId = null;
        InternalInvoiceNo = "تلقائي عند الحفظ";
        SupplierInvoiceNo = string.Empty;
        SelectedSupplier = Suppliers.FirstOrDefault();
        InvoiceDate = DateTime.Today;
        DueDate = null;
        ExtraCosts = 0;
        PaidAmount = 0;
        Notes = string.Empty;
        Items = new ObservableCollection<PurchaseLineEditor>();
        CurrentStatus = InvoiceStatus.Draft;
        StatusMessage = string.Empty;
        NotifyTotals();
    }

    [RelayCommand]
    private async Task OpenInvoiceAsync(PurchaseInvoiceListItem? item)
    {
        if (item is null)
            return;
        IsBusy = true;
        try
        {
            var details = await _purchaseService.GetAsync(item.Id);
            EditingId = details.Id;
            InternalInvoiceNo = details.InvoiceNo;
            SupplierInvoiceNo = details.SupplierInvoiceNo ?? string.Empty;
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == details.SupplierId);
            InvoiceDate = details.InvoiceDate.ToLocalTime().Date;
            DueDate = details.DueDate?.ToLocalTime().Date;
            ExtraCosts = details.ExtraCosts;
            PaidAmount = details.PaidAmount;
            Notes = details.Notes ?? string.Empty;
            CurrentStatus = details.Status;
            var lines = details.Items.Select(i => new PurchaseLineEditor(
                new PurchaseProductOption(
                    i.ProductId, i.ProductUnitId, i.ProductCode, i.ProductName, i.UnitName,
                    i.ConversionFactorToBase, 0, null, i.IsSerialTracked, null),
                i.Quantity, i.UnitPurchasePrice, string.Join(Environment.NewLine, i.SerialNumbers))).ToList();
            foreach (var line in lines)
                line.PropertyChanged += (_, _) => NotifyTotals();
            Items = new ObservableCollection<PurchaseLineEditor>(lines);
            NotifyState();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        try
        {
            ProductResults = new ObservableCollection<PurchaseProductOption>(
                await _purchaseService.SearchProductsAsync(ProductSearch));
            SelectedProduct = ProductResults.FirstOrDefault();
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    private void AddProduct()
    {
        if (!CanEditCurrent)
        {
            ShowError("الفاتورة الحالية غير قابلة للتعديل.");
            return;
        }
        if (SelectedProduct is null)
        {
            ShowError("ابحث عن صنف واختر الوحدة أولًا.");
            return;
        }
        var existing = Items.FirstOrDefault(i =>
            i.ProductUnitId == SelectedProduct.ProductUnitId ||
            (SelectedProduct.IsSerialTracked && i.ProductId == SelectedProduct.ProductId));
        if (existing is not null)
        {
            existing.Quantity += 1;
            return;
        }
        var price = SelectedProduct.LastPurchasePriceBaseUnit ?? 0;
        var line = new PurchaseLineEditor(SelectedProduct, 1, price, string.Empty);
        line.PropertyChanged += (_, _) => NotifyTotals();
        Items.Add(line);
        NotifyTotals();
    }

    [RelayCommand]
    private void RemoveLine(PurchaseLineEditor? line)
    {
        if (line is null || !CanEditCurrent)
            return;
        Items.Remove(line);
        NotifyTotals();
    }

    [RelayCommand]
    private async Task SaveDraftAsync()
    {
        if (!CanEditCurrent)
        {
            ShowError("ليس لديك صلاحية حفظ هذه المسودة.");
            return;
        }
        IsBusy = true;
        try
        {
            var id = await _purchaseService.SaveDraftAsync(BuildDraftRequest());
            await LoadInvoicesAsync();
            await OpenInvoiceAsync(Invoices.FirstOrDefault(i => i.Id == id));
            ShowSuccess("تم حفظ مسودة فاتورة الشراء ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PostAsync()
    {
        if (!CanPost)
        {
            ShowError("ليس لديك صلاحية ترحيل المشتريات.");
            return;
        }
        IsBusy = true;
        try
        {
            var id = EditingId ?? await _purchaseService.SaveDraftAsync(BuildDraftRequest());
            await _purchaseService.PostAsync(new PostPurchaseRequest
            {
                InvoiceId = id,
                PaidAmount = PaidAmount,
                CashboxId = PaidAmount > 0 ? SelectedCashbox?.Id : null,
                PaymentMethod = SelectedPaymentMethod.Value
            });
            await LoadInvoicesAsync();
            await OpenInvoiceAsync(Invoices.FirstOrDefault(i => i.Id == id));
            ShowSuccess("تم ترحيل الفاتورة وتحديث المخزون والتكلفة وحساب المورد ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task VoidAsync()
    {
        if (!EditingId.HasValue || !CanVoid)
            return;
        IsBusy = true;
        try
        {
            var id = EditingId.Value;
            await _purchaseService.VoidAsync(id);
            await LoadInvoicesAsync();
            await OpenInvoiceAsync(Invoices.FirstOrDefault(i => i.Id == id));
            ShowSuccess("تم إلغاء الفاتورة وعكس أثرها المسموح به ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private PurchaseDraftRequest BuildDraftRequest() => new()
    {
        Id = EditingId,
        SupplierId = SelectedSupplier?.Id ?? Guid.Empty,
        SupplierInvoiceNo = SupplierInvoiceNo,
        InvoiceDate = InvoiceDate,
        DueDate = DueDate,
        ExtraCosts = ExtraCosts,
        Notes = Notes,
        Items = Items.Select(i => new PurchaseLineInput
        {
            ProductId = i.ProductId,
            ProductUnitId = i.ProductUnitId,
            Quantity = i.Quantity,
            UnitPurchasePrice = i.UnitPurchasePrice,
            SerialNumbers = i.ParseSerialNumbers()
        }).ToList()
    };

    partial void OnExtraCostsChanged(decimal value) => NotifyTotals();
    partial void OnPaidAmountChanged(decimal value) => NotifyTotals();
    partial void OnCurrentStatusChanged(InvoiceStatus value) => NotifyState();

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(RemainingPreview));
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(IsDraft));
        OnPropertyChanged(nameof(CanEditCurrent));
        NotifyTotals();
    }

    private void ShowError(string message) { StatusMessage = message; IsSuccess = false; }
    private void ShowSuccess(string message) { StatusMessage = message; IsSuccess = true; }
}

public partial class PurchaseLineEditor : ObservableObject
{
    public Guid ProductId { get; }
    public Guid ProductUnitId { get; }
    public string? ProductCode { get; }
    public string ProductName { get; }
    public string UnitName { get; }
    public decimal ConversionFactorToBase { get; }
    public bool IsSerialTracked { get; }

    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitPurchasePrice;
    [ObservableProperty] private string _serialNumbersText;

    public decimal QuantityBaseUnit => Quantity * ConversionFactorToBase;
    public decimal LineTotal => Quantity * UnitPurchasePrice;
    public string ConversionPreview => $"1 {UnitName} = {ConversionFactorToBase:0.###} أساسي";
    public string SerialHint => IsSerialTracked ? "مطلوب" : "لا";

    public PurchaseLineEditor(
        PurchaseProductOption product,
        decimal quantity,
        decimal unitPurchasePrice,
        string serialNumbersText)
    {
        ProductId = product.ProductId;
        ProductUnitId = product.ProductUnitId;
        ProductCode = product.ProductCode;
        ProductName = product.ProductName;
        UnitName = product.UnitName;
        ConversionFactorToBase = product.ConversionFactorToBase;
        IsSerialTracked = product.IsSerialTracked;
        _quantity = quantity;
        _unitPurchasePrice = unitPurchasePrice;
        _serialNumbersText = serialNumbersText;
    }

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(QuantityBaseUnit));
        OnPropertyChanged(nameof(LineTotal));
    }

    partial void OnUnitPurchasePriceChanged(decimal value)
        => OnPropertyChanged(nameof(LineTotal));

    public IReadOnlyCollection<string> ParseSerialNumbers()
        => SerialNumbersText.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
}

public sealed record PaymentMethodOption(PaymentMethod Value, string Name);
