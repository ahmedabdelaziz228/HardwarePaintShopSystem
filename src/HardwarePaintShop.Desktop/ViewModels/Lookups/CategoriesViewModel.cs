using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Lookups;

public partial class CategoriesViewModel : BaseViewModel
{
    private readonly ILookupService _lookup;

    [ObservableProperty] private ObservableCollection<CategoryNode> _nodes = new();
    [ObservableProperty] private CategoryNode? _selectedNode;

    // Form fields
    [ObservableProperty] private string _formName    = string.Empty;
    [ObservableProperty] private Guid?  _formParentId;
    [ObservableProperty] private string _formNotes   = string.Empty;
    [ObservableProperty] private bool   _isEditing;
    [ObservableProperty] private Guid?  _editingId;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isSuccess;

    public ObservableCollection<CategoryNode> FlatList { get; } = new();
    public ObservableCollection<ParentOption> ParentOptions { get; } = new();

    public CategoriesViewModel(ILookupService lookup)
    {
        _lookup = lookup;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var cats = await _lookup.GetCategoriesAsync();
            BuildTree(cats);
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void BuildTree(List<Category> cats)
    {
        Nodes.Clear(); FlatList.Clear(); ParentOptions.Clear();
        ParentOptions.Add(new ParentOption(null, "— بدون أب —"));

        var dict = cats.ToDictionary(c => c.Id, c => new CategoryNode(c));
        foreach (var node in dict.Values)
            FlatList.Add(node);

        foreach (var cat in cats)
        {
            if (cat.ParentCategoryId.HasValue && dict.TryGetValue(cat.ParentCategoryId.Value, out var parent))
                parent.Children.Add(dict[cat.Id]);
            else
                Nodes.Add(dict[cat.Id]);
        }

        foreach (var cat in cats.OrderBy(c => c.Name))
            ParentOptions.Add(new ParentOption(cat.Id, cat.Name));
    }

    [RelayCommand]
    private void NewCategory()
    {
        IsEditing = false; EditingId = null;
        FormName = string.Empty; FormParentId = null; FormNotes = string.Empty;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void EditCategory(CategoryNode node)
    {
        IsEditing = true; EditingId = node.Id;
        FormName = node.Name; FormParentId = node.ParentCategoryId; FormNotes = node.Notes ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { ShowError("الاسم مطلوب."); return; }
        IsBusy = true;
        try
        {
            if (IsEditing && EditingId.HasValue)
                await _lookup.UpdateCategoryAsync(EditingId.Value, FormName, FormParentId, FormNotes);
            else
                await _lookup.AddCategoryAsync(FormName, FormParentId, FormNotes);

            ShowSuccess("تم الحفظ بنجاح ✓");
            NewCategoryCommand.Execute(null);
            await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync(CategoryNode node)
    {
        IsBusy = true;
        try
        {
            await _lookup.DeactivateCategoryAsync(node.Id);
            ShowSuccess("تم الحذف بنجاح ✓");
            await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void ShowError(string msg)   { StatusMessage = msg; IsSuccess = false; }
    private void ShowSuccess(string msg) { StatusMessage = msg; IsSuccess = true; }
}

public partial class CategoryNode : ObservableObject
{
    public Guid    Id       { get; }
    public string  Name     { get; }
    public Guid?   ParentCategoryId { get; }
    public string? Notes    { get; }
    public ObservableCollection<CategoryNode> Children { get; } = new();

    public CategoryNode(Category c)
    {
        Id = c.Id; Name = c.Name; ParentCategoryId = c.ParentCategoryId; Notes = c.Notes;
    }
}

public record ParentOption(Guid? Id, string Name);
