namespace HardwarePaintShop.Application.Models;

public sealed class DashboardSummary
{
    public decimal TodaySales { get; init; }
    public decimal TodayCollections { get; init; }
    public decimal TodayExpenses { get; init; }
    public decimal CashboxBalance { get; init; }
    public decimal CustomerReceivables { get; init; }
    public decimal SupplierPayables { get; init; }
    public int SalesInvoiceCount { get; init; }
    public int PurchaseInvoiceCount { get; init; }
    public int ActiveProductCount { get; init; }
    public int ActiveCustomerCount { get; init; }
    public int ActiveSupplierCount { get; init; }
    public int LowStockCount { get; init; }
    public List<LowStockProductSummary> LowStockProducts { get; init; } = new();
}

public sealed record LowStockProductSummary(
    string ProductName,
    decimal CurrentQuantity,
    decimal MinimumQuantity,
    string BaseUnitName);
