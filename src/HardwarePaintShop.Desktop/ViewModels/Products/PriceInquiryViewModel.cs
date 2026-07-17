using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Desktop.ViewModels;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Products;

public partial class PriceInquiryViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly ILookupService _lookupService;
    private readonly bool _canViewCost;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Guid? _selectedPriceGroupId;
    [ObservableProperty] private ObservableCollection<LookupOption> _priceGroupOptions = new();
    [ObservableProperty] private ObservableCollection<PriceInquiryResult> _results = new();
    [ObservableProperty] private string _statusMessage = "اكتب اسم المنتج أو الكود أو امسح الباركود أو السيريال.";
    [ObservableProperty] private bool _isSuccess = true;

    public PriceInquiryViewModel(
        IProductService productService,
        ILookupService lookupService,
        IPermissionService permissionService)
    {
        _productService = productService;
        _lookupService = lookupService;
        _canViewCost = permissionService.Can("Product.ViewCost");
        _ = LoadLookupsAsync();
    }

    [RelayCommand]
    private async Task LoadLookupsAsync()
    {
        try
        {
            var groups = await _lookupService.GetPriceGroupsAsync();
            PriceGroupOptions.Clear();
            PriceGroupOptions.Add(new LookupOption(null, "كل فئات الأسعار"));
            foreach (var group in groups)
                PriceGroupOptions.Add(new LookupOption(group.Id, group.Name));
            SelectedPriceGroupId = groups.FirstOrDefault(g => g.IsDefault)?.Id;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Results.Clear();
            ShowError("أدخل اسم المنتج أو الكود أو الباركود أو السيريال.");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _productService.InquirePriceAsync(SearchText, SelectedPriceGroupId);
            if (!_canViewCost)
                result = result.Select(r => r with { LastPurchasePriceBaseUnit = null }).ToList();
            Results = new ObservableCollection<PriceInquiryResult>(result);
            if (result.Count == 0)
                ShowError("لا توجد نتائج مطابقة.");
            else
                ShowSuccess($"تم العثور على {result.Select(r => r.ProductId).Distinct().Count()} منتج.");
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
    private void Clear()
    {
        SearchText = string.Empty;
        Results.Clear();
        ShowSuccess("اكتب اسم المنتج أو الكود أو امسح الباركود أو السيريال.");
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
