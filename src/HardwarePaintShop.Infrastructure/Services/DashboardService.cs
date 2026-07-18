using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DashboardService(IDbContextFactory<AppDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var todayLocal = DateTime.Today;
        var todayStartUtc = todayLocal.ToUniversalTime();
        var tomorrowStartUtc = todayLocal.AddDays(1).ToUniversalTime();
        var weekStartUtc = todayLocal.AddDays(-6).ToUniversalTime();

        var todaySales = await db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= todayStartUtc &&
                        i.InvoiceDate < tomorrowStartUtc &&
                        i.Status != InvoiceStatus.Voided)
            .SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0;
        var salesInvoiceCount = await db.SalesInvoices
            .AsNoTracking()
            .CountAsync(i => i.InvoiceDate >= todayStartUtc &&
                             i.InvoiceDate < tomorrowStartUtc &&
                             i.Status != InvoiceStatus.Voided,
                cancellationToken);
        var purchaseInvoiceCount = await db.PurchaseInvoices
            .AsNoTracking()
            .CountAsync(i => i.InvoiceDate >= todayStartUtc &&
                             i.InvoiceDate < tomorrowStartUtc &&
                             i.Status != InvoiceStatus.Voided,
                cancellationToken);
        var todayCollections = await db.CashMovements
            .AsNoTracking()
            .Where(m => m.CreatedAt >= todayStartUtc &&
                        m.CreatedAt < tomorrowStartUtc &&
                        m.Direction == CashDirection.In &&
                        (m.MovementType == CashMovementType.SalePayment ||
                         m.MovementType == CashMovementType.CustomerCollection))
            .SumAsync(m => (decimal?)m.Amount, cancellationToken) ?? 0;
        var todayExpenses = await db.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= todayStartUtc && e.ExpenseDate < tomorrowStartUtc)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0;
        var todayCost = await db.SalesInvoiceItems
            .AsNoTracking()
            .Where(i => i.SalesInvoice.InvoiceDate >= todayStartUtc &&
                        i.SalesInvoice.InvoiceDate < tomorrowStartUtc &&
                        i.SalesInvoice.Status != InvoiceStatus.Voided)
            .SumAsync(i => (decimal?)(i.QuantityBaseUnit *
                (i.Product.ProductCost != null ? i.Product.ProductCost.AverageCostBaseUnit : 0)),
                cancellationToken) ?? 0;
        var cashboxBalance = await db.Cashboxes
            .AsNoTracking()
            .Where(c => c.IsActive)
            .SumAsync(c => (decimal?)c.CurrentBalance, cancellationToken) ?? 0;
        var customerReceivables = await db.Customers
            .AsNoTracking()
            .Where(c => c.IsActive && c.CurrentBalance > 0)
            .SumAsync(c => (decimal?)c.CurrentBalance, cancellationToken) ?? 0;
        var supplierPayables = await db.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive && s.CurrentBalance > 0)
            .SumAsync(s => (decimal?)s.CurrentBalance, cancellationToken) ?? 0;

        var productStocks = await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Name,
                CategoryName = p.Category != null ? p.Category.Name : "بدون تصنيف",
                p.MinStockBaseQuantity,
                BaseUnitName = p.BaseUnit.Name,
                AverageCost = p.ProductCost != null ? p.ProductCost.AverageCostBaseUnit : 0,
                CurrentQuantity = p.StockMovements
                    .Select(m => (decimal?)m.QuantityBaseUnit)
                    .Sum() ?? 0
            })
            .ToListAsync(cancellationToken);

        var inventoryValue = productStocks
            .Where(p => p.CurrentQuantity > 0)
            .Sum(p => p.CurrentQuantity * p.AverageCost);
        var inventoryByCategory = BuildInventoryDistribution(productStocks
            .Where(p => p.CurrentQuantity > 0)
            .Select(p => (p.CategoryName, Value: p.CurrentQuantity * p.AverageCost)), inventoryValue);

        var weeklySalesRows = await db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= weekStartUtc &&
                        i.InvoiceDate < tomorrowStartUtc &&
                        i.Status != InvoiceStatus.Voided)
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync(cancellationToken);
        var weeklyExpenseRows = await db.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= weekStartUtc && e.ExpenseDate < tomorrowStartUtc)
            .Select(e => new { e.ExpenseDate, e.Amount })
            .ToListAsync(cancellationToken);
        var weeklyTrend = Enumerable.Range(0, 7)
            .Select(offset => todayLocal.AddDays(offset - 6))
            .Select(day => new DashboardTrendPoint(
                day,
                weeklySalesRows
                    .Where(i => i.InvoiceDate.ToLocalTime().Date == day)
                    .Sum(i => i.TotalAmount),
                weeklyExpenseRows
                    .Where(e => e.ExpenseDate.ToLocalTime().Date == day)
                    .Sum(e => e.Amount)))
            .ToList();
        var yesterdaySales = weeklyTrend[^2].Sales;
        var salesChangePercentage = yesterdaySales == 0
            ? (todaySales > 0 ? 100 : 0)
            : Math.Round((todaySales - yesterdaySales) / yesterdaySales * 100, 1);

        var recentOperations = await GetRecentOperationsAsync(db, cancellationToken);
        var lowStock = productStocks
            .Where(p => p.MinStockBaseQuantity > 0 && p.CurrentQuantity <= p.MinStockBaseQuantity)
            .OrderBy(p => p.CurrentQuantity - p.MinStockBaseQuantity)
            .ToList();

        return new DashboardSummary
        {
            TodaySales = todaySales,
            EstimatedTodayProfit = todaySales - todayCost - todayExpenses,
            InventoryValue = inventoryValue,
            SalesChangePercentage = salesChangePercentage,
            TodayCollections = todayCollections,
            TodayExpenses = todayExpenses,
            CashboxBalance = cashboxBalance,
            CustomerReceivables = customerReceivables,
            SupplierPayables = supplierPayables,
            SalesInvoiceCount = salesInvoiceCount,
            PurchaseInvoiceCount = purchaseInvoiceCount,
            ActiveProductCount = productStocks.Count,
            ActiveCustomerCount = await db.Customers.CountAsync(c => c.IsActive, cancellationToken),
            ActiveSupplierCount = await db.Suppliers.CountAsync(s => s.IsActive, cancellationToken),
            LowStockCount = lowStock.Count,
            LowStockProducts = lowStock.Take(6)
                .Select(p => new LowStockProductSummary(
                    p.Name,
                    p.CurrentQuantity,
                    p.MinStockBaseQuantity,
                    p.BaseUnitName))
                .ToList(),
            WeeklyTrend = weeklyTrend,
            InventoryByCategory = inventoryByCategory,
            RecentOperations = recentOperations
        };
    }

    private static List<InventoryCategorySummary> BuildInventoryDistribution(
        IEnumerable<(string CategoryName, decimal Value)> rows,
        decimal totalValue)
    {
        if (totalValue <= 0)
            return new List<InventoryCategorySummary>();

        var groups = rows
            .GroupBy(r => r.CategoryName)
            .Select(g => new { CategoryName = g.Key, Value = g.Sum(r => r.Value) })
            .Where(g => g.Value > 0)
            .OrderByDescending(g => g.Value)
            .ToList();

        var selected = groups.Take(3)
            .Select(g => (g.CategoryName, g.Value))
            .ToList();
        var otherValue = groups.Skip(3).Sum(g => g.Value);
        if (otherValue > 0)
            selected.Add(("أخرى", otherValue));

        return selected
            .Select(g => new InventoryCategorySummary(
                g.CategoryName,
                g.Value,
                Math.Round(g.Value / totalValue * 100, 1)))
            .ToList();
    }

    private static async Task<List<RecentOperationSummary>> GetRecentOperationsAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var sales = await db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Voided)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new
            {
                i.InvoiceNo,
                CustomerName = i.Customer != null ? i.Customer.Name : "عميل نقدي",
                i.TotalAmount,
                i.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var purchases = await db.PurchaseInvoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Voided)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .Select(i => new
            {
                i.InvoiceNo,
                SupplierName = i.Supplier.Name,
                i.TotalAmount,
                i.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new
            {
                CategoryName = e.ExpenseCategory.Name,
                e.Amount,
                e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return sales
            .Select(i => new RecentOperationSummary(
                "sale", $"فاتورة بيع #{i.InvoiceNo}", i.CustomerName,
                i.TotalAmount, i.CreatedAt.ToLocalTime()))
            .Concat(purchases.Select(i => new RecentOperationSummary(
                "purchase", $"فاتورة شراء #{i.InvoiceNo}", i.SupplierName,
                i.TotalAmount, i.CreatedAt.ToLocalTime())))
            .Concat(expenses.Select(e => new RecentOperationSummary(
                "expense", "مصروف مسجل", e.CategoryName,
                e.Amount, e.CreatedAt.ToLocalTime())))
            .OrderByDescending(i => i.OccurredAt)
            .Take(5)
            .ToList();
    }
}
