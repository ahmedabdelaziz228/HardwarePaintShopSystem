namespace HardwarePaintShop.Application.Models;

public sealed class DashboardSummary
{
    public decimal TodaySales { get; init; }
    public decimal EstimatedTodayProfit { get; init; }
    public decimal InventoryValue { get; init; }
    public decimal SalesChangePercentage { get; init; }
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
    public List<DashboardTrendPoint> WeeklyTrend { get; init; } = new();
    public List<InventoryCategorySummary> InventoryByCategory { get; init; } = new();
    public List<RecentOperationSummary> RecentOperations { get; init; } = new();
}

public sealed record LowStockProductSummary(
    string ProductName,
    decimal CurrentQuantity,
    decimal MinimumQuantity,
    string BaseUnitName);

public sealed record DashboardTrendPoint(
    DateTime Date,
    decimal Sales,
    decimal Expenses);

public sealed record InventoryCategorySummary(
    string CategoryName,
    decimal Value,
    decimal Percentage);

public sealed record RecentOperationSummary(
    string OperationType,
    string Title,
    string Subtitle,
    decimal Amount,
    DateTime OccurredAt);
