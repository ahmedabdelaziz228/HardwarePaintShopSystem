using FluentAssertions;
using HardwarePaintShop.Application.Helpers;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Application.Services;
using HardwarePaintShop.Desktop;
using HardwarePaintShop.Desktop.ViewModels.Dashboard;
using HardwarePaintShop.Desktop.ViewModels.Finance;
using HardwarePaintShop.Desktop.ViewModels.Lookups;
using HardwarePaintShop.Desktop.ViewModels.Inventory;
using HardwarePaintShop.Desktop.ViewModels.Parties;
using HardwarePaintShop.Desktop.ViewModels.Products;
using HardwarePaintShop.Desktop.ViewModels.Purchases;
using HardwarePaintShop.Desktop.ViewModels.Reports;
using HardwarePaintShop.Desktop.ViewModels.Returns;
using HardwarePaintShop.Desktop.ViewModels.Sales;
using HardwarePaintShop.Desktop.ViewModels.Settings;
using HardwarePaintShop.Desktop.ViewModels.Shell;
using HardwarePaintShop.Desktop.ViewModels.Users;
using HardwarePaintShop.Desktop.Views.Shell;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HardwarePaintShop.UnitTests;

public class FoundationTests
{
    [Fact]
    public void UnitConverter_ToBase_Works()
    {
        UnitConverter.ToBase(2, 25).Should().Be(50);
    }

    [Fact]
    public void UnitConverter_FromBase_Works()
    {
        UnitConverter.FromBase(50, 25).Should().Be(2);
    }

    [Fact]
    public void InvoiceCalculator_LineTotal_Works()
    {
        InvoiceCalculator.CalculateLineTotal(3, 10, 5).Should().Be(25);
    }

    [Fact]
    public void InvoiceCalculator_ReturnsPaidStatus()
    {
        InvoiceCalculator.DeterminePaymentStatus(100, 100).Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public void InvoiceCalculator_ReturnsPartialStatus()
    {
        InvoiceCalculator.DeterminePaymentStatus(100, 40).Should().Be(PaymentStatus.Partial);
    }

    [Fact]
    public void InvoiceCalculator_ReturnsUnpaidStatus()
    {
        InvoiceCalculator.DeterminePaymentStatus(100, 0).Should().Be(PaymentStatus.Unpaid);
    }

    [Fact]
    public void PasswordHasher_HashAndVerify_Works()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("admin123");

        hash.Should().NotBe("admin123");
        hasher.VerifyPassword("admin123", hash).Should().BeTrue();
        hasher.VerifyPassword("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void DI_ImportantServicesCanResolve()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=hardware_paint_shop;Username=postgres;Password=CHANGE_ME"
            })
            .Build();

        var services = new ServiceCollection();
        DesktopServiceRegistration.ConfigureServices(services, configuration);

