using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Enums;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Returns;

public partial class ReturnsViewModel : BaseViewModel
{
    private readonly IReturnService _returnService;
    private readonly ICashboxAdminService _cashboxService;

    [ObservableProperty] private ObservableCollection<ReturnSourceInvoice> _sourceInvoices = new();
    [ObservableProperty] private ReturnSourceInvoice? _selectedSource;
    [ObservableProperty] private ObservableCollection<ReturnLineEditor> _items = new();
    [ObservableProperty] private ObservableCollection<ReturnListItem> _recentReturns = new();
    [ObservableProperty] private ObservableCollection<CashboxListItem> _cashboxes = new();
    [ObservableProperty] private CashboxListItem? _selectedCashbox;
    [ObservableProperty] private ReturnTypeOption _selectedReturnType;
    [ObservableProperty] private RefundMethodOption _selectedRefundMethod;
    [ObservableProperty] private string _invoiceSearch = string.Empty;
    [ObservableProperty] private string _returnSearch = string.Empty;
    [ObservableProperty] private string _sourceCaption = "لم يتم اختيار فاتورة";
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<ReturnTypeOption> ReturnTypes { get; } = new(new[]
    {
        new ReturnTypeOption(ReturnType.SalesReturn, "مرتجع مبيعات"),
        new ReturnTypeOption(ReturnType.PurchaseReturn, "مرتجع مشتريات")
    });

    public ObservableCollection<RefundMethodOption> RefundMethods { get; } = new();
    public bool CanCreate { get; }
    public decimal ReturnTotal => Items.Sum(i => i.LineTotal);
    public int SelectedLines => Items.Count(i => i.Quantity > 0);
    public bool NeedsCashbox => SelectedRefundMethod.Code == "cash";

