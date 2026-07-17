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
        var todayStartUtc = DateTime.Today.ToUniversalTime();
        var tomorrowStartUtc = DateTime.Today.AddDays(1).ToUniversalTime();

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
                p.MinStockBaseQuantity,
                BaseUnitName = p.BaseUnit.Name,
                CurrentQuantity = p.StockMovements
                    .Select(m => (decimal?)m.QuantityBaseUnit)
                    .Sum() ?? 0
            })
            .ToListAsync(cancellationToken);
        var lowStock = productStocks
            .Where(p => p.MinStockBaseQuantity > 0 && p.CurrentQuantity <= p.MinStockBaseQuantity)
            .OrderBy(p => p.CurrentQuantity - p.MinStockBaseQuantity)
            .ToList();

        return new DashboardSummary
        {
            TodaySales = todaySales,
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
                .ToList()
        };
    }
}
