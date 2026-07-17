using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Desktop.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace HardwarePaintShop.Desktop.ViewModels.Shell;

public partial class MainViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;
    private readonly IServiceProvider _services;
    private readonly ISettingsBackupService _settingsService;
    private readonly IInventoryService _inventoryService;

    [ObservableProperty] private string _shopName = "محل الحدايد والبوهيات";
    [ObservableProperty] private string _currentPageTitle = "لوحة التحكم";
    [ObservableProperty] private string _currentUserDisplay = string.Empty;
    [ObservableProperty] private int _unreadAlertCount;
    [ObservableProperty] private DateTime _currentDateTime = DateTime.Now;
    [ObservableProperty] private object? _currentPage;
    [ObservableProperty] private NavigationItem? _selectedNavigationItem;

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public MainViewModel(
        IAuthService authService,
        IPermissionService permissionService,
        ISettingsBackupService settingsService,
        IInventoryService inventoryService,
        IServiceProvider services)
    {
        _authService = authService;
        _permissionService = permissionService;
        _settingsService = settingsService;
        _inventoryService = inventoryService;
        _services = services;
        CurrentUserDisplay = _authService.CurrentUserName ?? string.Empty;

        BuildNavigationMenu();
        StartClock();
        _ = LoadShellDataAsync();

        CurrentPageTitle = "لوحة التحكم";
    }

    private async Task LoadShellDataAsync()
    {
        try
        {
            var settingsTask = _settingsService.GetSettingsAsync();
            var alertsTask = _inventoryService.RefreshAlertsAsync(true);
            await Task.WhenAll(settingsTask, alertsTask);
            ShopName = settingsTask.Result.ShopName;
            UnreadAlertCount = alertsTask.Result.Count;
        }
        catch
        {
            // The dashboard and individual pages show actionable errors; the shell remains usable.
        }
    }

    public void Initialize()
    {
        var dashboard = NavigationItems.FirstOrDefault(i => i.PageKey == "Dashboard");
        if (dashboard is not null)
            Navigate(dashboard);
    }

    private void BuildNavigationMenu()
    {
        NavigationItems.Clear();
        NavigationItems.Add(new NavigationItem("⌂", "لوحة التحكم",     "Dashboard"));
        if (_permissionService.Can("Product.View"))
            NavigationItems.Add(new NavigationItem("▣", "المنتجات",    "Products"));
        if (_permissionService.Can("PriceInquiry.View"))
            NavigationItems.Add(new NavigationItem("⌕", "استعلام سعر", "PriceInquiry"));
        if (_permissionService.Can("Customer.View"))
            NavigationItems.Add(new NavigationItem("♙", "العملاء",      "Customers"));
        if (_permissionService.Can("Supplier.View"))
            NavigationItems.Add(new NavigationItem("▤", "الموردين",     "Suppliers"));
        if (_permissionService.Can("Sales.View"))
            NavigationItems.Add(new NavigationItem("▧", "نقطة البيع POS", "SalesInvoices"));
        if (_permissionService.Can("Purchase.View"))
            NavigationItems.Add(new NavigationItem("▨", "فواتير الشراء", "PurchaseInvoices"));
        if (_permissionService.Can("Return.View"))
            NavigationItems.Add(new NavigationItem("↩", "المرتجعات", "Returns"));
        if (_permissionService.Can("Inventory.View"))
            NavigationItems.Add(new NavigationItem("▦", "المخزون والجرد", "Inventory"));
        if (_permissionService.Can("Cashbox.View"))
            NavigationItems.Add(new NavigationItem("¤", "الخزينة", "Cashbox"));
        if (_permissionService.Can("Finance.View"))
            NavigationItems.Add(new NavigationItem("≡", "الحسابات اليومية", "FinanceOperations"));
        if (_permissionService.Can("Report.View"))
            NavigationItems.Add(new NavigationItem("▥", "التقارير", "Reports"));
        if (_permissionService.Can("Alert.View"))
            NavigationItems.Add(new NavigationItem("○", "التنبيهات", "Alerts"));
        NavigationItems.Add(new NavigationItem("♟", "المستخدمين",      "Users"));
        NavigationItems.Add(new NavigationItem("◆", "الصلاحيات",       "Roles"));
        NavigationItems.Add(new NavigationItem("▰", "الأصناف",         "Categories"));
        NavigationItems.Add(new NavigationItem("↔", "الوحدات",         "Units"));
        NavigationItems.Add(new NavigationItem("⌑", "فئات الأسعار",    "PriceGroups"));
        NavigationItems.Add(new NavigationItem("▱", "أنواع المصروفات", "ExpenseCategories"));
        if (_permissionService.Can("Backup.View"))
            NavigationItems.Add(new NavigationItem("◫", "النسخ الاحتياطي", "Backup"));
        if (_permissionService.Can("Settings.View"))
            NavigationItems.Add(new NavigationItem("⚙", "الإعدادات", "Settings"));
        if (_permissionService.Can("License.View"))
            NavigationItems.Add(new NavigationItem("◇", "الترخيص", "License"));
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        if (!item.IsAvailable)
            return;
        foreach (var navigationItem in NavigationItems)
            navigationItem.IsSelected = false;
        item.IsSelected = true;
        SelectedNavigationItem = item;
        NavigateToKey(item.PageKey, item.Label);
    }

    private void NavigateToKey(string pageKey, string title)
    {
        CurrentPageTitle = title;
        CurrentPage = ResolveView(pageKey);
    }

    private UserControl? ResolveView(string pageKey) => pageKey switch
    {
        "Dashboard"        => Resolve<Views.Dashboard.DashboardView,  Dashboard.DashboardViewModel>(),
        "Products"         => Resolve<Views.Products.ProductsView,    Products.ProductsViewModel>(),
        "PriceInquiry"     => Resolve<Views.Products.PriceInquiryView, Products.PriceInquiryViewModel>(),
        "Customers"        => Resolve<Views.Parties.CustomersView,      Parties.CustomersViewModel>(),
        "Suppliers"        => Resolve<Views.Parties.SuppliersView,      Parties.SuppliersViewModel>(),
        "PurchaseInvoices" => Resolve<Views.Purchases.PurchaseInvoicesView, Purchases.PurchaseInvoicesViewModel>(),
        "Cashbox"          => Resolve<Views.Finance.CashboxesView, Finance.CashboxesViewModel>(),
        "SalesInvoices"    => Resolve<Views.Sales.SalesPosView, Sales.SalesPosViewModel>(),
        "FinanceOperations"=> Resolve<Views.Finance.FinanceOperationsView, Finance.FinanceOperationsViewModel>(),
        "Returns"          => Resolve<Views.Returns.ReturnsView, Returns.ReturnsViewModel>(),
        "Inventory"        => Resolve<Views.Inventory.InventoryView, Inventory.InventoryViewModel>(),
        "Alerts"           => Resolve<Views.Inventory.AlertsView, Inventory.AlertsViewModel>(),
        "Reports"          => Resolve<Views.Reports.ReportsView, Reports.ReportsViewModel>(),
        "Backup"           => Resolve<Views.Settings.BackupView, Settings.BackupViewModel>(),
        "Settings"         => Resolve<Views.Settings.SettingsView, Settings.SettingsViewModel>(),
        "License"          => Resolve<Views.Settings.LicenseView, Settings.LicenseViewModel>(),
        "Categories"       => Resolve<Views.Lookups.CategoriesView,   Lookups.CategoriesViewModel>(),
        "Units"            => Resolve<Views.Lookups.UnitsView,         Lookups.UnitsViewModel>(),
        "PriceGroups"      => Resolve<Views.Lookups.PriceGroupsView,   Lookups.PriceGroupsViewModel>(),
        "ExpenseCategories"=> Resolve<Views.Lookups.ExpenseCategoriesView, Lookups.ExpenseCategoriesViewModel>(),
        "Users"            => Resolve<Views.Users.UsersView,           Users.UsersViewModel>(),
        "Roles"            => Resolve<Views.Users.RolesView,           Users.RolesViewModel>(),
        _                  => new Views.Shell.NotImplementedView()
    };

    /// <summary>Creates view, injects VM from DI, and sets DataContext.</summary>
    private TView Resolve<TView, TViewModel>()
        where TView     : UserControl, new()
        where TViewModel : class
    {
        var view = new TView();
        var vm   = _services.GetService(typeof(TViewModel)) as TViewModel;
        view.DataContext = vm;
        return view;
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        var loginWindow = new Views.Shell.LoginWindow(
            App.Services.GetService(typeof(LoginViewModel)) as LoginViewModel
            ?? throw new InvalidOperationException());
        loginWindow.Show();

        foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
        {
            if (w is Views.Shell.MainWindow) { w.Close(); break; }
        }
    }

    private void StartClock()
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        timer.Tick += (_, _) => CurrentDateTime = DateTime.Now;
        timer.Start();
    }
}

public partial class NavigationItem : ObservableObject
{
    public string Icon { get; }
    public string Label { get; }
    public string PageKey { get; }
    public bool IsAvailable { get; }
    [ObservableProperty] private bool _isSelected;

    public NavigationItem(string icon, string label, string pageKey, bool isAvailable = true)
    {
        Icon = icon;
        Label = label;
        PageKey = pageKey;
        IsAvailable = isAvailable;
    }
}
