using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Finance;

public partial class FinanceOperationsViewModel : BaseViewModel
{
    private readonly IFinanceOperationsService _financeService;
    private readonly ICashboxAdminService _cashboxService;
    private readonly IPartyService _partyService;
    private readonly ILookupService _lookupService;

    [ObservableProperty] private ObservableCollection<FinanceOperationOption> _operationOptions = new();
    [ObservableProperty] private FinanceOperationOption? _selectedOperation;
    [ObservableProperty] private ObservableCollection<CashboxListItem> _cashboxes = new();
    [ObservableProperty] private CashboxListItem? _selectedCashbox;
    [ObservableProperty] private CashboxListItem? _destinationCashbox;
    [ObservableProperty] private ObservableCollection<CustomerListItem> _customers = new();
    [ObservableProperty] private CustomerListItem? _selectedCustomer;
    [ObservableProperty] private ObservableCollection<SupplierListItem> _suppliers = new();
    [ObservableProperty] private SupplierListItem? _selectedSupplier;
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _expenseCategories = new();
    [ObservableProperty] private ExpenseCategory? _selectedExpenseCategory;
    [ObservableProperty] private ObservableCollection<CashMovementListItem> _movements = new();
    [ObservableProperty] private ObservableCollection<ExpenseListItem> _expenses = new();
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private DateTime _operationDate = DateTime.Now;
    [ObservableProperty] private DateTime? _filterFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _filterTo = DateTime.Today;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private decimal _totalIn;
    [ObservableProperty] private decimal _totalOut;

    public bool NeedsExpenseCategory => SelectedOperation?.Value == FinanceOperationType.Expense;
    public bool NeedsCustomer => SelectedOperation?.Value == FinanceOperationType.CustomerCollection;
    public bool NeedsSupplier => SelectedOperation?.Value == FinanceOperationType.SupplierPayment;
    public bool NeedsDestination => SelectedOperation?.Value == FinanceOperationType.CashTransfer;

    public FinanceOperationsViewModel(
        IFinanceOperationsService financeService,
        ICashboxAdminService cashboxService,
        IPartyService partyService,
        ILookupService lookupService,
        IPermissionService permissionService)
    {
        _financeService = financeService;
        _cashboxService = cashboxService;
        _partyService = partyService;
        _lookupService = lookupService;

        var options = new List<FinanceOperationOption>();
        if (permissionService.Can("Finance.Expense"))
            options.Add(new(FinanceOperationType.Expense, "تسجيل مصروف"));
        if (permissionService.Can("Finance.CustomerCollection"))
            options.Add(new(FinanceOperationType.CustomerCollection, "سند قبض من عميل"));
        if (permissionService.Can("Finance.SupplierPayment"))
            options.Add(new(FinanceOperationType.SupplierPayment, "سند دفع لمورد"));
        if (permissionService.Can("Finance.OwnerMovement"))
        {
            options.Add(new(FinanceOperationType.OwnerDeposit, "إيداع مالك"));
            options.Add(new(FinanceOperationType.OwnerWithdrawal, "سحب مالك"));
        }
        if (permissionService.Can("Finance.CashTransfer"))
            options.Add(new(FinanceOperationType.CashTransfer, "تحويل بين الخزائن"));
        OperationOptions = new ObservableCollection<FinanceOperationOption>(options);
        SelectedOperation = OperationOptions.FirstOrDefault();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var cashboxesTask = _cashboxService.GetAsync();
            var customersTask = _partyService.SearchCustomersAsync(new PartySearchCriteria());
            var suppliersTask = _partyService.SearchSuppliersAsync(new PartySearchCriteria());
            var categoriesTask = _lookupService.GetExpenseCategoriesAsync();
            await Task.WhenAll(cashboxesTask, customersTask, suppliersTask, categoriesTask);
            Cashboxes = new ObservableCollection<CashboxListItem>(cashboxesTask.Result);
            Customers = new ObservableCollection<CustomerListItem>(customersTask.Result);
            Suppliers = new ObservableCollection<SupplierListItem>(suppliersTask.Result);
            ExpenseCategories = new ObservableCollection<ExpenseCategory>(categoriesTask.Result);
            SelectedCashbox = Cashboxes.FirstOrDefault();
            DestinationCashbox = Cashboxes.Skip(1).FirstOrDefault();
            SelectedCustomer = Customers.FirstOrDefault();
            SelectedSupplier = Suppliers.FirstOrDefault();
            SelectedExpenseCategory = ExpenseCategories.FirstOrDefault();
            await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var movementsTask = _financeService.GetMovementsAsync(FilterFrom, FilterTo);
            var expensesTask = _financeService.GetExpensesAsync(FilterFrom, FilterTo);
            await Task.WhenAll(movementsTask, expensesTask);
            Movements = new ObservableCollection<CashMovementListItem>(movementsTask.Result);
            Expenses = new ObservableCollection<ExpenseListItem>(expensesTask.Result);
            TotalIn = Movements.Where(m => m.Direction == "In").Sum(m => m.Amount);
            TotalOut = Movements.Where(m => m.Direction == "Out").Sum(m => m.Amount);
        }
        catch (Exception ex) { ShowError(ex.Message); }
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (SelectedOperation is null || SelectedCashbox is null)
        {
            ShowError("اختر نوع الحركة والخزينة.");
            return;
        }
        IsBusy = true;
        try
        {
            await _financeService.ExecuteAsync(new FinanceOperationRequest
            {
                OperationType = SelectedOperation.Value,
                CashboxId = SelectedCashbox.Id,
                DestinationCashboxId = DestinationCashbox?.Id,
                CustomerId = SelectedCustomer?.Id,
                SupplierId = SelectedSupplier?.Id,
                ExpenseCategoryId = SelectedExpenseCategory?.Id,
                Amount = Amount,
                OperationDate = OperationDate,
                Notes = Notes
            });
            Cashboxes = new ObservableCollection<CashboxListItem>(await _cashboxService.GetAsync());
            SelectedCashbox = Cashboxes.FirstOrDefault(c => c.Id == SelectedCashbox.Id) ?? Cashboxes.FirstOrDefault();
            DestinationCashbox = Cashboxes.FirstOrDefault(c => c.Id == DestinationCashbox?.Id);
            Amount = 0;
            Notes = string.Empty;
            await LoadAsync();
            ShowSuccess("تم تسجيل الحركة وتحديث الأرصدة بنجاح ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    partial void OnSelectedOperationChanged(FinanceOperationOption? value)
    {
        OnPropertyChanged(nameof(NeedsExpenseCategory));
        OnPropertyChanged(nameof(NeedsCustomer));
        OnPropertyChanged(nameof(NeedsSupplier));
        OnPropertyChanged(nameof(NeedsDestination));
    }

    private void ShowError(string message) { StatusMessage = message; IsSuccess = false; }
    private void ShowSuccess(string message) { StatusMessage = message; IsSuccess = true; }
}
