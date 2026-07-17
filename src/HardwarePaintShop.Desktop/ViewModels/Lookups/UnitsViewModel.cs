using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Lookups;

public partial class UnitsViewModel : BaseViewModel
{
    private readonly ILookupService _lookup;

    [ObservableProperty] private ObservableCollection<Unit> _items = new();
    [ObservableProperty] private Unit? _selectedItem;

    [ObservableProperty] private string _formName   = string.Empty;
    [ObservableProperty] private string _formShortName = string.Empty;
    [ObservableProperty] private bool   _isEditing;
    [ObservableProperty] private Guid?  _editingId;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isSuccess;
    [ObservableProperty] private string _searchText = string.Empty;

    public UnitsViewModel(ILookupService lookup)
    {
        _lookup = lookup;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => FilterItems();

    private List<Unit> _allItems = new();

    private void FilterItems()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allItems
            : _allItems.Where(u => u.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Items = new ObservableCollection<Unit>(filtered);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try { _allItems = await _lookup.GetUnitsAsync(); FilterItems(); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void NewItem()
    {
        IsEditing = false; EditingId = null;
        FormName = string.Empty; FormShortName = string.Empty;
        StatusMessage = string.Empty; SelectedItem = null;
    }

    [RelayCommand]
    private void EditItem(Unit item)
    {
        IsEditing = true; EditingId = item.Id;
        FormName = item.Name; FormShortName = item.ShortName ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { ShowError("الاسم مطلوب."); return; }
        IsBusy = true;
        try
        {
            if (IsEditing && EditingId.HasValue)
                await _lookup.UpdateUnitAsync(EditingId.Value, FormName,
                    string.IsNullOrWhiteSpace(FormShortName) ? null : FormShortName,
                    null);
            else
                await _lookup.AddUnitAsync(FormName,
                    string.IsNullOrWhiteSpace(FormShortName) ? null : FormShortName,
                    null);

            ShowSuccess("تم الحفظ ✓"); NewItemCommand.Execute(null); await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync(Unit item)
    {
        IsBusy = true;
        try { await _lookup.DeactivateUnitAsync(item.Id); ShowSuccess("تم الحذف ✓"); await LoadAsync(); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void ShowError(string msg)   { StatusMessage = msg; IsSuccess = false; }
    private void ShowSuccess(string msg) { StatusMessage = msg; IsSuccess = true; }
}
