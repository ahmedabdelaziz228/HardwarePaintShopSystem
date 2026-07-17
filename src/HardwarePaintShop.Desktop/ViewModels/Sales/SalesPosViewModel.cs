using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace HardwarePaintShop.Desktop.ViewModels.Sales;

public partial class SalesPosViewModel : BaseViewModel
{
    private readonly ISalesService _salesService;
    private readonly IPartyService _partyService;
    private readonly ILookupService _lookupService;
    private readonly ICashboxAdminService _cashboxService;
    private readonly ISettingsBackupService _settingsService;

    [ObservableProperty] private ObservableCollection<SalesInvoiceListItem> _invoices = new();
    [ObservableProperty] private SalesInvoiceListItem? _selectedInvoice;
    [ObservableProperty] private ObservableCollection<CustomerListItem> _customers = new();
    [ObservableProperty] private CustomerListItem? _selectedCustomer;
    [ObservableProperty] private ObservableCollection<PriceGroup> _priceGroups = new();
    [ObservableProperty] private PriceGroup? _selectedPriceGroup;
    [ObservableProperty] private ObservableCollection<CashboxListItem> _cashboxes = new();
    [ObservableProperty] private CashboxListItem? _selectedCashbox;
    [ObservableProperty] private ObservableCollection<SalesProductOption> _productResults = new();
    [ObservableProperty] private SalesProductOption? _selectedProduct;
    [ObservableProperty] private ObservableCollection<SalesLineEditor> _items = new();

    [ObservableProperty] private Guid? _editingId;
    [ObservableProperty] private string _invoiceNo = "فاتورة جديدة";
    [ObservableProperty] private string _invoiceSearch = string.Empty;
    [ObservableProperty] private string _productSearch = string.Empty;
    [ObservableProperty] private DateTime _invoiceDate = DateTime.Today;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private InvoiceStatus _currentStatus = InvoiceStatus.Draft;
    [ObservableProperty] private SalesPaymentMethodOption _selectedPaymentMethod;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;

    public ObservableCollection<SalesPaymentMethodOption> PaymentMethods { get; } = new(
        new[]
        {
            new SalesPaymentMethodOption(PaymentMethod.Cash, "نقدي"),
            new SalesPaymentMethodOption(PaymentMethod.Card, "بطاقة"),
            new SalesPaymentMethodOption(PaymentMethod.BankTransfer, "تحويل بنكي"),
            new SalesPaymentMethodOption(PaymentMethod.MobileWallet, "محفظة إلكترونية"),
            new SalesPaymentMethodOption(PaymentMethod.InstaPay, "InstaPay"),
            new SalesPaymentMethodOption(PaymentMethod.Other, "أخرى")
        });

    public bool CanCreate { get; }
    public bool CanUpdate { get; }
    public bool CanPost { get; }
    public bool CanVoidDraft { get; }
    public bool CanPrint { get; }
    public bool IsDraft => CurrentStatus == InvoiceStatus.Draft;
    public bool CanEditCurrent => IsDraft && (EditingId.HasValue ? CanUpdate : CanCreate);
    public string CustomerDisplay => SelectedCustomer?.Name ?? "عميل نقدي";
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal TotalAmount => Math.Max(0, Subtotal - DiscountAmount);
    public decimal RemainingAmount => Math.Max(0, TotalAmount - PaidAmount);
    public int CartLines => Items.Count;

