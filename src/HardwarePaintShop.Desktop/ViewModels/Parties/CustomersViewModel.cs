using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Desktop.ViewModels;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;
using HardwarePaintShop.Desktop.Views.Parties;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HardwarePaintShop.Desktop.ViewModels.Parties;

public partial class CustomersViewModel : BaseViewModel
{
    private readonly IPartyService _partyService;
    private readonly ILookupService _lookupService;

    [ObservableProperty] private ObservableCollection<CustomerListItem> _customers = new();
    [ObservableProperty] private ObservableCollection<PriceGroup> _priceGroups = new();
    [ObservableProperty] private ObservableCollection<PartyLookupOption> _priceGroupOptions = new();
    [ObservableProperty] private CustomerListItem? _selectedCustomer;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _includeInactive;
    [ObservableProperty] private int _customerCount;
    [ObservableProperty] private decimal _totalReceivables;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private Guid? _editingId;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formPhone = string.Empty;
    [ObservableProperty] private string _formAddress = string.Empty;
    [ObservableProperty] private string _formCustomerType = "قطاعي";
    [ObservableProperty] private Guid? _formPriceGroupId;
    [ObservableProperty] private decimal _formCreditLimit;
    [ObservableProperty] private decimal _formCurrentBalance;
    [ObservableProperty] private string _formNotes = string.Empty;
    [ObservableProperty] private bool _formIsActive = true;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;

    public IReadOnlyList<string> CustomerTypes { get; } =
        new[] { "قطاعي", "جملة", "مقاول", "شركة", "جهة حكومية" };
    public bool CanCreate { get; }
    public bool CanUpdate { get; }
    public bool CanDeactivate { get; }

    public CustomersViewModel(
        IPartyService partyService,
        ILookupService lookupService,
        IPermissionService permissionService)
    {
        _partyService = partyService;
        _lookupService = lookupService;
        CanCreate = permissionService.Can("Customer.Create");
        CanUpdate = permissionService.Can("Customer.Update");
        CanDeactivate = permissionService.Can("Customer.Deactivate");
        _ = InitializeAsync();
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            PriceGroups = new ObservableCollection<PriceGroup>(await _lookupService.GetPriceGroupsAsync());
            PriceGroupOptions.Clear();
            PriceGroupOptions.Add(new PartyLookupOption(null, "— السعر الافتراضي —"));
            foreach (var group in PriceGroups)
                PriceGroupOptions.Add(new PartyLookupOption(group.Id, group.Name));
            NewCustomer();
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
            var result = await _partyService.SearchCustomersAsync(
                new PartySearchCriteria(SearchText, IncludeInactive));
            Customers = new ObservableCollection<CustomerListItem>(result);
            CustomerCount = result.Count;
            TotalReceivables = result.Where(c => c.CurrentBalance > 0).Sum(c => c.CurrentBalance);
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
    private void NewCustomer()
    {
        IsEditing = false;
        EditingId = null;
        SelectedCustomer = null;
        FormName = string.Empty;
        FormPhone = string.Empty;
        FormAddress = string.Empty;
        FormCustomerType = "قطاعي";
        FormPriceGroupId = PriceGroups.FirstOrDefault(p => p.IsDefault)?.Id;
        FormCreditLimit = 0;
        FormCurrentBalance = 0;
        FormNotes = string.Empty;
        FormIsActive = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task EditCustomerAsync(CustomerListItem? item)
    {
        if (item is null)
            return;
        if (!CanUpdate)
        {
            ShowError("ليس لديك صلاحية تعديل العملاء.");
            return;
        }

        IsBusy = true;
        try
        {
            var customer = await _partyService.GetCustomerAsync(item.Id);
            EditingId = customer.Id;
            IsEditing = true;
            SelectedCustomer = item;
            FormName = customer.Name;
            FormPhone = customer.Phone ?? string.Empty;
            FormAddress = customer.Address ?? string.Empty;
            FormCustomerType = customer.CustomerType ?? "قطاعي";
            FormPriceGroupId = customer.PriceGroupId;
            FormCreditLimit = customer.CreditLimit;
            FormCurrentBalance = customer.CurrentBalance;
            FormNotes = customer.Notes ?? string.Empty;
            FormIsActive = customer.IsActive;
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
            ShowError("ليس لديك صلاحية تعديل العملاء.");
            return;
        }
        if (!IsEditing && !CanCreate)
        {
            ShowError("ليس لديك صلاحية إضافة العملاء.");
            return;
        }
        if (string.IsNullOrWhiteSpace(FormName))
        {
            ShowError("اسم العميل مطلوب.");
            return;
        }

        IsBusy = true;
        try
        {
            await _partyService.SaveCustomerAsync(new CustomerSaveRequest
            {
                Id = EditingId,
                Name = FormName,
                Phone = FormPhone,
                Address = FormAddress,
                CustomerType = FormCustomerType,
                PriceGroupId = FormPriceGroupId,
                CreditLimit = FormCreditLimit,
                Notes = FormNotes,
                IsActive = FormIsActive
            });
            await LoadAsync();
            NewCustomer();
            ShowSuccess("تم حفظ بيانات العميل بنجاح ✓");
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
    private async Task ToggleActiveAsync(CustomerListItem? item)
    {
        if (item is null)
            return;
        if (!CanDeactivate)
        {
            ShowError("ليس لديك صلاحية تعطيل أو تفعيل العملاء.");
            return;
        }

        IsBusy = true;
        try
        {
            await _partyService.SetCustomerActiveAsync(item.Id, !item.IsActive);
            await LoadAsync();
            ShowSuccess(item.IsActive ? "تم تعطيل العميل." : "تم تفعيل العميل.");
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
    private async Task OpenStatementAsync(CustomerListItem? item)
    {
        if (item is null)
        {
            ShowError("اختر العميل أولًا.");
            return;
        }
        try
        {
            var viewModel = App.Services.GetRequiredService<CustomerStatementViewModel>();
            await viewModel.InitializeAsync(item.Id);
            var window = new CustomerStatementWindow
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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

public sealed record PartyLookupOption(Guid? Id, string Name);
