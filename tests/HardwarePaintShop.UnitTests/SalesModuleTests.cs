using FluentAssertions;
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
    public void SalesPermissions_AreSeeded()
    {
        PermissionCodes.All.Should().Contain(new[]
        {
            "Sales.View", "Sales.Create", "Sales.Post", "Sales.VoidDraft"
        });
    }
}
