using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwarePaintShop.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260717000100_AddPurchasingFields")]
public sealed class AddPurchasingFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "DueDate",
            table: "SalesInvoices",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "DueDate",
            table: "PurchaseInvoices",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SupplierInvoiceNo",
            table: "PurchaseInvoices",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PaymentMethod",
            table: "InvoicePayments",
            type: "text",
            nullable: false,
            defaultValue: "Cash");

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseInvoices_SupplierId_SupplierInvoiceNo",
            table: "PurchaseInvoices",
            columns: new[] { "SupplierId", "SupplierInvoiceNo" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PurchaseInvoices_SupplierId_SupplierInvoiceNo",
            table: "PurchaseInvoices");

        migrationBuilder.DropColumn(name: "DueDate", table: "SalesInvoices");
        migrationBuilder.DropColumn(name: "DueDate", table: "PurchaseInvoices");
        migrationBuilder.DropColumn(name: "SupplierInvoiceNo", table: "PurchaseInvoices");
        migrationBuilder.DropColumn(name: "PaymentMethod", table: "InvoicePayments");
    }
}
