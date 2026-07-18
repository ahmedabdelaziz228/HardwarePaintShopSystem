namespace HardwarePaintShop.Application.Models;

public sealed class BusinessReportData
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public decimal Sales { get; init; }
    public decimal SalesReturns { get; init; }
    public decimal NetSales => Sales - SalesReturns;
    public decimal Purchases { get; init; }
    public decimal PurchaseReturns { get; init; }
    public decimal Expenses { get; init; }
    public decimal CollectedCash { get; init; }
    public decimal PaidCash { get; init; }
    public decimal SalesReturnCost { get; init; }
    public decimal EstimatedCostOfSales { get; init; }
    public decimal EstimatedGrossProfit => NetSales - EstimatedCostOfSales;
    public decimal EstimatedNetProfit => EstimatedGrossProfit - Expenses;
    public decimal CustomerDebt { get; init; }
    public decimal SupplierDebt { get; init; }
    public decimal StockValue { get; init; }
    public List<DailySalesReportItem> DailySales { get; init; } = new();
    public List<TopProductReportItem> TopProducts { get; init; } = new();
    public List<PartyBalanceReportItem> CustomerBalances { get; init; } = new();
    public List<PartyBalanceReportItem> SupplierBalances { get; init; } = new();
}

public sealed record DailySalesReportItem(DateTime Date, int InvoiceCount, decimal Sales, decimal Paid, decimal Remaining);
public sealed record TopProductReportItem(
    Guid ProductId,
    string ProductName,
    decimal QuantityBase,
    decimal Revenue,
    decimal Cost,
    decimal GrossProfit);
public sealed record PartyBalanceReportItem(Guid PartyId, string PartyName, string? Phone, decimal Balance, decimal Limit);
