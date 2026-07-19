using FluentAssertions;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Desktop.ViewModels.Products;
using HardwarePaintShop.Domain.Entities;
using Moq;
using Xunit;

namespace HardwarePaintShop.UnitTests;

public class ProductModuleTests
{
    [Fact]
    public async Task ProductsViewModel_InitializesBaseUnitAndExposesCommands()
    {
        var unit = new Unit { Id = Guid.NewGuid(), Name = "قطعة" };
        var lookup = CreateLookupMock(unit);
        var products = new Mock<IProductService>();
        products.Setup(x => x.SearchAsync(
                It.IsAny<ProductSearchCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductListItem>());
        var permissions = new Mock<IPermissionService>();
        permissions.Setup(x => x.Can(It.IsAny<string>())).Returns(true);

        var parties = new Mock<IPartyService>();
        parties.Setup(x => x.SearchSuppliersAsync(It.IsAny<PartySearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SupplierListItem>());
        var vm = new ProductsViewModel(products.Object, lookup.Object, parties.Object, permissions.Object);
        await vm.InitializeCommand.ExecuteAsync(null);

        vm.FormBaseUnitId.Should().Be(unit.Id);
        vm.ProductUnits.Should().ContainSingle(x =>
            x.UnitId == unit.Id && x.ConversionFactorToBase == 1);
        vm.NewProductCommand.Should().NotBeNull();
        vm.SaveCommand.Should().NotBeNull();
        vm.AddUnitCommand.Should().NotBeNull();
        vm.AddPriceCommand.Should().NotBeNull();
        vm.AddBarcodeCommand.Should().NotBeNull();
        vm.ToggleActiveCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task PriceInquiryViewModel_SearchesByEnteredValue()
    {
        var unit = new Unit { Id = Guid.NewGuid(), Name = "قطعة" };
        var lookup = CreateLookupMock(unit);
        var products = new Mock<IProductService>();
        products.Setup(x => x.InquirePriceAsync(
                "ABC-1",
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceInquiryResult>
            {
                new(Guid.NewGuid(), "ABC-1", "شاكوش", "قطعة", 1,
                    "قطاعي", 150, 140, 3, "6220001", false, "كود")
            });

        var permissions = new Mock<IPermissionService>();
        permissions.Setup(x => x.Can(It.IsAny<string>())).Returns(true);
        var vm = new PriceInquiryViewModel(products.Object, lookup.Object, permissions.Object)
        {
            SearchText = "ABC-1"
        };
        await vm.SearchCommand.ExecuteAsync(null);

        vm.Results.Should().ContainSingle();
        vm.Results[0].SalePrice.Should().Be(150);
        vm.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ProductSaveRequest_CanCarryUnitsPricesAndBarcodesTogether()
    {
        var unitId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var request = new ProductSaveRequest
        {
            Name = "دهان بلاستيك",
            BaseUnitId = unitId,
            Units = new[] { new ProductUnitInput(null, unitId, 1, true, true) },
            Prices = new[] { new ProductPriceInput(null, unitId, groupId, 250, 240) },
            Barcodes = new[] { new ProductBarcodeInput(null, unitId, "6221234567890") }
        };

        request.Units.Should().ContainSingle();
        request.Prices.Should().ContainSingle();
        request.Barcodes.Should().ContainSingle();
    }

    [Fact]
    public void ProductSaveRequest_CarriesSingleWarehouseOpeningBalance()
    {
        var request = new ProductSaveRequest
        {
            Name = "دهان بلاستيك",
            BaseUnitId = Guid.NewGuid(),
            OpeningQuantityBase = 24,
            OpeningCostBaseUnit = 110
        };

        request.OpeningQuantityBase.Should().Be(24);
        request.OpeningCostBaseUnit.Should().Be(110);
    }

    [Theory]
    [InlineData(20, 5, "متوفر")]
    [InlineData(3, 5, "منخفض")]
    [InlineData(0, 5, "نفد المخزون")]
    [InlineData(-1, 5, "رصيد سالب")]
    public void StockBalanceItem_ReturnsArabicInventoryStatus(
        decimal quantity,
        decimal minimum,
        string expected)
    {
        var item = new StockBalanceItem(
            Guid.NewGuid(), "P-1", "منتج", "تصنيف", "قطعة",
            quantity, minimum, 10, quantity * 10, 0);

        item.StockStatus.Should().Be(expected);
    }

    private static Mock<ILookupService> CreateLookupMock(Unit unit)
    {
        var lookup = new Mock<ILookupService>();
        lookup.Setup(x => x.GetCategoriesAsync()).ReturnsAsync(new List<Category>());
        lookup.Setup(x => x.GetUnitsAsync()).ReturnsAsync(new List<Unit> { unit });
        lookup.Setup(x => x.GetPriceGroupsAsync()).ReturnsAsync(new List<PriceGroup>());
        return lookup;
    }
}