    public SalesPosViewModel(
        ISalesService salesService,
        IPartyService partyService,
        ILookupService lookupService,
        ICashboxAdminService cashboxService,
        ISettingsBackupService settingsService,
        IPermissionService permissionService)
    {
        _salesService = salesService;
        _partyService = partyService;
        _lookupService = lookupService;
        _cashboxService = cashboxService;
        _settingsService = settingsService;
        CanCreate = permissionService.Can("Sales.Create");
        CanUpdate = permissionService.Can("Sales.Update");
        CanPost = permissionService.Can("Sales.Post");
        CanVoidDraft = permissionService.Can("Sales.VoidDraft");
        CanPrint = permissionService.Can("Sales.Print");
        _selectedPaymentMethod = PaymentMethods[0];
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var customersTask = _partyService.SearchCustomersAsync(new PartySearchCriteria());
            var groupsTask = _lookupService.GetPriceGroupsAsync();
            var cashboxesTask = _cashboxService.GetAsync();
            var invoicesTask = _salesService.SearchAsync();
            await Task.WhenAll(customersTask, groupsTask, cashboxesTask, invoicesTask);
            Customers = new ObservableCollection<CustomerListItem>(customersTask.Result);
            PriceGroups = new ObservableCollection<PriceGroup>(groupsTask.Result);
            Cashboxes = new ObservableCollection<CashboxListItem>(cashboxesTask.Result);
            Invoices = new ObservableCollection<SalesInvoiceListItem>(invoicesTask.Result);
            SelectedPriceGroup = PriceGroups.FirstOrDefault(g => g.IsDefault) ?? PriceGroups.FirstOrDefault();
            SelectedCashbox = Cashboxes.FirstOrDefault();
            NewSale();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        try
        {
            Invoices = new ObservableCollection<SalesInvoiceListItem>(
                await _salesService.SearchAsync(InvoiceSearch));
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    private void NewSale()
    {
        EditingId = null;
        InvoiceNo = "فاتورة جديدة";
        SelectedInvoice = null;
        SelectedCustomer = null;
        InvoiceDate = DateTime.Today;
        DueDate = null;
        DiscountAmount = 0;
        PaidAmount = 0;
        Notes = string.Empty;
        Items = new ObservableCollection<SalesLineEditor>();
        CurrentStatus = InvoiceStatus.Draft;
        StatusMessage = string.Empty;
        NotifyState();
    }

    [RelayCommand]
    private void UseCashCustomer()
    {
        SelectedCustomer = null;
        DueDate = null;
        OnPropertyChanged(nameof(CustomerDisplay));
    }

    [RelayCommand]
    private async Task OpenInvoiceAsync(SalesInvoiceListItem? item)
    {
        if (item is null)
            return;
        IsBusy = true;
        try
        {
            var details = await _salesService.GetAsync(item.Id);
            EditingId = details.Id;
            InvoiceNo = details.InvoiceNo;
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == details.CustomerId);
            InvoiceDate = details.InvoiceDate.ToLocalTime().Date;
            DueDate = details.DueDate?.ToLocalTime().Date;
            DiscountAmount = details.DiscountAmount;
            PaidAmount = details.PaidAmount;
            Notes = details.Notes ?? string.Empty;
            CurrentStatus = details.Status;
            var lines = details.Items.Select(i => new SalesLineEditor(
                new SalesProductOption(
                    i.ProductId, i.ProductUnitId, i.ProductCode, i.ProductName, i.UnitName,
                    i.ConversionFactorToBase, 0, i.UnitPrice, 0, i.IsSerialTracked, null),
                i.Quantity, i.UnitPrice, string.Join(Environment.NewLine, i.SerialNumbers))).ToList();
            foreach (var line in lines)
                line.PropertyChanged += (_, _) => NotifyTotals();
            Items = new ObservableCollection<SalesLineEditor>(lines);
            NotifyState();
            OnPropertyChanged(nameof(CustomerDisplay));
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        try
        {
            var results = await _salesService.SearchProductsAsync(ProductSearch, SelectedPriceGroup?.Id);
            ProductResults = new ObservableCollection<SalesProductOption>(results);
            SelectedProduct = ProductResults.FirstOrDefault();
            if (results.Count == 1 && ProductSearch.Length > 0)
                AddProduct();
            else if (results.Count == 0)
                ShowError("لا يوجد صنف مطابق أو نشط.");
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
            ShowError("ابحث عن صنف واختر وحدة البيع.");
            return;
        }
        if (SelectedProduct.StockBaseQuantity <= 0)
        {
            ShowError("الصنف غير متوفر في المخزون.");
            return;
        }

        var existing = Items.FirstOrDefault(i => i.ProductUnitId == SelectedProduct.ProductUnitId);
        if (existing is not null)
        {
            if (existing.IsSerialTracked)
            {
                ShowError("الصنف المسلسل موجود بالفعل؛ عدّل الكمية وأدخل السيريالات في نفس السطر.");
                return;
            }
            existing.Quantity += 1;
        }
        else
        {
            var line = new SalesLineEditor(SelectedProduct, 1, SelectedProduct.SalePrice, string.Empty);
            line.PropertyChanged += (_, _) => NotifyTotals();
            Items.Add(line);
        }
        ProductSearch = string.Empty;
        NotifyTotals();
        ShowSuccess("تمت إضافة الصنف للسلة.");
    }

    [RelayCommand]
    private void RemoveLine(SalesLineEditor? line)
    {
        if (line is null || !CanEditCurrent)
            return;
        Items.Remove(line);
        NotifyTotals();
    }

    [RelayCommand]
    private void SetFullPayment()
    {
        PaidAmount = TotalAmount;
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
            var id = await _salesService.SaveDraftAsync(BuildDraftRequest());
            await LoadInvoicesAsync();
            await OpenInvoiceAsync(Invoices.FirstOrDefault(i => i.Id == id));
            ShowSuccess("تم حفظ مسودة البيع وحجز السيريالات ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PostAsync()
    {
        if (!CanPost)
        {
            ShowError("ليس لديك صلاحية ترحيل المبيعات.");
            return;
        }
        IsBusy = true;
        try
        {
            var id = EditingId ?? await _salesService.SaveDraftAsync(BuildDraftRequest());
            await _salesService.PostAsync(new PostSalesRequest
            {
                InvoiceId = id,
                PaidAmount = PaidAmount,
                CashboxId = PaidAmount > 0 ? SelectedCashbox?.Id : null,
                PaymentMethod = SelectedPaymentMethod.Value
            });
            await LoadInvoicesAsync();
            await OpenInvoiceAsync(Invoices.FirstOrDefault(i => i.Id == id));
            ShowSuccess("تم ترحيل البيع وتحديث المخزون والخزينة وحساب العميل ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task VoidDraftAsync()
    {
        if (!EditingId.HasValue || !CanVoidDraft)
            return;
        IsBusy = true;
        try
        {
            var id = EditingId.Value;
            await _salesService.VoidDraftAsync(id);
            await LoadInvoicesAsync();
            await OpenInvoiceAsync(Invoices.FirstOrDefault(i => i.Id == id));
            ShowSuccess("تم إلغاء المسودة وإتاحة السيريالات مرة أخرى.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PrintReceiptAsync()
    {
        if (!CanPrint) { ShowError("ليس لديك صلاحية طباعة الفواتير."); return; }
        if (!EditingId.HasValue || CurrentStatus == InvoiceStatus.Draft)
        {
            ShowError("احفظ ورحّل الفاتورة قبل طباعتها."); return;
        }
        try
        {
            var settings = await _settingsService.GetSettingsAsync();
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;
            var width = settings.ThermalPrinterWidth / 25.4 * 96.0;
            var document = BuildReceipt(settings, width);
            dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, $"فاتورة {InvoiceNo}");
            ShowSuccess("تم إرسال الفاتورة إلى الطابعة.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    private FlowDocument BuildReceipt(ShopSettings settings, double width)
    {
        var document = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft, FontFamily = new FontFamily("Segoe UI"),
            FontSize = settings.ThermalPrinterWidth == 58 ? 8.5 : 10,
            PageWidth = width, PagePadding = new Thickness(8), ColumnWidth = Math.Max(120, width - 16)
        };
        document.Blocks.Add(new Paragraph(new Run(settings.ShopName)) { TextAlignment = TextAlignment.Center, FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 2) });
        if (!string.IsNullOrWhiteSpace(settings.ShopAddress)) document.Blocks.Add(Centered(settings.ShopAddress));
        if (!string.IsNullOrWhiteSpace(settings.ShopPhone)) document.Blocks.Add(Centered($"ت: {settings.ShopPhone}"));
        if (!string.IsNullOrWhiteSpace(settings.TaxNumber)) document.Blocks.Add(Centered($"الرقم الضريبي: {settings.TaxNumber}"));
        document.Blocks.Add(Centered($"فاتورة بيع: {InvoiceNo}"));
        document.Blocks.Add(Centered($"{InvoiceDate:dd/MM/yyyy}  {DateTime.Now:HH:mm}"));
        document.Blocks.Add(new Paragraph(new Run($"العميل: {CustomerDisplay}")) { Margin = new Thickness(0, 4, 0, 4) });
        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(0.7, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(0.9, GridUnitType.Star) });
        var group = new TableRowGroup(); table.RowGroups.Add(group);
        AddReceiptRow(group, "الصنف", "الكمية", "الإجمالي", true);
        foreach (var item in Items)
            AddReceiptRow(group, item.ProductName, $"{item.Quantity:0.###}", $"{item.LineTotal:N2}", false);
        document.Blocks.Add(table);
        document.Blocks.Add(new Paragraph(new Run($"الإجمالي: {Subtotal:N2} ج.م")) { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 6, 0, 0) });
        if (DiscountAmount > 0) document.Blocks.Add(new Paragraph(new Run($"الخصم: {DiscountAmount:N2} ج.م")) { Margin = new Thickness(0) });
        document.Blocks.Add(new Paragraph(new Run($"الصافي: {TotalAmount:N2} ج.م")) { FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0) });
        document.Blocks.Add(new Paragraph(new Run($"المدفوع: {PaidAmount:N2} — المتبقي: {RemainingAmount:N2}")) { Margin = new Thickness(0) });
        document.Blocks.Add(Centered("شكرًا لزيارتكم"));
        return document;
    }

    private static Paragraph Centered(string text) => new(new Run(text)) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0) };
    private static void AddReceiptRow(TableRowGroup group, string first, string second, string third, bool header)
    {
        var row = new TableRow { FontWeight = header ? FontWeights.Bold : FontWeights.Normal };
        foreach (var value in new[] { first, second, third })
            row.Cells.Add(new TableCell(new Paragraph(new Run(value)) { Margin = new Thickness(1) })
            { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 0.5), Padding = new Thickness(2) });
        group.Rows.Add(row);
    }

    private SalesDraftRequest BuildDraftRequest() => new()
    {
        Id = EditingId,
        CustomerId = SelectedCustomer?.Id,
        PriceGroupId = SelectedPriceGroup?.Id,
        InvoiceDate = InvoiceDate,
        DueDate = DueDate,
        DiscountAmount = DiscountAmount,
        Notes = Notes,
        Items = Items.Select(i => new SalesLineInput
        {
            ProductId = i.ProductId,
            ProductUnitId = i.ProductUnitId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            SerialNumbers = i.ParseSerialNumbers()
        }).ToList()
    };

    partial void OnSelectedCustomerChanged(CustomerListItem? value)
    {
        OnPropertyChanged(nameof(CustomerDisplay));
        if (value is null)
            DueDate = null;
        else
            _ = ApplyCustomerPriceGroupAsync(value.Id);
    }

    private async Task ApplyCustomerPriceGroupAsync(Guid customerId)
    {
        try
        {
            var customer = await _partyService.GetCustomerAsync(customerId);
            if (customer.PriceGroupId.HasValue)
                SelectedPriceGroup = PriceGroups.FirstOrDefault(g => g.Id == customer.PriceGroupId.Value)
                    ?? SelectedPriceGroup;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    partial void OnDiscountAmountChanged(decimal value) => NotifyTotals();
    partial void OnPaidAmountChanged(decimal value) => NotifyTotals();
    partial void OnCurrentStatusChanged(InvoiceStatus value) => NotifyState();

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(RemainingAmount));
        OnPropertyChanged(nameof(CartLines));
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

public partial class SalesLineEditor : ObservableObject
{
    public Guid ProductId { get; }
    public Guid ProductUnitId { get; }
    public string? ProductCode { get; }
    public string ProductName { get; }
    public string UnitName { get; }
    public decimal ConversionFactorToBase { get; }
    public decimal AvailableBaseQuantity { get; }
    public decimal MinSalePrice { get; }
    public bool IsSerialTracked { get; }

    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private string _serialNumbersText;

    public decimal QuantityBaseUnit => Quantity * ConversionFactorToBase;
    public decimal LineTotal => Quantity * UnitPrice;
    public string StockStatus => AvailableBaseQuantity == 0 ? "—" : $"{AvailableBaseQuantity:0.###} أساسي";

    public SalesLineEditor(
        SalesProductOption product,
        decimal quantity,
        decimal unitPrice,
        string serialNumbersText)
    {
        ProductId = product.ProductId;
        ProductUnitId = product.ProductUnitId;
        ProductCode = product.ProductCode;
        ProductName = product.ProductName;
        UnitName = product.UnitName;
        ConversionFactorToBase = product.ConversionFactorToBase;
        AvailableBaseQuantity = product.StockBaseQuantity;
        MinSalePrice = product.MinSalePrice;
        IsSerialTracked = product.IsSerialTracked;
        _quantity = quantity;
        _unitPrice = unitPrice;
        _serialNumbersText = serialNumbersText;
    }

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(QuantityBaseUnit));
        OnPropertyChanged(nameof(LineTotal));
    }

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));

    public IReadOnlyCollection<string> ParseSerialNumbers()
        => SerialNumbersText.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
}

public sealed record SalesPaymentMethodOption(PaymentMethod Value, string Name);
