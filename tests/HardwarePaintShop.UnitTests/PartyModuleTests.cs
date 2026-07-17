using FluentAssertions;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Desktop.ViewModels.Parties;
using HardwarePaintShop.Domain.Entities;
using Moq;
using Xunit;

namespace HardwarePaintShop.UnitTests;

public class PartyModuleTests
{
    [Fact]
    public async Task CustomersViewModel_LoadsReceivablesAndExposesCommands()
    {
        var partyService = new Mock<IPartyService>();
        partyService.Setup(x => x.SearchCustomersAsync(
                It.IsAny<PartySearchCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerListItem>
            {
                new(Guid.NewGuid(), "عميل نقدي", "01000000000", "قطاعي", "قطاعي", 0, 0, true),
                new(Guid.NewGuid(), "مقاول", "01100000000", "مقاول", "جملة", 10000, 2500, true)
            });
        var lookup = new Mock<ILookupService>();
        lookup.Setup(x => x.GetPriceGroupsAsync()).ReturnsAsync(new List<PriceGroup>());
        var permissions = CreatePermissionsMock();

        var vm = new CustomersViewModel(partyService.Object, lookup.Object, permissions.Object);
        await vm.LoadCommand.ExecuteAsync(null);

        vm.CustomerCount.Should().Be(2);
        vm.TotalReceivables.Should().Be(2500);
        vm.SaveCommand.Should().NotBeNull();
        vm.EditCustomerCommand.Should().NotBeNull();
        vm.ToggleActiveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SuppliersViewModel_LoadsPayablesAndExposesCommands()
    {
        var partyService = new Mock<IPartyService>();
        partyService.Setup(x => x.SearchSuppliersAsync(
                It.IsAny<PartySearchCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SupplierListItem>
            {
                new(Guid.NewGuid(), "مورد بويات", "01200000000", "القاهرة", 4500, true)
            });
        var permissions = CreatePermissionsMock();

        var vm = new SuppliersViewModel(partyService.Object, permissions.Object);
        await vm.LoadCommand.ExecuteAsync(null);

        vm.SupplierCount.Should().Be(1);
        vm.TotalPayables.Should().Be(4500);
        vm.SaveCommand.Should().NotBeNull();
        vm.EditSupplierCommand.Should().NotBeNull();
        vm.ToggleActiveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task DashboardViewModel_UsesRealSummaryServiceValues()
    {
        var dashboardService = new Mock<IDashboardService>();
        dashboardService.Setup(x => x.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSummary
            {
                TodaySales = 1200,
                ActiveProductCount = 25,
                LowStockCount = 2
            });
        var vm = new HardwarePaintShop.Desktop.ViewModels.Dashboard.DashboardViewModel(
            dashboardService.Object);

        await vm.RefreshCommand.ExecuteAsync(null);

        vm.TodaySales.Should().Be(1200);
        vm.ActiveProductCount.Should().Be(25);
        vm.LowStockCount.Should().Be(2);
    }

    private static Mock<IPermissionService> CreatePermissionsMock()
    {
        var permissions = new Mock<IPermissionService>();
        permissions.Setup(x => x.Can(It.IsAny<string>())).Returns(true);
        return permissions;
    }
}
