using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwarePaintShop.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260718000100_AddSalesProfitAndBalanceSnapshots")]
public sealed class AddSalesProfitAndBalanceSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "CustomerBalanceBefore",
            table: "SalesInvoices",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "CustomerBalanceAfter",
            table: "SalesInvoices",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "UnitCostBaseAtSale",
            table: "SalesInvoiceItems",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "CostTotal",
            table: "SalesInvoiceItems",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "GrossProfit",
            table: "SalesInvoiceItems",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        // Historical invoices did not keep a cost snapshot. Use the best available
        // current average cost once, then all future postings capture the exact cost.
        migrationBuilder.Sql("""
            UPDATE "SalesInvoiceItems" AS item
            SET "UnitCostBaseAtSale" = COALESCE(cost."AverageCostBaseUnit", 0),
                "CostTotal" = ROUND(item."QuantityBaseUnit" * COALESCE(cost."AverageCostBaseUnit", 0), 2),
                "GrossProfit" = ROUND(
                    item."LineTotal" - item."DiscountAmount" -
                    (item."QuantityBaseUnit" * COALESCE(cost."AverageCostBaseUnit", 0)), 2)
            FROM "ProductCosts" AS cost
            WHERE cost."ProductId" = item."ProductId";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CustomerBalanceBefore", table: "SalesInvoices");
        migrationBuilder.DropColumn(name: "CustomerBalanceAfter", table: "SalesInvoices");
        migrationBuilder.DropColumn(name: "UnitCostBaseAtSale", table: "SalesInvoiceItems");
        migrationBuilder.DropColumn(name: "CostTotal", table: "SalesInvoiceItems");
        migrationBuilder.DropColumn(name: "GrossProfit", table: "SalesInvoiceItems");
    }
}
