using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

/// <summary>
/// Owns the complete purchase workflow. Posting is deliberately performed with one
/// DbContext and one database transaction so stock, supplier and cash never diverge.
/// </summary>
public sealed class PurchaseService : IPurchaseService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;

    public PurchaseService(IDbContextFactory<AppDbContext> dbFactory, IAuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public async Task<List<PurchaseInvoiceListItem>> SearchAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var invoices = db.PurchaseInvoices.AsNoTracking().AsQueryable();
        var term = query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            invoices = invoices.Where(i =>
                EF.Functions.ILike(i.InvoiceNo, pattern) ||
                (i.SupplierInvoiceNo != null && EF.Functions.ILike(i.SupplierInvoiceNo, pattern)) ||
                EF.Functions.ILike(i.Supplier.Name, pattern));
        }

        return await invoices
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAt)
            .Take(300)
            .Select(i => new PurchaseInvoiceListItem(
                i.Id, i.InvoiceNo, i.SupplierInvoiceNo, i.Supplier.Name,
                i.InvoiceDate, i.DueDate, i.TotalAmount, i.PaidAmount,
                i.RemainingAmount, i.PaymentStatus, i.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<PurchaseInvoiceDetails> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(i => i.Product)
            .Include(i => i.Items).ThenInclude(i => i.ProductUnit).ThenInclude(u => u.Unit)
            .SingleOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("فاتورة الشراء المطلوبة غير موجودة.");

        var serials = await db.ProductSerials.AsNoTracking()
            .Where(s => s.PurchaseInvoiceId == invoiceId)
            .OrderBy(s => s.SerialNumber)
            .Select(s => new { s.ProductId, s.SerialNumber })
            .ToListAsync(cancellationToken);

        return new PurchaseInvoiceDetails
        {
            Id = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            SupplierInvoiceNo = invoice.SupplierInvoiceNo,
            SupplierId = invoice.SupplierId,
            SupplierName = invoice.Supplier.Name,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            ExtraCosts = invoice.ExtraCosts,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            RemainingAmount = invoice.RemainingAmount,
            PaymentStatus = invoice.PaymentStatus,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Items = invoice.Items.OrderBy(x => x.CreatedAt).Select(item => new PurchaseInvoiceItemData
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductCode = item.Product.ProductCode,
                ProductName = item.Product.Name,
                ProductUnitId = item.ProductUnitId,
                UnitName = item.ProductUnit.Unit.Name,
                ConversionFactorToBase = item.ProductUnit.ConversionFactorToBase,
                Quantity = item.Quantity,
                QuantityBaseUnit = item.QuantityBaseUnit,
                UnitPurchasePrice = item.UnitPurchasePrice,
                LineTotal = item.LineTotal,
                IsSerialTracked = item.Product.IsSerialTracked,
                SerialNumbers = serials.Where(s => s.ProductId == item.ProductId)
                    .Select(s => s.SerialNumber).ToList()
            }).ToList()
        };
    }

    public async Task<List<PurchaseProductOption>> SearchProductsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var term = query.Trim();
        var products = db.ProductUnits.AsNoTracking()
            .Where(u => u.IsActive && u.Product.IsActive);

        if (term.Length > 0)
        {
            var pattern = $"%{term}%";
            products = products.Where(u =>
                EF.Functions.ILike(u.Product.Name, pattern) ||
                (u.Product.ProductCode != null && EF.Functions.ILike(u.Product.ProductCode, pattern)) ||
                u.Product.ProductBarcodes.Any(b => EF.Functions.ILike(b.Barcode, pattern)) ||
                u.Product.ProductSerials.Any(s => EF.Functions.ILike(s.SerialNumber, pattern)));
        }

        return await products
            .OrderByDescending(u => u.IsDefaultPurchase)
            .ThenBy(u => u.Product.Name)
            .ThenBy(u => u.Unit.Name)
            .Take(60)
            .Select(u => new PurchaseProductOption(
                u.ProductId,
                u.Id,
                u.Product.ProductCode,
                u.Product.Name,
                u.Unit.Name,
                u.ConversionFactorToBase,
                u.Product.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0,
                u.Product.ProductCost == null
                    ? null
                    : u.Product.ProductCost.LastPurchasePriceBaseUnit * u.ConversionFactorToBase,
                u.Product.IsSerialTracked,
                u.Product.ProductBarcodes
                    .Where(b => b.ProductUnitId == u.Id || b.ProductUnitId == null)
                    .OrderByDescending(b => b.ProductUnitId == u.Id)
                    .Select(b => b.Barcode)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> SaveDraftAsync(
        PurchaseDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateDraft(request);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var supplier = await db.Suppliers.SingleOrDefaultAsync(
            s => s.Id == request.SupplierId && s.IsActive,
            cancellationToken) ?? throw new InvalidOperationException("المورد المختار غير موجود أو غير نشط.");

        var supplierInvoiceNo = Normalize(request.SupplierInvoiceNo);
        if (supplierInvoiceNo is not null && await db.PurchaseInvoices.AnyAsync(i =>
                i.SupplierId == request.SupplierId &&
                i.SupplierInvoiceNo == supplierInvoiceNo &&
                (!request.Id.HasValue || i.Id != request.Id.Value),
                cancellationToken))
        {
            throw new InvalidOperationException("رقم فاتورة المورد مسجل من قبل لنفس المورد.");
        }

        PurchaseInvoice invoice;
        if (request.Id.HasValue)
        {
            invoice = await db.PurchaseInvoices
                .Include(i => i.Items)
                .SingleOrDefaultAsync(i => i.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("مسودة فاتورة الشراء غير موجودة.");
            if (invoice.Status != InvoiceStatus.Draft)
                throw new InvalidOperationException("لا يمكن تعديل فاتورة تم ترحيلها أو إلغاؤها.");

            var oldSerials = await db.ProductSerials
                .Where(s => s.PurchaseInvoiceId == invoice.Id && s.Status == SerialStatus.Reserved)
                .ToListAsync(cancellationToken);
            db.ProductSerials.RemoveRange(oldSerials);
            db.PurchaseInvoiceItems.RemoveRange(invoice.Items);
            invoice.Items.Clear();
        }
        else
        {
            invoice = new PurchaseInvoice
            {
                Id = Guid.NewGuid(),
                InvoiceNo = await GenerateInvoiceNoAsync(db, cancellationToken),
                Status = InvoiceStatus.Draft,
                UserId = _authService.CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };
            await db.PurchaseInvoices.AddAsync(invoice, cancellationToken);
        }

        var prepared = await PrepareLinesAsync(db, request.Items, invoice.Id, cancellationToken);
        var subtotal = prepared.Sum(x => x.LineTotal);

        invoice.SupplierId = supplier.Id;
        invoice.SupplierInvoiceNo = supplierInvoiceNo;
        invoice.InvoiceDate = AsUtcDate(request.InvoiceDate);
        invoice.DueDate = request.DueDate.HasValue ? AsUtcDate(request.DueDate.Value) : null;
        invoice.Subtotal = RoundMoney(subtotal);
        invoice.ExtraCosts = RoundMoney(request.ExtraCosts);
        invoice.TotalAmount = RoundMoney(subtotal + request.ExtraCosts);
        invoice.PaidAmount = 0;
        invoice.RemainingAmount = invoice.TotalAmount;
        invoice.PaymentStatus = PaymentStatus.Unpaid;
        invoice.Notes = Normalize(request.Notes);
        invoice.UpdatedAt = DateTime.UtcNow;

        foreach (var line in prepared)
        {
            invoice.Items.Add(new PurchaseInvoiceItem
            {
                Id = Guid.NewGuid(),
                PurchaseInvoiceId = invoice.Id,
                ProductId = line.ProductId,
                ProductUnitId = line.ProductUnitId,
                Quantity = line.Quantity,
                QuantityBaseUnit = line.QuantityBaseUnit,
                UnitPurchasePrice = line.UnitPurchasePrice,
                LineTotal = line.LineTotal,
                CreatedAt = DateTime.UtcNow
            });

            foreach (var serialNumber in line.SerialNumbers)
            {
                await db.ProductSerials.AddAsync(new ProductSerial
                {
                    Id = Guid.NewGuid(),
                    ProductId = line.ProductId,
                    SerialNumber = serialNumber,
                    Status = SerialStatus.Reserved,
                    PurchaseInvoiceId = invoice.Id,
                    Notes = "محجوز في مسودة شراء",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return invoice.Id;
    }

    public async Task PostAsync(
        PostPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PaidAmount < 0)
            throw new InvalidOperationException("المدفوع لا يمكن أن يكون سالبًا.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var invoice = await db.PurchaseInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("فاتورة الشراء المطلوبة غير موجودة.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("يمكن ترحيل المسودات فقط.");
        if (invoice.Items.Count == 0)
            throw new InvalidOperationException("لا يمكن ترحيل فاتورة بلا أصناف.");
        if (request.PaidAmount > invoice.TotalAmount)
            throw new InvalidOperationException("المدفوع أكبر من إجمالي الفاتورة.");

        Cashbox? cashbox = null;
        if (request.PaidAmount > 0)
        {
            if (!request.CashboxId.HasValue)
                throw new InvalidOperationException("اختر الخزينة التي سيخرج منها المبلغ.");
            cashbox = await db.Cashboxes.SingleOrDefaultAsync(
                c => c.Id == request.CashboxId.Value && c.IsActive,
                cancellationToken) ?? throw new InvalidOperationException("الخزينة المختارة غير موجودة أو غير نشطة.");
            if (cashbox.CurrentBalance < request.PaidAmount)
                throw new InvalidOperationException("رصيد الخزينة غير كافٍ لسداد المبلغ.");
        }

        var productIds = invoice.Items.Select(i => i.ProductId).Distinct().ToList();
        var currentStocks = await db.StockMovements
            .Where(m => productIds.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(m => m.QuantityBaseUnit) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);
        var costs = await db.ProductCosts.Where(c => productIds.Contains(c.ProductId))
            .ToDictionaryAsync(c => c.ProductId, cancellationToken);

        var now = DateTime.UtcNow;
        var totalLandedByProduct = invoice.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.LineTotal + AllocateExtraCost(invoice, i.LineTotal)));
        var incomingByProduct = invoice.Items.GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantityBaseUnit));

        foreach (var item in invoice.Items)
        {
            await db.StockMovements.AddAsync(new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                ProductUnitId = item.ProductUnitId,
                QuantityBaseUnit = item.QuantityBaseUnit,
                MovementType = StockMovementType.Purchase,
                ReferenceType = "PurchaseInvoice",
                ReferenceId = invoice.Id,
                UserId = _authService.CurrentUserId,
                Notes = $"ترحيل فاتورة شراء {invoice.InvoiceNo}",
                CreatedAt = now
            }, cancellationToken);
        }

        foreach (var productId in productIds)
        {
            var incomingQuantity = incomingByProduct[productId];
            var landedTotal = totalLandedByProduct[productId];
            var landedPerBase = incomingQuantity == 0 ? 0 : landedTotal / incomingQuantity;
            var currentStock = currentStocks.GetValueOrDefault(productId);
            var oldCost = costs.GetValueOrDefault(productId);
            var oldAverage = oldCost?.AverageCostBaseUnit ?? 0;
            var usableCurrentStock = Math.Max(0, currentStock);
            var average = (usableCurrentStock + incomingQuantity) == 0
                ? landedPerBase
                : ((usableCurrentStock * oldAverage) + landedTotal) / (usableCurrentStock + incomingQuantity);

            if (oldCost is null)
            {
                oldCost = new ProductCost { Id = Guid.NewGuid(), ProductId = productId };
                await db.ProductCosts.AddAsync(oldCost, cancellationToken);
            }
            oldCost.LastPurchasePriceBaseUnit = RoundMoney(landedPerBase);
            oldCost.AverageCostBaseUnit = RoundMoney(average);
            oldCost.UpdatedAt = now;
        }

        await db.SupplierTransactions.AddAsync(new SupplierTransaction
        {
            Id = Guid.NewGuid(), SupplierId = invoice.SupplierId,
            TransactionType = "PurchaseInvoice", Direction = "credit",
            Amount = invoice.TotalAmount, ReferenceType = "PurchaseInvoice", ReferenceId = invoice.Id,
            UserId = _authService.CurrentUserId, Notes = $"فاتورة شراء {invoice.InvoiceNo}", CreatedAt = now
        }, cancellationToken);

        if (request.PaidAmount > 0 && cashbox is not null)
        {
            await db.SupplierTransactions.AddAsync(new SupplierTransaction
            {
                Id = Guid.NewGuid(), SupplierId = invoice.SupplierId,
                TransactionType = "PurchasePayment", Direction = "debit",
                Amount = request.PaidAmount, ReferenceType = "PurchaseInvoice", ReferenceId = invoice.Id,
                UserId = _authService.CurrentUserId, Notes = $"دفعة فاتورة {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
            await db.InvoicePayments.AddAsync(new InvoicePayment
            {
                Id = Guid.NewGuid(), PaymentNo = await GeneratePaymentNoAsync(db, cancellationToken),
                PaymentType = "purchase", PaymentMethod = request.PaymentMethod,
                PurchaseInvoiceId = invoice.Id, SupplierId = invoice.SupplierId,
                CashboxId = cashbox.Id, Amount = request.PaidAmount, PaymentDate = now,
                UserId = _authService.CurrentUserId, Notes = $"دفعة عند ترحيل {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
            await db.CashMovements.AddAsync(new CashMovement
            {
                Id = Guid.NewGuid(), CashboxId = cashbox.Id,
                MovementType = CashMovementType.SupplierPayment, Direction = CashDirection.Out,
                Amount = request.PaidAmount, ReferenceType = "PurchaseInvoice", ReferenceId = invoice.Id,
                UserId = _authService.CurrentUserId, Notes = $"سداد فاتورة شراء {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
            cashbox.CurrentBalance -= request.PaidAmount;
            cashbox.UpdatedAt = now;
        }

        invoice.Supplier.CurrentBalance += invoice.TotalAmount - request.PaidAmount;
        invoice.Supplier.UpdatedAt = now;
        invoice.PaidAmount = RoundMoney(request.PaidAmount);
        invoice.RemainingAmount = RoundMoney(invoice.TotalAmount - request.PaidAmount);
        invoice.PaymentStatus = invoice.RemainingAmount == 0
            ? PaymentStatus.Paid
            : invoice.PaidAmount > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid;
        invoice.Status = InvoiceStatus.Active;
        invoice.UpdatedAt = now;

        var serials = await db.ProductSerials
            .Where(s => s.PurchaseInvoiceId == invoice.Id && s.Status == SerialStatus.Reserved)
            .ToListAsync(cancellationToken);
        foreach (var serial in serials)
        {
            serial.Status = SerialStatus.Available;
            serial.Notes = $"وارد من فاتورة شراء {invoice.InvoiceNo}";
        }

        await db.AuditLogs.AddAsync(NewAudit(invoice.Id, AuditAction.Update, "Post"), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task VoidAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var invoice = await db.PurchaseInvoices
            .Include(i => i.Supplier)
            .Include(i => i.Items)
            .SingleOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("فاتورة الشراء المطلوبة غير موجودة.");

        if (invoice.Status == InvoiceStatus.Voided)
            throw new InvalidOperationException("الفاتورة ملغاة بالفعل.");

        var serials = await db.ProductSerials.Where(s => s.PurchaseInvoiceId == invoice.Id)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        if (invoice.Status == InvoiceStatus.Draft)
        {
            foreach (var serial in serials)
                serial.Status = SerialStatus.Cancelled;
            invoice.Status = InvoiceStatus.Voided;
            invoice.UpdatedAt = now;
            await db.AuditLogs.AddAsync(NewAudit(invoice.Id, AuditAction.Void, "VoidDraft"), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (invoice.Status != InvoiceStatus.Active)
            throw new InvalidOperationException("حالة الفاتورة لا تسمح بالإلغاء.");
        if (invoice.PaidAmount != 0)
            throw new InvalidOperationException("لا يمكن إلغاء فاتورة عليها دفعة. استخدم مرتجع مشتريات للحفاظ على أثر السداد.");
        if (serials.Any(s => s.Status != SerialStatus.Available))
            throw new InvalidOperationException("لا يمكن الإلغاء لأن أحد سيريالات الفاتورة تم بيعه أو تغيير حالته.");

        var required = invoice.Items.GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.QuantityBaseUnit));
        var productIds = required.Keys.ToList();
        var stocks = await db.StockMovements.Where(m => productIds.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(m => m.QuantityBaseUnit) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);
        if (required.Any(x => stocks.GetValueOrDefault(x.Key) < x.Value))
            throw new InvalidOperationException("لا يمكن الإلغاء لأن جزءًا من الكمية المشتراة خرج من المخزون.");

        var costs = await db.ProductCosts.Where(c => productIds.Contains(c.ProductId))
            .ToDictionaryAsync(c => c.ProductId, cancellationToken);
        foreach (var productId in productIds)
        {
            if (!costs.TryGetValue(productId, out var cost))
                continue;
            var currentStock = stocks.GetValueOrDefault(productId);
            var remainingStock = currentStock - required[productId];
            var removedLandedValue = invoice.Items.Where(i => i.ProductId == productId)
                .Sum(i => i.LineTotal + AllocateExtraCost(invoice, i.LineTotal));
            var previousValue = Math.Max(0m, currentStock * cost.AverageCostBaseUnit - removedLandedValue);
            cost.AverageCostBaseUnit = remainingStock > 0
                ? RoundMoney(previousValue / remainingStock)
                : 0;
            cost.LastPurchasePriceBaseUnit = cost.AverageCostBaseUnit;
            cost.UpdatedAt = now;
        }

        foreach (var item in invoice.Items)
        {
            await db.StockMovements.AddAsync(new StockMovement
            {
                Id = Guid.NewGuid(), ProductId = item.ProductId, ProductUnitId = item.ProductUnitId,
                QuantityBaseUnit = -item.QuantityBaseUnit, MovementType = StockMovementType.PurchaseReturn,
                ReferenceType = "VoidedPurchaseInvoice", ReferenceId = invoice.Id,
                UserId = _authService.CurrentUserId, Notes = $"عكس فاتورة شراء {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
        }

        await db.SupplierTransactions.AddAsync(new SupplierTransaction
        {
            Id = Guid.NewGuid(), SupplierId = invoice.SupplierId,
            TransactionType = "VoidedPurchase", Direction = "debit", Amount = invoice.RemainingAmount,
            ReferenceType = "PurchaseInvoice", ReferenceId = invoice.Id,
            UserId = _authService.CurrentUserId, Notes = $"إلغاء فاتورة {invoice.InvoiceNo}", CreatedAt = now
        }, cancellationToken);
        invoice.Supplier.CurrentBalance -= invoice.RemainingAmount;
        invoice.Supplier.UpdatedAt = now;
        foreach (var serial in serials)
        {
            serial.Status = SerialStatus.Cancelled;
            serial.Notes = $"ألغي مع فاتورة الشراء {invoice.InvoiceNo}";
        }
        invoice.Status = InvoiceStatus.Voided;
        invoice.UpdatedAt = now;
        await db.AuditLogs.AddAsync(NewAudit(invoice.Id, AuditAction.Void, "VoidPosted"), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<List<PreparedLine>> PrepareLinesAsync(
        AppDbContext db,
        IReadOnlyCollection<PurchaseLineInput> inputs,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var productIds = inputs.Select(i => i.ProductId).Distinct().ToList();
        var unitIds = inputs.Select(i => i.ProductUnitId).Distinct().ToList();
        var units = await db.ProductUnits
            .Include(u => u.Product)
            .Where(u => unitIds.Contains(u.Id) && productIds.Contains(u.ProductId) && u.IsActive && u.Product.IsActive)
            .ToDictionaryAsync(u => u.Id, cancellationToken);
        if (units.Count != unitIds.Count)
            throw new InvalidOperationException("إحدى وحدات الأصناف غير موجودة أو غير نشطة.");

        var repeatedSerialProduct = inputs.GroupBy(i => i.ProductId)
            .FirstOrDefault(g => g.Count() > 1 && g.Any(i => units[i.ProductUnitId].Product.IsSerialTracked));
        if (repeatedSerialProduct is not null)
            throw new InvalidOperationException("الصنف المتتبع بالسيريال يجب أن يظهر في سطر واحد فقط داخل الفاتورة.");

        var allSerials = inputs.SelectMany(i => i.SerialNumbers)
            .Select(Normalize).Where(s => s is not null).Cast<string>().ToList();
        if (allSerials.Count != allSerials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException("يوجد رقم سيريال مكرر داخل الفاتورة.");
        if (allSerials.Count > 0)
        {
            var lowered = allSerials.Select(s => s.ToLower()).ToList();
            if (await db.ProductSerials.AnyAsync(s =>
                    lowered.Contains(s.SerialNumber.ToLower()) && s.PurchaseInvoiceId != invoiceId,
                    cancellationToken))
                throw new InvalidOperationException("أحد أرقام السيريال مسجل من قبل.");
        }

        var lines = new List<PreparedLine>();
        foreach (var input in inputs)
        {
            if (!units.TryGetValue(input.ProductUnitId, out var unit) || unit.ProductId != input.ProductId)
                throw new InvalidOperationException("الوحدة المختارة لا تتبع الصنف المحدد.");
            var quantityBase = input.Quantity * unit.ConversionFactorToBase;
            var serialNumbers = input.SerialNumbers.Select(Normalize)
                .Where(s => s is not null).Cast<string>().ToList();
            if (unit.Product.IsSerialTracked)
            {
                if (quantityBase != decimal.Truncate(quantityBase))
                    throw new InvalidOperationException($"كمية الصنف المسلسل '{unit.Product.Name}' يجب أن تنتج عددًا صحيحًا.");
                if (serialNumbers.Count != decimal.ToInt32(quantityBase))
                    throw new InvalidOperationException($"أدخل {quantityBase:0} سيريال للصنف '{unit.Product.Name}'.");
            }
            else if (serialNumbers.Count > 0)
            {
                throw new InvalidOperationException($"الصنف '{unit.Product.Name}' غير مفعّل لتتبع السيريال.");
            }

            lines.Add(new PreparedLine(
                input.ProductId, input.ProductUnitId, input.Quantity, quantityBase,
                RoundMoney(input.UnitPurchasePrice), RoundMoney(input.Quantity * input.UnitPurchasePrice), serialNumbers));
        }
        return lines;
    }

    private static void ValidateDraft(PurchaseDraftRequest request)
    {
        if (request.SupplierId == Guid.Empty)
            throw new InvalidOperationException("اختر المورد.");
        if (request.Items.Count == 0)
            throw new InvalidOperationException("أضف صنفًا واحدًا على الأقل.");
        if (request.Items.Any(i => i.ProductId == Guid.Empty || i.ProductUnitId == Guid.Empty))
            throw new InvalidOperationException("بيانات أحد الأصناف غير مكتملة.");
        if (request.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("كل الكميات يجب أن تكون أكبر من صفر.");
        if (request.Items.Any(i => i.UnitPurchasePrice < 0) || request.ExtraCosts < 0)
            throw new InvalidOperationException("الأسعار والتكاليف الإضافية لا يمكن أن تكون سالبة.");
        if (request.DueDate.HasValue && request.DueDate.Value.Date < request.InvoiceDate.Date)
            throw new InvalidOperationException("تاريخ الاستحقاق لا يمكن أن يسبق تاريخ الفاتورة.");
    }

    private static decimal AllocateExtraCost(PurchaseInvoice invoice, decimal lineTotal)
        => invoice.Subtotal <= 0 ? 0 : invoice.ExtraCosts * lineTotal / invoice.Subtotal;

    private async Task<string> GenerateInvoiceNoAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await db.PurchaseInvoices.CountAsync(i => i.InvoiceNo.StartsWith($"PUR-{date}-"), cancellationToken);
        return $"PUR-{date}-{count + 1:0000}";
    }

    private async Task<string> GeneratePaymentNoAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await db.InvoicePayments.CountAsync(i => i.PaymentNo.StartsWith($"PAY-{date}-"), cancellationToken);
        return $"PAY-{date}-{count + 1:0000}";
    }

    private AuditLog NewAudit(Guid invoiceId, AuditAction action, string operation)
        => new()
        {
            Id = Guid.NewGuid(), TableName = "PurchaseInvoices", RecordId = invoiceId,
            Action = action, ChangedByUserId = _authService.CurrentUserId,
            NewValuesJson = $"{{\"Operation\":\"{operation}\"}}", CreatedAt = DateTime.UtcNow
        };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime AsUtcDate(DateTime date)
        => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record PreparedLine(
        Guid ProductId,
        Guid ProductUnitId,
        decimal Quantity,
        decimal QuantityBaseUnit,
        decimal UnitPurchasePrice,
        decimal LineTotal,
        List<string> SerialNumbers);
}
