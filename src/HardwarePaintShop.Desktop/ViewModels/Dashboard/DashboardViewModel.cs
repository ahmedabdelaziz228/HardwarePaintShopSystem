using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Dashboard;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDashboardService _dashboardService;
    private readonly IAuthService? _authService;

    [ObservableProperty] private decimal _todaySales;
    [ObservableProperty] private decimal _estimatedTodayProfit;
    [ObservableProperty] private decimal _inventoryValue;
    [ObservableProperty] private decimal _salesChangePercentage;
    [ObservableProperty] private decimal _todayCollections;
    [ObservableProperty] private decimal _todayExpenses;
    [ObservableProperty] private decimal _cashboxBalance;
    [ObservableProperty] private decimal _customerReceivables;
    [ObservableProperty] private decimal _supplierPayables;
    [ObservableProperty] private int _salesInvoiceCount;
    [ObservableProperty] private int _purchaseInvoiceCount;
    [ObservableProperty] private int _activeProductCount;
    [ObservableProperty] private int _activeCustomerCount;
    [ObservableProperty] private int _activeSupplierCount;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private string _greeting = string.Empty;
    [ObservableProperty] private string _lastUpdatedText = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<LowStockProductSummary> _lowStockProducts = new();
    [ObservableProperty] private ObservableCollection<DashboardTrendPoint> _weeklyTrend = new();
    [ObservableProperty] private ObservableCollection<InventoryCategorySummary> _inventoryByCategory = new();
    [ObservableProperty] private ObservableCollection<RecentOperationSummary> _recentOperations = new();

    public DashboardViewModel(IDashboardService dashboardService, IAuthService? authService = null)
    {
        _dashboardService = dashboardService;
        _authService = authService;
        UpdateGreeting();
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var summary = await _dashboardService.GetSummaryAsync();
            TodaySales = summary.TodaySales;
            EstimatedTodayProfit = summary.EstimatedTodayProfit;
            InventoryValue = summary.InventoryValue;
            SalesChangePercentage = summary.SalesChangePercentage;
            TodayCollections = summary.TodayCollections;
            TodayExpenses = summary.TodayExpenses;
            CashboxBalance = summary.CashboxBalance;
            CustomerReceivables = summary.CustomerReceivables;
            SupplierPayables = summary.SupplierPayables;
            SalesInvoiceCount = summary.SalesInvoiceCount;
            PurchaseInvoiceCount = summary.PurchaseInvoiceCount;
            ActiveProductCount = summary.ActiveProductCount;
            ActiveCustomerCount = summary.ActiveCustomerCount;
            ActiveSupplierCount = summary.ActiveSupplierCount;
            LowStockCount = summary.LowStockCount;
            LowStockProducts = new ObservableCollection<LowStockProductSummary>(summary.LowStockProducts);
            WeeklyTrend = new ObservableCollection<DashboardTrendPoint>(summary.WeeklyTrend);
            InventoryByCategory = new ObservableCollection<InventoryCategorySummary>(summary.InventoryByCategory);
            RecentOperations = new ObservableCollection<RecentOperationSummary>(summary.RecentOperations);
            LastUpdatedText = $"آخر تحديث {DateTime.Now:hh:mm tt}";
            UpdateGreeting();
        }
        catch (Exception ex)
        {
            StatusMessage = $"تعذر تحميل مؤشرات لوحة التحكم: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        var welcome = hour < 12 ? "صباح الخير" : hour < 18 ? "مساء الخير" : "مساء النور";
        var userName = _authService?.CurrentUserName;
        Greeting = string.IsNullOrWhiteSpace(userName) ? welcome : $"{welcome}، {userName}";
    }
}
