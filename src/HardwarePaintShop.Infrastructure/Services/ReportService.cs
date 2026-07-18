using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public ReportService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<BusinessReportData> GetBusinessReportAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (to.Date < from.Date) throw new InvalidOperationException("تاريخ النهاية يسبق تاريخ البداية.");
        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var sales = db.SalesInvoices.AsNoTracking().Where(i => i.InvoiceDate >= start && i.InvoiceDate < end &&
            i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Voided);
        var purchases = db.PurchaseInvoices.AsNoTracking().Where(i => i.InvoiceDate >= start && i.InvoiceDate < end &&
            i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Voided);
        var periodReturns = db.Returns.AsNoTracking().Where(r => r.CreatedAt >= start && r.CreatedAt < end && r.Status == "active");

        // A DbContext supports one active operation at a time; keep these aggregates sequential.
        var salesTotal = await sales.SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0;
        var purchaseTotal = await purchases.SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0;
        var salesReturn = await periodReturns.Where(r => r.ReturnType == ReturnType.SalesReturn)
            .SumAsync(r => (decimal?)r.TotalAmount, cancellationToken) ?? 0;
        var purchaseReturn = await periodReturns.Where(r => r.ReturnType == ReturnType.PurchaseReturn)
            .SumAsync(r => (decimal?)r.TotalAmount, cancellationToken) ?? 0;
        var expense = await db.Expenses.Where(e => e.ExpenseDate >= start && e.ExpenseDate < end)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;
        var cashIn = await db.CashMovements.Where(m => m.CreatedAt >= start && m.CreatedAt < end && m.Direction == CashDirection.In)
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0;
        var cashOut = await db.CashMovements.Where(m => m.CreatedAt >= start && m.CreatedAt < end && m.Direction == CashDirection.Out)
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0;

        var capturedCost = await db.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice.InvoiceDate >= start && i.SalesInvoice.InvoiceDate < end &&
                i.SalesInvoice.Status != InvoiceStatus.Draft && i.SalesInvoice.Status != InvoiceStatus.Voided)
            .SumAsync(i => (decimal?)i.CostTotal, cancellationToken) ?? 0;

        var salesReturnLines = await db.ReturnItems.AsNoTracking()
            .Where(i => i.Return.CreatedAt >= start && i.Return.CreatedAt < end &&
                i.Return.Status == "active" && i.Return.ReturnType == ReturnType.SalesReturn &&
                i.Return.OriginalSalesInvoiceId.HasValue)
            .Select(i => new
            {
                InvoiceId = i.Return.OriginalSalesInvoiceId!.Value,
                i.ProductId,
                i.ProductUnitId,
                i.UnitPrice,
                i.QuantityBaseUnit,
                i.Total
            })
            .ToListAsync(cancellationToken);
        var returnInvoiceIds = salesReturnLines.Select(i => i.InvoiceId).Distinct().ToList();
        var originalCostLines = returnInvoiceIds.Count == 0
            ? new List<ReturnCostSource>()
            : await db.SalesInvoiceItems.AsNoTracking()
                .Where(i => returnInvoiceIds.Contains(i.SalesInvoiceId))
                .Select(i => new ReturnCostSource(
                    i.SalesInvoiceId,
                    i.ProductId,
                    i.ProductUnitId,
                    i.UnitPrice,
                    i.QuantityBaseUnit,
                    i.CostTotal))
                .ToListAsync(cancellationToken);
        var costPerBase = originalCostLines
            .GroupBy(i => (i.InvoiceId, i.ProductId, i.ProductUnitId, i.UnitPrice))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.QuantityBaseUnit) == 0
                    ? 0
                    : g.Sum(i => i.CostTotal) / g.Sum(i => i.QuantityBaseUnit));
        var returnedCost = salesReturnLines.Sum(i =>
            i.QuantityBaseUnit * costPerBase.GetValueOrDefault(
                (i.InvoiceId, i.ProductId, i.ProductUnitId, i.UnitPrice)));
        var daily = await sales.GroupBy(i => i.InvoiceDate.Date).OrderBy(g => g.Key)
            .Select(g => new DailySalesReportItem(g.Key, g.Count(), g.Sum(i => i.TotalAmount),
                g.Sum(i => i.PaidAmount), g.Sum(i => i.RemainingAmount))).ToListAsync(cancellationToken);
        var topSales = await db.SalesInvoiceItems.AsNoTracking()
            .Where(i => i.SalesInvoice.InvoiceDate >= start && i.SalesInvoice.InvoiceDate < end &&
                i.SalesInvoice.Status != InvoiceStatus.Draft && i.SalesInvoice.Status != InvoiceStatus.Voided)
            .GroupBy(i => new { i.ProductId, i.Product.Name }).Select(g => new TopProductReportItem(
                g.Key.ProductId, g.Key.Name, g.Sum(i => i.QuantityBaseUnit),
                g.Sum(i => i.LineTotal - i.DiscountAmount), g.Sum(i => i.CostTotal), g.Sum(i => i.GrossProfit)))
            .OrderByDescending(i => i.Revenue).Take(50).ToListAsync(cancellationToken);
        var returnedByProduct = salesReturnLines
            .GroupBy(i => i.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Quantity = g.Sum(i => i.QuantityBaseUnit),
                    Revenue = g.Sum(i => i.Total),
                    Cost = g.Sum(i => i.QuantityBaseUnit * costPerBase.GetValueOrDefault(
                        (i.InvoiceId, i.ProductId, i.ProductUnitId, i.UnitPrice)))
                });
        var top = topSales.Select(item =>
        {
            var returned = returnedByProduct.GetValueOrDefault(item.ProductId);
            var quantity = item.QuantityBase - (returned?.Quantity ?? 0);
            var revenue = item.Revenue - (returned?.Revenue ?? 0);
            var cost = item.Cost - (returned?.Cost ?? 0);
            return item with
            {
                QuantityBase = quantity,
                Revenue = revenue,
                Cost = cost,
                GrossProfit = revenue - cost
            };
        }).OrderByDescending(i => i.Revenue).ToList();
        var customers = await db.Customers.AsNoTracking().Where(c => c.CurrentBalance != 0)
            .OrderByDescending(c => c.CurrentBalance).Take(300)
            .Select(c => new PartyBalanceReportItem(c.Id, c.Name, c.Phone, c.CurrentBalance, c.CreditLimit))
            .ToListAsync(cancellationToken);
        var suppliers = await db.Suppliers.AsNoTracking().Where(s => s.CurrentBalance != 0)
            .OrderByDescending(s => s.CurrentBalance).Take(300)
            .Select(s => new PartyBalanceReportItem(s.Id, s.Name, s.Phone, s.CurrentBalance, 0))
            .ToListAsync(cancellationToken);
        var stockValue = await db.Products.AsNoTracking().Where(p => p.IsActive)
            .SumAsync(p => (decimal?)((p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0) *
                (p.ProductCost != null ? p.ProductCost.AverageCostBaseUnit : 0)), cancellationToken) ?? 0;

        return new BusinessReportData
        {
            From = from.Date, To = to.Date, Sales = salesTotal,
            Purchases = purchaseTotal, SalesReturns = salesReturn,
            PurchaseReturns = purchaseReturn, Expenses = expense,
            CollectedCash = cashIn, PaidCash = cashOut,
            SalesReturnCost = returnedCost,
            EstimatedCostOfSales = capturedCost - returnedCost,
            CustomerDebt = customers.Sum(c => c.Balance),
            SupplierDebt = suppliers.Sum(s => s.Balance), StockValue = stockValue,
            DailySales = daily, TopProducts = top, CustomerBalances = customers, SupplierBalances = suppliers
        };
    }

    private sealed record ReturnCostSource(
        Guid InvoiceId,
        Guid ProductId,
        Guid ProductUnitId,
        decimal UnitPrice,
        decimal QuantityBaseUnit,
        decimal CostTotal);
}
