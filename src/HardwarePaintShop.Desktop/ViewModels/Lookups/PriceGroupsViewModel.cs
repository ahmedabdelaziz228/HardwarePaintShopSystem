using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Lookups;

public partial class PriceGroupsViewModel : BaseViewModel
{
    private readonly ILookupService _lookup;

    [ObservableProperty] private ObservableCollection<PriceGroup> _items = new();
    [ObservableProperty] private PriceGroup? _selectedItem;
    [ObservableProperty] private string _formName        = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private bool   _formIsDefault;
    [ObservableProperty] private bool   _isEditing;
    [ObservableProperty] private Guid?  _editingId;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isSuccess;
    [ObservableProperty] private string _searchText = string.Empty;

    private List<PriceGroup> _allItems = new();

    public PriceGroupsViewModel(ILookupService lookup)
    { _lookup = lookup; _ = LoadAsync(); }

    partial void OnSearchTextChanged(string value) => FilterItems();

    private void FilterItems()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allItems
            : _allItems.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Items = new ObservableCollection<PriceGroup>(filtered);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try { _allItems = await _lookup.GetPriceGroupsAsync(); FilterItems(); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void NewItem()
    {
        IsEditing = false; EditingId = null; SelectedItem = null;
        FormName = string.Empty; FormDescription = string.Empty; FormIsDefault = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void EditItem(PriceGroup item)
    {
        IsEditing = true; EditingId = item.Id; SelectedItem = item;
        FormName = item.Name; FormDescription = item.Description ?? string.Empty;
        FormIsDefault = item.IsDefault;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { ShowError("الاسم مطلوب."); return; }
        IsBusy = true;
        try
        {
            if (IsEditing && EditingId.HasValue)
                await _lookup.UpdatePriceGroupAsync(EditingId.Value, FormName,
                    string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription, FormIsDefault);
            else
                await _lookup.AddPriceGroupAsync(FormName,
                    string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription, FormIsDefault);
            ShowSuccess("تم الحفظ ✓"); NewItemCommand.Execute(null); await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync(PriceGroup item)
    {
        IsBusy = true;
        try { await _lookup.DeactivatePriceGroupAsync(item.Id); ShowSuccess("تم الحذف ✓"); await LoadAsync(); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void ShowError(string msg)   { StatusMessage = msg; IsSuccess = false; }
    private void ShowSuccess(string msg) { StatusMessage = msg; IsSuccess = true; }
}
