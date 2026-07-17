using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Finance;

public partial class CashboxesViewModel : BaseViewModel
{
    private readonly ICashboxAdminService _cashboxService;

    [ObservableProperty] private ObservableCollection<CashboxListItem> _cashboxes = new();
    [ObservableProperty] private CashboxListItem? _selectedCashbox;
    [ObservableProperty] private bool _includeInactive;
    [ObservableProperty] private Guid? _editingId;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private decimal _formOpeningBalance;
    [ObservableProperty] private bool _formIsActive = true;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccess;
    [ObservableProperty] private decimal _totalBalance;

    public bool CanCreate { get; }
    public bool CanUpdate { get; }
    public bool CanDeactivate { get; }

    public CashboxesViewModel(ICashboxAdminService cashboxService, IPermissionService permissionService)
    {
        _cashboxService = cashboxService;
        CanCreate = permissionService.Can("Cashbox.Create");
        CanUpdate = permissionService.Can("Cashbox.Update");
        CanDeactivate = permissionService.Can("Cashbox.Deactivate");
        NewCashbox();
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _cashboxService.GetAsync(IncludeInactive);
            Cashboxes = new ObservableCollection<CashboxListItem>(result);
            TotalBalance = result.Where(c => c.IsActive).Sum(c => c.CurrentBalance);
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void NewCashbox()
    {
        EditingId = null;
        IsEditing = false;
        SelectedCashbox = null;
        FormName = string.Empty;
        FormOpeningBalance = 0;
        FormIsActive = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void EditCashbox(CashboxListItem? item)
    {
        if (item is null || !CanUpdate)
            return;
        EditingId = item.Id;
        IsEditing = true;
        SelectedCashbox = item;
        FormName = item.Name;
        FormOpeningBalance = item.OpeningBalance;
        FormIsActive = item.IsActive;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if ((IsEditing && !CanUpdate) || (!IsEditing && !CanCreate))
        {
            ShowError("ليس لديك صلاحية حفظ الخزائن.");
            return;
        }
        IsBusy = true;
        try
        {
            await _cashboxService.SaveAsync(new CashboxSaveRequest
            {
                Id = EditingId,
                Name = FormName,
                OpeningBalance = FormOpeningBalance,
                IsActive = FormIsActive
            });
            await LoadAsync();
            NewCashbox();
            ShowSuccess("تم حفظ الخزينة بنجاح ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CashboxListItem? item)
    {
        if (item is null || !CanDeactivate)
            return;
        IsBusy = true;
        try
        {
            await _cashboxService.SetActiveAsync(item.Id, !item.IsActive);
            await LoadAsync();
            ShowSuccess(item.IsActive ? "تم تعطيل الخزينة." : "تم تفعيل الخزينة.");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void ShowError(string message) { StatusMessage = message; IsSuccess = false; }
    private void ShowSuccess(string message) { StatusMessage = message; IsSuccess = true; }
}
