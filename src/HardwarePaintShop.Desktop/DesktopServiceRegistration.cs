using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Services;
using HardwarePaintShop.Desktop.ViewModels.Dashboard;
using HardwarePaintShop.Desktop.ViewModels.Finance;
using HardwarePaintShop.Desktop.ViewModels.Lookups;
using HardwarePaintShop.Desktop.ViewModels.Inventory;
using HardwarePaintShop.Desktop.ViewModels.Parties;
using HardwarePaintShop.Desktop.ViewModels.Products;
using HardwarePaintShop.Desktop.ViewModels.Reports;
using HardwarePaintShop.Desktop.ViewModels.Purchases;
using HardwarePaintShop.Desktop.ViewModels.Returns;
using HardwarePaintShop.Desktop.ViewModels.Sales;
using HardwarePaintShop.Desktop.ViewModels.Settings;
using HardwarePaintShop.Desktop.ViewModels.Shell;
using HardwarePaintShop.Desktop.ViewModels.Users;
using HardwarePaintShop.Desktop.Views.Shell;
using HardwarePaintShop.Infrastructure.Data;
using HardwarePaintShop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwarePaintShop.Desktop;

public static class DesktopServiceRegistration
{
    public static IServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        var connectionString =
            Environment.GetEnvironmentVariable("HARDWARE_PAINT_SHOP_CONNECTION_STRING")
            ?? configuration.GetConnectionString("DefaultConnection");
        services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IPermissionService, PermissionService>();
        services.AddTransient<ILookupService, LookupService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IAuditService, AuditService>();
        services.AddTransient<IStockMovementService, StockMovementService>();
        services.AddTransient<ICashMovementService, CashMovementService>();
        services.AddTransient<ICashboxAdminService, CashboxAdminService>();
        services.AddTransient<IPurchaseService, PurchaseService>();
        services.AddTransient<ISalesService, SalesService>();
        services.AddTransient<IFinanceOperationsService, FinanceOperationsService>();
        services.AddTransient<IReturnService, ReturnService>();
        services.AddTransient<IInventoryService, InventoryService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<ISettingsBackupService, SettingsBackupService>();
        services.AddTransient<ILicenseService, LicenseService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<IPartyService, PartyService>();
        services.AddTransient<IDashboardService, DashboardService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<UnitsViewModel>();
        services.AddTransient<PriceGroupsViewModel>();
        services.AddTransient<ExpenseCategoriesViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<RolesViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<PriceInquiryViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<CashboxesViewModel>();
        services.AddTransient<PurchaseInvoicesViewModel>();
        services.AddTransient<SalesPosViewModel>();
        services.AddTransient<FinanceOperationsViewModel>();
        services.AddTransient<ReturnsViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<AlertsViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<LicenseViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
    }
}
