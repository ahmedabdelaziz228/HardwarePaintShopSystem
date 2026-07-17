using FluentAssertions;
using HardwarePaintShop.Application.Helpers;
using HardwarePaintShop.Application.Security;
using HardwarePaintShop.Desktop.ViewModels.Purchases;
using HardwarePaintShop.Application.Models;
using Xunit;

namespace HardwarePaintShop.UnitTests;

public class PurchasingModuleTests
{
    [Fact]
    public void CompoundStock_UsesLargestUnitFirst()
    {
        var result = StockDisplayFormatter.FormatCompound(
            29,
            new[] { ("قطعة", 1m), ("كرتونة", 12m) });

        result.Should().Be("2 كرتونة و5 قطعة");
    }

    [Fact]
    public void PurchaseLine_CalculatesBaseQuantityAndTotal()
    {
        var option = new PurchaseProductOption(
            Guid.NewGuid(), Guid.NewGuid(), "P-1", "مسمار", "علبة",
            100, 0, 25, false, null);
        var line = new PurchaseLineEditor(option, 3, 25, string.Empty);

        line.QuantityBaseUnit.Should().Be(300);
        line.LineTotal.Should().Be(75);
    }

    [Fact]
    public void PurchaseLine_ParsesSerialsFromLinesAndCommas()
    {
        var option = new PurchaseProductOption(
            Guid.NewGuid(), Guid.NewGuid(), "D-1", "شنيور", "قطعة",
            1, 0, 0, true, null);
        var line = new PurchaseLineEditor(option, 3, 500, "SN-1\nSN-2, SN-3");

        line.ParseSerialNumbers().Should().BeEquivalentTo("SN-1", "SN-2", "SN-3");
    }

    [Fact]
    public void PurchasingPermissions_AreSeeded()
    {
        PermissionCodes.All.Should().Contain(new[]
        {
            "Purchase.View", "Purchase.Create", "Purchase.Post", "Purchase.Void", "Cashbox.View"
        });
    }
}