        services.Any(x => x.ServiceType == typeof(LoginWindow)).Should().BeTrue();
        services.Any(x => x.ServiceType == typeof(MainWindow)).Should().BeTrue();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        provider.GetRequiredService<IAuthService>().Should().NotBeNull();
        provider.GetRequiredService<IPermissionService>().Should().NotBeNull();
        provider.GetRequiredService<ILookupService>().Should().NotBeNull();
        provider.GetRequiredService<IUserService>().Should().NotBeNull();
        provider.GetRequiredService<IPasswordHasher>().Should().NotBeNull();
        provider.GetRequiredService<IAuditService>().Should().NotBeNull();
        provider.GetRequiredService<IStockMovementService>().Should().NotBeNull();
        provider.GetRequiredService<ICashMovementService>().Should().NotBeNull();
        provider.GetRequiredService<ICashboxAdminService>().Should().NotBeNull();
        provider.GetRequiredService<IPurchaseService>().Should().NotBeNull();
        provider.GetRequiredService<ISalesService>().Should().NotBeNull();
        provider.GetRequiredService<IFinanceOperationsService>().Should().NotBeNull();
        provider.GetRequiredService<IReturnService>().Should().NotBeNull();
        provider.GetRequiredService<IInventoryService>().Should().NotBeNull();
        provider.GetRequiredService<IReportService>().Should().NotBeNull();
        provider.GetRequiredService<ISettingsBackupService>().Should().NotBeNull();
        provider.GetRequiredService<ILicenseService>().Should().NotBeNull();
        provider.GetRequiredService<IProductService>().Should().NotBeNull();
        provider.GetRequiredService<IPartyService>().Should().NotBeNull();
        provider.GetRequiredService<IDashboardService>().Should().NotBeNull();
        provider.GetRequiredService<LoginViewModel>().Should().NotBeNull();
        provider.GetRequiredService<MainViewModel>().Should().NotBeNull();
        provider.GetRequiredService<DashboardViewModel>().Should().NotBeNull();
        provider.GetRequiredService<CategoriesViewModel>().Should().NotBeNull();
        provider.GetRequiredService<UnitsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<PriceGroupsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<ExpenseCategoriesViewModel>().Should().NotBeNull();
        provider.GetRequiredService<UsersViewModel>().Should().NotBeNull();
        provider.GetRequiredService<RolesViewModel>().Should().NotBeNull();
        provider.GetRequiredService<ProductsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<PriceInquiryViewModel>().Should().NotBeNull();
        provider.GetRequiredService<CustomersViewModel>().Should().NotBeNull();
        provider.GetRequiredService<CustomerStatementViewModel>().Should().NotBeNull();
        provider.GetRequiredService<SuppliersViewModel>().Should().NotBeNull();
        provider.GetRequiredService<CashboxesViewModel>().Should().NotBeNull();
        provider.GetRequiredService<PurchaseInvoicesViewModel>().Should().NotBeNull();
        provider.GetRequiredService<SalesPosViewModel>().Should().NotBeNull();
        provider.GetRequiredService<FinanceOperationsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<ReturnsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<InventoryViewModel>().Should().NotBeNull();
        provider.GetRequiredService<AlertsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<ReportsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<SettingsViewModel>().Should().NotBeNull();
        provider.GetRequiredService<BackupViewModel>().Should().NotBeNull();
        provider.GetRequiredService<LicenseViewModel>().Should().NotBeNull();
    }

    [Fact]
    public void LookupService_UsesCorrectCategoryParentProperty()
    {
        var parentId = Guid.NewGuid();
        var category = new Category { Id = Guid.NewGuid(), Name = "Child", ParentCategoryId = parentId };
        var node = new CategoryNode(category);

        node.ParentCategoryId.Should().Be(parentId);
    }

    [Fact]
    public void Lookup_ViewModelsExposePropertiesAndCommandsUsedByXaml()
    {
        var lookup = new Mock<ILookupService>();
        lookup.Setup(x => x.GetUnitsAsync()).ReturnsAsync(new List<Unit>());
        lookup.Setup(x => x.GetPriceGroupsAsync()).ReturnsAsync(new List<PriceGroup>());
        lookup.Setup(x => x.GetExpenseCategoriesAsync()).ReturnsAsync(new List<ExpenseCategory>());

        var units = new UnitsViewModel(lookup.Object);
        units.Items.Should().NotBeNull();
        units.SelectedItem.Should().BeNull();
        units.FormShortName.Should().BeEmpty();
        units.NewItemCommand.Should().NotBeNull();
        units.EditItemCommand.Should().NotBeNull();
        units.DeleteCommand.Should().NotBeNull();
        units.LoadCommand.Should().NotBeNull();

        var priceGroups = new PriceGroupsViewModel(lookup.Object);
        priceGroups.Items.Should().NotBeNull();
        priceGroups.SelectedItem.Should().BeNull();
        priceGroups.SearchText.Should().BeEmpty();
        priceGroups.NewItemCommand.Should().NotBeNull();
        priceGroups.EditItemCommand.Should().NotBeNull();
        priceGroups.DeleteCommand.Should().NotBeNull();
        priceGroups.LoadCommand.Should().NotBeNull();

        var expenseCategories = new ExpenseCategoriesViewModel(lookup.Object);
        expenseCategories.Items.Should().NotBeNull();
        expenseCategories.SelectedItem.Should().BeNull();
        expenseCategories.SearchText.Should().BeEmpty();
        expenseCategories.NewItemCommand.Should().NotBeNull();
        expenseCategories.EditItemCommand.Should().NotBeNull();
        expenseCategories.DeleteCommand.Should().NotBeNull();
        expenseCategories.LoadCommand.Should().NotBeNull();

        var dashboardService = new Mock<IDashboardService>();
        dashboardService.Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSummary());
        var dashboard = new DashboardViewModel(dashboardService.Object);
        dashboard.RefreshCommand.Should().NotBeNull();
    }
}
