using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Lookups;

public partial class ExpenseCategoriesViewModel : BaseViewModel
{
    private readonly ILookupService _lookup;

    [ObservableProperty] private ObservableCollection<ExpenseCategory> _items = new();
    [ObservableProperty] private ExpenseCategory? _selectedItem;
    [ObservableProperty] private string _formName    = string.Empty;
    [ObservableProperty] private bool   _isEditing;
    [ObservableProperty] private Guid?  _editingId;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isSuccess;
    [ObservableProperty] private string _searchText = string.Empty;

    private List<ExpenseCategory> _allItems = new();

    public ExpenseCategoriesViewModel(ILookupService lookup)
    { _lookup = lookup; _ = LoadAsync(); }

    partial void OnSearchTextChanged(string value) => FilterItems();

    private void FilterItems()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allItems
            : _allItems.Where(e => e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Items = new ObservableCollection<ExpenseCategory>(filtered);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try { _allItems = await _lookup.GetExpenseCategoriesAsync(); FilterItems(); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void NewItem()
    { IsEditing = false; EditingId = null; SelectedItem = null; FormName = string.Empty; StatusMessage = string.Empty; }

    [RelayCommand]
    private void EditItem(ExpenseCategory item)
    { IsEditing = true; EditingId = item.Id; SelectedItem = item; FormName = item.Name; }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { ShowError("الاسم مطلوب."); return; }
        IsBusy = true;
        try
        {
            if (IsEditing && EditingId.HasValue)
                await _lookup.UpdateExpenseCategoryAsync(EditingId.Value, FormName);
            else
                await _lookup.AddExpenseCategoryAsync(FormName);
            ShowSuccess("تم الحفظ ✓"); NewItemCommand.Execute(null); await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync(ExpenseCategory item)
    {
        IsBusy = true;
        try { await _lookup.DeactivateExpenseCategoryAsync(item.Id); ShowSuccess("تم الحذف ✓"); await LoadAsync(); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void ShowError(string msg)   { StatusMessage = msg; IsSuccess = false; }
    private void ShowSuccess(string msg) { StatusMessage = msg; IsSuccess = true; }
}
