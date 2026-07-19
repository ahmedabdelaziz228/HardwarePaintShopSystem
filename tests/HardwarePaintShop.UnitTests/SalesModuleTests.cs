using FluentAssertions;
using HardwarePaintShop.Application.Helpers;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Application.Security;
using HardwarePaintShop.Desktop.ViewModels.Sales;
using Xunit;

namespace HardwarePaintShop.UnitTests;

public class SalesModuleTests
{
    [Fact]
    public void SalesLine_CalculatesBaseQuantityAndTotal()
    {
        var option = new SalesProductOption(
            Guid.NewGuid(), Guid.NewGuid(), "P-1", "بويات", "جردل",
            4, 40, 250, 230, false, "622001");
        var line = new SalesLineEditor(option, 3, 250, string.Empty);

        line.QuantityBaseUnit.Should().Be(12);
        line.LineTotal.Should().Be(750);
    }

    [Fact]
    public void SalesLine_ParsesSerialNumbers()
    {
        var option = new SalesProductOption(
            Guid.NewGuid(), Guid.NewGuid(), "D-1", "شنيور", "قطعة",
            1, 3, 1800, 1750, true, null);
        var line = new SalesLineEditor(option, 2, 1800, "DR-1\nDR-2");

        line.ParseSerialNumbers().Should().BeEquivalentTo("DR-1", "DR-2");
    }

    [Fact]
    public void CharcoalBags_ArePurchasedAsBagsAndSoldAsBagsOrKilograms()
    {
        var purchasedKilograms = UnitConverter.ToBase(25, 25);
        var soldAsBags = UnitConverter.ToBase(3, 25);
        var soldAsKilograms = UnitConverter.ToBase(10, 1);

        purchasedKilograms.Should().Be(625);
        (purchasedKilograms - soldAsBags - soldAsKilograms).Should().Be(540);
    }

    [Fact]
    public void WireRoll_RemainingMetersStayExactAfterFractionalSale()
    {
        var rollMeters = UnitConverter.ToBase(1, 50);
        var remainingMeters = rollMeters - UnitConverter.ToBase(12.5m, 1);

        remainingMeters.Should().Be(37.5m);
    }

    [Fact]
    public void SalesLine_AllowsActualPriceButKeepsMinimumPriceForValidation()
    {
        var option = new SalesProductOption(
            Guid.NewGuid(), Guid.NewGuid(), "P-2", "منتج", "قطعة",
            1, 10, 50, 40, false, null);
        var line = new SalesLineEditor(option, 2, 46, string.Empty);

        line.UnitPrice.Should().Be(46);
        line.MinSalePrice.Should().Be(40);
        line.LineTotal.Should().Be(92);
    }

    [Fact]
    public void CreditPostingRequest_CarriesAutomaticCustomerData()
    {
        var request = new PostSalesRequest
        {
            InvoiceId = Guid.NewGuid(),
            PaidAmount = 60,
            CreditCustomerName = "أحمد",
            CreditCustomerPhone = "01000000000",
            CreditCustomerAddress = "القاهرة"
        };

        request.CreditCustomerName.Should().Be("أحمد");
        request.CreditCustomerPhone.Should().Be("01000000000");
    }

    [Fact]
    public void BusinessReport_CalculatesNetProfitAfterReturnsCostAndExpenses()
    {
        var report = new BusinessReportData
        {
            Sales = 1_000,
            SalesReturns = 100,
            EstimatedCostOfSales = 500,
            Expenses = 100
        };

        report.NetSales.Should().Be(900);
        report.EstimatedGrossProfit.Should().Be(400);
        report.EstimatedNetProfit.Should().Be(300);
    }

    [Fact]
    public void SalesPermissions_AreSeeded()
    {
        PermissionCodes.All.Should().Contain(new[]
        {
            "Sales.View", "Sales.Create", "Sales.Post", "Sales.VoidDraft"
        });
    }
}
