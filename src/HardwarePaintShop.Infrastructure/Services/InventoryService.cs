using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;

    public InventoryService(IDbContextFactory<AppDbContext> dbFactory, IAuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public async Task<List<StockBalanceItem>> GetStockAsync(
        string? query = null, bool lowOnly = false, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var products = db.Products.AsNoTracking().Where(p => p.IsActive);
        var term = query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            products = products.Where(p => EF.Functions.ILike(p.Name, pattern) ||
                (p.ProductCode != null && EF.Functions.ILike(p.ProductCode, pattern)) ||
                p.ProductBarcodes.Any(b => EF.Functions.ILike(b.Barcode, pattern)));
        }
        var rows = products.Select(p => new StockBalanceItem(
            p.Id, p.ProductCode, p.Name, p.Category != null ? p.Category.Name : "بدون تصنيف",
            p.BaseUnit.Name, p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0,
            p.MinStockBaseQuantity, p.ProductCost != null ? p.ProductCost.AverageCostBaseUnit : 0,
            (p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0) *
                (p.ProductCost != null ? p.ProductCost.AverageCostBaseUnit : 0),
            p.ProductSerials.Count(s => s.Status == SerialStatus.Available),
            p.CategoryId,
            p.ProductBarcodes.OrderBy(b => b.CreatedAt).Select(b => b.Barcode).FirstOrDefault(),
            p.ImagePath,
            p.MainSupplier != null ? p.MainSupplier.Name : null,
            p.ProductPrices.OrderBy(pp => pp.PriceGroup.Name)
                .Select(pp => (decimal?)pp.SalePrice).FirstOrDefault()));
        if (lowOnly) rows = rows.Where(i => i.QuantityBase <= i.MinimumQuantity);
        return await rows.OrderBy(i => i.ProductName).Take(2000).ToListAsync(cancellationToken);
    }

    public async Task<List<StockMovementListItem>> GetMovementsAsync(
        Guid? productId = null, DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.StockMovements.AsNoTracking().AsQueryable();
        if (productId.HasValue) rows = rows.Where(m => m.ProductId == productId);
        if (from.HasValue) rows = rows.Where(m => m.CreatedAt >= UtcStart(from.Value));
        if (to.HasValue) rows = rows.Where(m => m.CreatedAt < UtcStart(to.Value).AddDays(1));
        return await rows.OrderByDescending(m => m.CreatedAt).Take(2000)
            .Select(m => new StockMovementListItem(
                m.Id, m.CreatedAt, m.Product.Name,
                m.ProductUnit != null ? m.ProductUnit.Unit.Name : m.Product.BaseUnit.Name,
                m.QuantityBaseUnit, m.MovementType, m.ReferenceType, m.Notes,
                m.User != null ? m.User.FullName : null)).ToListAsync(cancellationToken);
    }

    public async Task<List<InventoryCountListItem>> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.InventoryCounts.AsNoTracking().OrderByDescending(c => c.CreatedAt).Take(300)
            .Select(c => new InventoryCountListItem(
                c.Id, c.CountNo, c.CountScope, c.Status, c.Items.Count,
                c.Items.Sum(i => Math.Abs(i.DifferenceBase)),
                c.User != null ? c.User.FullName : null, c.CreatedAt, c.ConfirmedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> PostCountAsync(InventoryCountRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0) throw new InvalidOperationException("لا توجد أصناف في الجرد.");
        if (request.Items.Any(i => i.ActualQuantityBase < 0))
            throw new InvalidOperationException("الكمية الفعلية لا يمكن أن تكون سالبة.");
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var ids = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id) && p.IsActive).ToListAsync(cancellationToken);
        if (products.Count != ids.Count) throw new InvalidOperationException("أحد أصناف الجرد غير موجود أو غير نشط.");
        var liveBalances = await db.StockMovements.Where(m => ids.Contains(m.ProductId))
            .GroupBy(m => m.ProductId).Select(g => new { ProductId = g.Key, Balance = g.Sum(x => x.QuantityBaseUnit) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Balance, cancellationToken);

        var now = DateTime.UtcNow;
        var countId = Guid.NewGuid();
        var countNo = await GenerateCountNoAsync(db, cancellationToken);
        var count = new InventoryCount
        {
            Id = countId, CountNo = countNo, CountScope = request.CountScope,
            Status = "confirmed", UserId = _authService.CurrentUserId,
            Notes = Normalize(request.Notes), CreatedAt = now, ConfirmedAt = now
        };
        db.InventoryCounts.Add(count);
        foreach (var input in request.Items)
        {
            var live = liveBalances.GetValueOrDefault(input.ProductId);
            var difference = input.ActualQuantityBase - live;
            db.InventoryCountItems.Add(new InventoryCountItem
            {
                Id = Guid.NewGuid(), InventoryCountId = countId, ProductId = input.ProductId,
                SystemQuantityBase = live, ActualQuantityBase = input.ActualQuantityBase,
                DifferenceBase = difference, Notes = Normalize(input.Notes)
            });
            if (difference != 0)
                db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(), ProductId = input.ProductId, QuantityBaseUnit = difference,
                    MovementType = StockMovementType.InventoryCount, ReferenceType = "InventoryCount",
                    ReferenceId = countId, UserId = _authService.CurrentUserId,
                    Notes = $"تسوية الجرد {countNo}", CreatedAt = now
                });
        }
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), TableName = "InventoryCounts", RecordId = countId,
            Action = AuditAction.Insert, ChangedByUserId = _authService.CurrentUserId,
            NewValuesJson = JsonSerializer.Serialize(new { CountNo = countNo, Items = request.Items.Count }), CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return countId;
    }

    public async Task<StockAdjustmentResult> AdjustStockAsync(
        StockAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var adjustmentType = request.AdjustmentType.Trim().ToLowerInvariant();
        if (adjustmentType is not ("add" or "remove" or "set"))
            throw new InvalidOperationException("نوع حركة المخزون غير صحيح.");
        if (request.ProductId == Guid.Empty)
            throw new InvalidOperationException("اختر المنتج أولًا.");
        if (request.QuantityBase < 0 || (adjustmentType != "set" && request.QuantityBase == 0))
            throw new InvalidOperationException("أدخل كمية صحيحة أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("سبب الحركة مطلوب.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var product = await db.Products
            .Include(p => p.ProductUnits)
            .SingleOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("المنتج غير موجود أو غير نشط.");
        if (product.IsSerialTracked)
            throw new InvalidOperationException("لا يمكن تعديل رصيد منتج متتبع بالسيريال من هذه الشاشة.");

        var previousBalance = await db.StockMovements
            .Where(m => m.ProductId == request.ProductId)
            .SumAsync(m => (decimal?)m.QuantityBaseUnit, cancellationToken) ?? 0;
        var difference = adjustmentType switch
        {
            "add" => request.QuantityBase,
            "remove" => -request.QuantityBase,
            _ => request.QuantityBase - previousBalance
        };
        var newBalance = previousBalance + difference;
        if (newBalance < 0)
            throw new InvalidOperationException($"لا يمكن صرف الكمية؛ الرصيد المتاح {previousBalance:0.###} فقط.");
        if (difference == 0)
            throw new InvalidOperationException("الرصيد الجديد مطابق للرصيد الحالي، لا توجد حركة لتسجيلها.");

        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var baseProductUnitId = product.ProductUnits
            .Where(u => u.IsActive && u.UnitId == product.BaseUnitId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefault();
        var reference = Normalize(request.ReferenceNo);
        var details = Normalize(request.Notes);
        var noteParts = new[]
        {
            request.Reason.Trim(),
            reference is null ? null : $"مرجع: {reference}",
            details
        }.Where(v => !string.IsNullOrWhiteSpace(v));

        db.StockMovements.Add(new StockMovement
        {
            Id = movementId,
            ProductId = request.ProductId,
            ProductUnitId = baseProductUnitId,
            QuantityBaseUnit = difference,
            MovementType = StockMovementType.Adjustment,
            ReferenceType = "ManualStockAdjustment",
            UserId = _authService.CurrentUserId,
            Notes = string.Join(" — ", noteParts),
            CreatedAt = now
        });
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TableName = "StockMovements",
            RecordId = movementId,
            Action = AuditAction.Insert,
            ChangedByUserId = _authService.CurrentUserId,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                request.ProductId,
                AdjustmentType = adjustmentType,
                PreviousBalance = previousBalance,
                Difference = difference,
                NewBalance = newBalance,
                Reason = request.Reason,
                ReferenceNo = reference
            }),
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new StockAdjustmentResult(movementId, previousBalance, difference, newBalance);
    }

    public async Task<List<AlertListItem>> RefreshAlertsAsync(
        bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var lowProducts = await db.Products.AsNoTracking().Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id, p.Name, p.MinStockBaseQuantity,
                Balance = p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0
            }).Where(p => p.Balance <= p.MinStockBaseQuantity).Take(500).ToListAsync(cancellationToken);
        var currentLowIds = lowProducts.Select(p => p.Id).ToList();
        var resolvedLowAlerts = await db.Alerts.Where(a => !a.IsRead && a.AlertType == "low_stock" &&
            a.ReferenceId != null && !currentLowIds.Contains(a.ReferenceId.Value)).ToListAsync(cancellationToken);
        foreach (var alert in resolvedLowAlerts) { alert.IsRead = true; alert.ReadAt = DateTime.UtcNow; }
        var existingLowIds = await db.Alerts.Where(a => a.AlertType == "low_stock" && a.ReferenceId != null)
            .Select(a => a.ReferenceId!.Value).ToListAsync(cancellationToken);
        foreach (var product in lowProducts.Where(p => !existingLowIds.Contains(p.Id)))
            db.Alerts.Add(new Alert
            {
                Id = Guid.NewGuid(), AlertType = "low_stock", Title = "مخزون منخفض",
                Message = $"{product.Name}: الرصيد {product.Balance:0.###} والحد الأدنى {product.MinStockBaseQuantity:0.###}",
                Severity = product.Balance < 0 ? AlertSeverity.Critical : AlertSeverity.Warning,
                ReferenceType = "Product", ReferenceId = product.Id, CreatedAt = DateTime.UtcNow
            });

        var overdue = await db.SalesInvoices.AsNoTracking().Where(i => i.RemainingAmount > 0 &&
            i.DueDate != null && i.DueDate < DateTime.UtcNow && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Returned)
            .Select(i => new { i.Id, i.InvoiceNo, i.RemainingAmount }).Take(500).ToListAsync(cancellationToken);
        var currentOverdueIds = overdue.Select(i => i.Id).ToList();
        var resolvedOverdueAlerts = await db.Alerts.Where(a => !a.IsRead && a.AlertType == "overdue_invoice" &&
            a.ReferenceId != null && !currentOverdueIds.Contains(a.ReferenceId.Value)).ToListAsync(cancellationToken);
        foreach (var alert in resolvedOverdueAlerts) { alert.IsRead = true; alert.ReadAt = DateTime.UtcNow; }
        var existingOverdueIds = await db.Alerts.Where(a => a.AlertType == "overdue_invoice" && a.ReferenceId != null)
            .Select(a => a.ReferenceId!.Value).ToListAsync(cancellationToken);
        foreach (var invoice in overdue.Where(i => !existingOverdueIds.Contains(i.Id)))
            db.Alerts.Add(new Alert
            {
                Id = Guid.NewGuid(), AlertType = "overdue_invoice", Title = "فاتورة بيع متأخرة",
                Message = $"الفاتورة {invoice.InvoiceNo} متأخر عليها {invoice.RemainingAmount:N2} ج.م",
                Severity = AlertSeverity.Warning, ReferenceType = "SalesInvoice", ReferenceId = invoice.Id,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync(cancellationToken);

        var alerts = db.Alerts.AsNoTracking().AsQueryable();
        if (unreadOnly) alerts = alerts.Where(a => !a.IsRead);
        return await alerts.OrderBy(a => a.IsRead).ThenByDescending(a => a.CreatedAt).Take(1000)
            .Select(a => new AlertListItem(a.Id, a.AlertType, a.Title, a.Message, a.Severity,
                a.IsRead, a.ReferenceType, a.ReferenceId, a.CreatedAt, a.ReadAt)).ToListAsync(cancellationToken);
    }

    public async Task MarkAlertReadAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var alert = await db.Alerts.SingleOrDefaultAsync(a => a.Id == alertId, cancellationToken)
            ?? throw new KeyNotFoundException("التنبيه غير موجود.");
        alert.IsRead = true; alert.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAlertsReadAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var alerts = await db.Alerts.Where(a => !a.IsRead).ToListAsync(cancellationToken);
        foreach (var alert in alerts) { alert.IsRead = true; alert.ReadAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DateTime UtcStart(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static async Task<string> GenerateCountNoAsync(AppDbContext db, CancellationToken token)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await db.InventoryCounts.CountAsync(c => c.CountNo.StartsWith($"CNT-{date}-"), token);
        return $"CNT-{date}-{count + 1:0000}";
    }
}
