using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Desktop.ViewModels;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Parties;

public partial class SuppliersViewModel : BaseViewModel
{
    private readonly IPartyService _partyService;

    [ObservableProperty] private ObservableCollection<SupplierListItem> _suppliers = new();
    [ObservableProperty] private SupplierListItem? _selectedSupplier;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _includeInactive;
    [ObservableProperty] private int _supplierCount;
    [ObservableProperty] private decimal _totalPayables;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private Guid? _editingId;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formPhone = string.Empty;
    [ObservableProperty] private string _formAddress = string.Empty;
    [ObservableProperty] private decimal _formCurrentBalance;
    [ObservableProperty] private string _formNotes = string.Empty;
    [ObservableProperty] private bool _formIsActive = true;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;

    public bool CanCreate { get; }
    public bool CanUpdate { get; }
    public bool CanDeactivate { get; }

    public SuppliersViewModel(IPartyService partyService, IPermissionService permissionService)
    {
        _partyService = partyService;
        CanCreate = permissionService.Can("Supplier.Create");
        CanUpdate = permissionService.Can("Supplier.Update");
        CanDeactivate = permissionService.Can("Supplier.Deactivate");
        NewSupplier();
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _partyService.SearchSuppliersAsync(
                new PartySearchCriteria(SearchText, IncludeInactive));
            Suppliers = new ObservableCollection<SupplierListItem>(result);
            SupplierCount = result.Count;
            TotalPayables = result.Where(s => s.CurrentBalance > 0).Sum(s => s.CurrentBalance);
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
    private void NewSupplier()
    {
        IsEditing = false;
        EditingId = null;
        SelectedSupplier = null;
        FormName = string.Empty;
        FormPhone = string.Empty;
        FormAddress = string.Empty;
        FormCurrentBalance = 0;
        FormNotes = string.Empty;
        FormIsActive = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task EditSupplierAsync(SupplierListItem? item)
    {
        if (item is null)
            return;
        if (!CanUpdate)
        {
            ShowError("ليس لديك صلاحية تعديل الموردين.");
            return;
        }

        IsBusy = true;
        try
        {
            var supplier = await _partyService.GetSupplierAsync(item.Id);
            EditingId = supplier.Id;
            IsEditing = true;
            SelectedSupplier = item;
            FormName = supplier.Name;
            FormPhone = supplier.Phone ?? string.Empty;
            FormAddress = supplier.Address ?? string.Empty;
            FormCurrentBalance = supplier.CurrentBalance;
            FormNotes = supplier.Notes ?? string.Empty;
            FormIsActive = supplier.IsActive;
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
            ShowError("ليس لديك صلاحية تعديل الموردين.");
            return;
        }
        if (!IsEditing && !CanCreate)
        {
            ShowError("ليس لديك صلاحية إضافة الموردين.");
            return;
        }
        if (string.IsNullOrWhiteSpace(FormName))
        {
            ShowError("اسم المورد مطلوب.");
            return;
        }

        IsBusy = true;
        try
        {
            await _partyService.SaveSupplierAsync(new SupplierSaveRequest
            {
                Id = EditingId,
                Name = FormName,
                Phone = FormPhone,
                Address = FormAddress,
                Notes = FormNotes,
                IsActive = FormIsActive
            });
            await LoadAsync();
            NewSupplier();
            ShowSuccess("تم حفظ بيانات المورد بنجاح ✓");
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
    private async Task ToggleActiveAsync(SupplierListItem? item)
    {
        if (item is null)
            return;
        if (!CanDeactivate)
        {
            ShowError("ليس لديك صلاحية تعطيل أو تفعيل الموردين.");
            return;
        }

        IsBusy = true;
        try
        {
            await _partyService.SetSupplierActiveAsync(item.Id, !item.IsActive);
            await LoadAsync();
            ShowSuccess(item.IsActive ? "تم تعطيل المورد." : "تم تفعيل المورد.");
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