    public ReturnsViewModel(
        IReturnService returnService,
        ICashboxAdminService cashboxService,
        IPermissionService permissionService)
    {
        _returnService = returnService;
        _cashboxService = cashboxService;
        CanCreate = permissionService.Can("Return.Create");
        _selectedReturnType = ReturnTypes[0];
        _selectedRefundMethod = new RefundMethodOption("cash", "رد نقدي من/إلى الخزينة");
        ConfigureRefundMethods();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var cashTask = _cashboxService.GetAsync();
            var returnsTask = _returnService.SearchReturnsAsync();
            var sourcesTask = _returnService.SearchSourceInvoicesAsync(SelectedReturnType.Value);
            await Task.WhenAll(cashTask, returnsTask, sourcesTask);
            Cashboxes = new ObservableCollection<CashboxListItem>(cashTask.Result.Where(c => c.IsActive));
            SelectedCashbox = Cashboxes.FirstOrDefault();
            RecentReturns = new ObservableCollection<ReturnListItem>(returnsTask.Result);
            SourceInvoices = new ObservableCollection<ReturnSourceInvoice>(sourcesTask.Result);
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    partial void OnSelectedReturnTypeChanged(ReturnTypeOption value)
    {
        ConfigureRefundMethods();
        ClearDocument();
        _ = SearchSourcesAsync();
    }

    partial void OnSelectedRefundMethodChanged(RefundMethodOption value)
        => OnPropertyChanged(nameof(NeedsCashbox));

    private void ConfigureRefundMethods()
    {
        RefundMethods.Clear();
        RefundMethods.Add(new RefundMethodOption("cash", "رد نقدي من/إلى الخزينة"));
        RefundMethods.Add(SelectedReturnType.Value == ReturnType.SalesReturn
            ? new RefundMethodOption("customer_balance", "خصم من رصيد العميل")
            : new RefundMethodOption("supplier_balance", "خصم من رصيد المورد"));
        SelectedRefundMethod = RefundMethods[0];
    }

    [RelayCommand]
    private async Task SearchSourcesAsync()
    {
        try
        {
            SourceInvoices = new ObservableCollection<ReturnSourceInvoice>(
                await _returnService.SearchSourceInvoicesAsync(SelectedReturnType.Value, InvoiceSearch));
        }
        catch (Exception ex) { SetError(ex.Message); }
    }

    [RelayCommand]
    private async Task LoadSourceAsync(ReturnSourceInvoice? source)
    {
        source ??= SelectedSource;
        if (source is null) return;
        IsBusy = true;
        try
        {
            var details = await _returnService.GetSourceAsync(SelectedReturnType.Value, source.Id);
            var editors = details.Lines.Select(line => new ReturnLineEditor(line)).ToList();
            foreach (var editor in editors)
                editor.PropertyChanged += (_, _) => NotifyTotals();
            Items = new ObservableCollection<ReturnLineEditor>(editors);
            SelectedSource = source;
            SourceCaption = $"{details.InvoiceNo} — {details.PartyName}";
            StatusMessage = string.Empty;
            NotifyTotals();
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PostAsync()
    {
        if (!CanCreate) { SetError("ليس لديك صلاحية إنشاء مرتجع."); return; }
        if (SelectedSource is null) { SetError("اختر الفاتورة الأصلية."); return; }
        IsBusy = true;
        ClearError();
        try
        {
            var id = await _returnService.PostAsync(new PostReturnRequest
            {
                ReturnType = SelectedReturnType.Value,
                OriginalInvoiceId = SelectedSource.Id,
                RefundMethod = SelectedRefundMethod.Code,
                CashboxId = NeedsCashbox ? SelectedCashbox?.Id : null,
                Notes = Notes,
                Items = Items.Where(i => i.Quantity > 0).Select(i => new ReturnLineInput
                {
                    ProductId = i.ProductId, ProductUnitId = i.ProductUnitId,
                    Quantity = i.Quantity, UnitPrice = i.UnitPrice, Restock = i.Restock,
                    Condition = i.Condition, SerialNumbers = i.SerialNumbers, Notes = i.Notes
                }).ToList()
            });
            StatusMessage = $"تم ترحيل المرتجع بنجاح ({id.ToString()[..8]}).";
            RecentReturns = new ObservableCollection<ReturnListItem>(await _returnService.SearchReturnsAsync());
            await SearchSourcesAsync();
            ClearDocument(keepMessage: true);
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SearchReturnsAsync()
    {
        try
        {
            RecentReturns = new ObservableCollection<ReturnListItem>(
                await _returnService.SearchReturnsAsync(ReturnSearch));
        }
        catch (Exception ex) { SetError(ex.Message); }
    }

    [RelayCommand]
    private void SelectAllAvailable()
    {
        foreach (var item in Items) item.Quantity = item.AvailableQuantity;
        NotifyTotals();
    }

    private void ClearDocument(bool keepMessage = false)
    {
        SelectedSource = null; Items.Clear(); SourceCaption = "لم يتم اختيار فاتورة"; Notes = string.Empty;
        if (!keepMessage) StatusMessage = string.Empty;
        NotifyTotals();
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(ReturnTotal));
        OnPropertyChanged(nameof(SelectedLines));
    }
}

public sealed record ReturnTypeOption(ReturnType Value, string Name);
public sealed record RefundMethodOption(string Code, string Name);

public partial class ReturnLineEditor : ObservableObject
{
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private bool _restock = true;
    [ObservableProperty] private ReturnItemCondition _condition = ReturnItemCondition.Good;
    [ObservableProperty] private string _serialsText = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    public Guid ProductId { get; }
    public Guid ProductUnitId { get; }
    public string ProductName { get; }
    public string UnitName { get; }
    public decimal AvailableQuantity { get; }
    public decimal UnitPrice { get; }
    public bool IsSerialTracked { get; }
    public string AvailableSerialsText { get; }
    public decimal LineTotal => Quantity * UnitPrice;
    public IReadOnlyCollection<string> SerialNumbers => SerialsText.Split(
        new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
    public Array Conditions { get; } = Enum.GetValues(typeof(ReturnItemCondition));

    public ReturnLineEditor(ReturnSourceLine line)
    {
        ProductId = line.ProductId; ProductUnitId = line.ProductUnitId;
        ProductName = line.ProductName; UnitName = line.UnitName;
        AvailableQuantity = line.AvailableQuantity; UnitPrice = line.UnitPrice;
        IsSerialTracked = line.IsSerialTracked;
        AvailableSerialsText = string.Join(", ", line.AvailableSerials);
    }

    partial void OnQuantityChanged(decimal value)
    {
        if (value < 0) Quantity = 0;
        else if (value > AvailableQuantity) Quantity = AvailableQuantity;
        OnPropertyChanged(nameof(LineTotal));
    }
}
