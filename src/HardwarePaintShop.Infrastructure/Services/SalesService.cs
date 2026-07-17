using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

/// <summary>
/// Barcode-first sales workflow. Posting stock, customer, cash, serials and audit
/// is performed in one database transaction to prevent partial invoices.
/// </summary>
public sealed class SalesService : ISalesService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public SalesService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
        _permissionService = permissionService;
    }

    public async Task<List<SalesInvoiceListItem>> SearchAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var invoices = db.SalesInvoices.AsNoTracking().AsQueryable();
        var term = query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            invoices = invoices.Where(i =>
                EF.Functions.ILike(i.InvoiceNo, pattern) ||
                (i.Customer != null && EF.Functions.ILike(i.Customer.Name, pattern)) ||
                (i.Customer != null && i.Customer.Phone != null && EF.Functions.ILike(i.Customer.Phone, pattern)));
        }

        return await invoices
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.CreatedAt)
            .Take(300)
            .Select(i => new SalesInvoiceListItem(
                i.Id, i.InvoiceNo, i.Customer != null ? i.Customer.Name : "عميل نقدي",
                i.InvoiceDate, i.TotalAmount, i.PaidAmount, i.RemainingAmount,
                i.PaymentStatus, i.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<SalesInvoiceDetails> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.SalesInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Items).ThenInclude(i => i.Product)
            .Include(i => i.Items).ThenInclude(i => i.ProductUnit).ThenInclude(u => u.Unit)
            .SingleOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("فاتورة البيع المطلوبة غير موجودة.");

        var groupedItems = invoice.Items
            .GroupBy(i => new
            {
                i.ProductId,
                i.ProductUnitId,
                i.UnitPrice,
                i.Product.ProductCode,
                ProductName = i.Product.Name,
                UnitName = i.ProductUnit.Unit.Name,
                i.ProductUnit.ConversionFactorToBase,
                i.Product.IsSerialTracked
            })
            .Select(itemGroup => new SalesInvoiceItemData
            {
                ProductId = itemGroup.Key.ProductId,
                ProductUnitId = itemGroup.Key.ProductUnitId,
                ProductCode = itemGroup.Key.ProductCode,
                ProductName = itemGroup.Key.ProductName,
                UnitName = itemGroup.Key.UnitName,
                ConversionFactorToBase = itemGroup.Key.ConversionFactorToBase,
                Quantity = itemGroup.Sum(i => i.Quantity),
                QuantityBaseUnit = itemGroup.Sum(i => i.QuantityBaseUnit),
                UnitPrice = itemGroup.Key.UnitPrice,
                LineTotal = itemGroup.Sum(i => i.LineTotal),
                IsSerialTracked = itemGroup.Key.IsSerialTracked,
                SerialNumbers = itemGroup.Where(i => i.SerialNumber != null)
                    .Select(i => i.SerialNumber!).OrderBy(x => x).ToList()
            })
            .OrderBy(i => i.ProductName)
            .ToList();

        return new SalesInvoiceDetails
        {
            Id = invoice.Id,
            InvoiceNo = invoice.InvoiceNo,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.Name ?? "عميل نقدي",
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            RemainingAmount = invoice.RemainingAmount,
            PaymentStatus = invoice.PaymentStatus,
            Status = invoice.Status,
            Notes = invoice.Notes,
            Items = groupedItems
        };
    }

    public async Task<List<SalesProductOption>> SearchProductsAsync(
        string query,
        Guid? priceGroupId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var term = query.Trim();
        var units = db.ProductUnits.AsNoTracking()
            .Where(u => u.IsActive && u.Product.IsActive);
        if (term.Length > 0)
        {
            var pattern = $"%{term}%";
            units = units.Where(u =>
                EF.Functions.ILike(u.Product.Name, pattern) ||
                (u.Product.ProductCode != null && EF.Functions.ILike(u.Product.ProductCode, pattern)) ||
                u.Product.ProductBarcodes.Any(b => EF.Functions.ILike(b.Barcode, pattern)) ||
                u.Product.ProductSerials.Any(s => EF.Functions.ILike(s.SerialNumber, pattern)));
        }

        return await units
            .OrderByDescending(u => u.IsDefaultSale)
            .ThenBy(u => u.Product.Name)
            .ThenBy(u => u.Unit.Name)
            .Take(60)
            .Select(u => new SalesProductOption(
                u.ProductId,
                u.Id,
                u.Product.ProductCode,
                u.Product.Name,
                u.Unit.Name,
                u.ConversionFactorToBase,
                u.Product.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0,
                u.Product.ProductPrices
                    .Where(p => p.ProductUnitId == u.Id &&
                        (!priceGroupId.HasValue || p.PriceGroupId == priceGroupId.Value))
                    .OrderByDescending(p => priceGroupId.HasValue && p.PriceGroupId == priceGroupId.Value)
                    .ThenByDescending(p => p.PriceGroup.IsDefault)
                    .Select(p => (decimal?)p.SalePrice).FirstOrDefault() ?? 0,
                u.Product.ProductPrices
                    .Where(p => p.ProductUnitId == u.Id &&
                        (!priceGroupId.HasValue || p.PriceGroupId == priceGroupId.Value))
                    .OrderByDescending(p => priceGroupId.HasValue && p.PriceGroupId == priceGroupId.Value)
                    .ThenByDescending(p => p.PriceGroup.IsDefault)
                    .Select(p => (decimal?)p.MinSalePrice).FirstOrDefault() ?? 0,
                u.Product.IsSerialTracked,
                u.Product.ProductBarcodes
                    .Where(b => b.ProductUnitId == u.Id || b.ProductUnitId == null)
                    .OrderByDescending(b => b.ProductUnitId == u.Id)
                    .Select(b => b.Barcode).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> SaveDraftAsync(
        SalesDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateDraft(request);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await db.Customers.SingleOrDefaultAsync(
                c => c.Id == request.CustomerId.Value && c.IsActive,
                cancellationToken) ?? throw new InvalidOperationException("العميل المختار غير موجود أو غير نشط.");
        }

        SalesInvoice invoice;
        if (request.Id.HasValue)
        {
            invoice = await db.SalesInvoices.Include(i => i.Items)
                .SingleOrDefaultAsync(i => i.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("مسودة فاتورة البيع غير موجودة.");
            if (invoice.Status != InvoiceStatus.Draft)
                throw new InvalidOperationException("لا يمكن تعديل فاتورة تم ترحيلها أو إلغاؤها.");

            var oldReservedSerials = await db.ProductSerials.Where(s =>
                s.SalesInvoiceId == invoice.Id && s.Status == SerialStatus.Reserved)
                .ToListAsync(cancellationToken);
            foreach (var serial in oldReservedSerials)
            {
                serial.Status = SerialStatus.Available;
                serial.SalesInvoiceId = null;
                serial.Notes = "متاح للبيع";
            }
            db.SalesInvoiceItems.RemoveRange(invoice.Items);
            invoice.Items.Clear();
        }
        else
        {
            invoice = new SalesInvoice
            {
                Id = Guid.NewGuid(),
                InvoiceNo = await GenerateInvoiceNoAsync(db, cancellationToken),
                Status = InvoiceStatus.Draft,
                UserId = _authService.CurrentUserId,
                CreatedAt = DateTime.UtcNow
            };
            await db.SalesInvoices.AddAsync(invoice, cancellationToken);
        }

        var prepared = await PrepareLinesAsync(db, request, invoice.Id, cancellationToken);
        var subtotal = prepared.Sum(i => i.Quantity * i.UnitPrice);
        if (request.DiscountAmount > subtotal)
            throw new InvalidOperationException("خصم الفاتورة لا يمكن أن يتجاوز الإجمالي.");

        invoice.CustomerId = customer?.Id;
        invoice.InvoiceDate = AsUtcDate(request.InvoiceDate);
        invoice.DueDate = request.DueDate.HasValue ? AsUtcDate(request.DueDate.Value) : null;
        invoice.Subtotal = RoundMoney(subtotal);
        invoice.DiscountAmount = RoundMoney(request.DiscountAmount);
        invoice.TotalAmount = RoundMoney(subtotal - request.DiscountAmount);
        invoice.PaidAmount = 0;
        invoice.RemainingAmount = invoice.TotalAmount;
        invoice.PaymentStatus = PaymentStatus.Unpaid;
        invoice.Notes = Normalize(request.Notes);
        invoice.UpdatedAt = DateTime.UtcNow;

        foreach (var line in prepared)
        {
            if (!line.IsSerialTracked)
            {
                invoice.Items.Add(NewItem(invoice.Id, line, line.Quantity, line.QuantityBaseUnit, null));
                continue;
            }

            foreach (var serial in line.SerialEntities)
            {
                invoice.Items.Add(NewItem(invoice.Id, line, 1, 1, serial.SerialNumber));
                serial.Status = SerialStatus.Reserved;
                serial.SalesInvoiceId = invoice.Id;
                serial.Notes = $"محجوز في مسودة البيع {invoice.InvoiceNo}";
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return invoice.Id;
    }

    public async Task PostAsync(
        PostSalesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PaidAmount < 0)
            throw new InvalidOperationException("المدفوع لا يمكن أن يكون سالبًا.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var invoice = await db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .SingleOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("فاتورة البيع المطلوبة غير موجودة.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("يمكن ترحيل مسودات البيع فقط.");
        if (request.PaidAmount > invoice.TotalAmount)
            throw new InvalidOperationException("المدفوع أكبر من إجمالي الفاتورة.");
        var remaining = invoice.TotalAmount - request.PaidAmount;
        if (remaining > 0 && invoice.Customer is null)
            throw new InvalidOperationException("البيع الآجل أو الجزئي يتطلب اختيار عميل.");
        if (invoice.Customer is not null && remaining > 0)
        {
            var projectedBalance = invoice.Customer.CurrentBalance + remaining;
            if (invoice.Customer.CreditLimit <= 0 || projectedBalance > invoice.Customer.CreditLimit)
                throw new InvalidOperationException("الرصيد الجديد يتجاوز حد ائتمان العميل.");
        }

        Cashbox? cashbox = null;
        if (request.PaidAmount > 0)
        {
            if (!request.CashboxId.HasValue)
                throw new InvalidOperationException("اختر الخزينة التي سيدخل إليها المبلغ.");
            cashbox = await db.Cashboxes.SingleOrDefaultAsync(
                c => c.Id == request.CashboxId.Value && c.IsActive,
                cancellationToken) ?? throw new InvalidOperationException("الخزينة المختارة غير موجودة أو غير نشطة.");
        }

        var required = invoice.Items.GroupBy(i => i.ProductId)
            .ToDictionary(itemGroup => itemGroup.Key, itemGroup => itemGroup.Sum(i => i.QuantityBaseUnit));
        var productIds = required.Keys.ToList();
        var stocks = await db.StockMovements.Where(m => productIds.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(stockGroup => new { ProductId = stockGroup.Key, Quantity = stockGroup.Sum(m => m.QuantityBaseUnit) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);
        var insufficient = required.FirstOrDefault(x => stocks.GetValueOrDefault(x.Key) < x.Value);
        if (!insufficient.Equals(default(KeyValuePair<Guid, decimal>)))
            throw new InvalidOperationException("الرصيد غير كافٍ لأحد أصناف الفاتورة.");

        var serials = await db.ProductSerials.Where(s =>
            s.SalesInvoiceId == invoice.Id && s.Status == SerialStatus.Reserved)
            .ToListAsync(cancellationToken);
        var serialNumbers = invoice.Items.Where(i => i.SerialNumber != null).Select(i => i.SerialNumber!).ToList();
        if (serials.Count != serialNumbers.Count || serials.Any(s => !serialNumbers.Contains(s.SerialNumber)))
            throw new InvalidOperationException("حجز سيريالات الفاتورة غير مكتمل. افتح المسودة واحفظها مرة أخرى.");

        var now = DateTime.UtcNow;
        foreach (var item in invoice.Items)
        {
            await db.StockMovements.AddAsync(new StockMovement
            {
                Id = Guid.NewGuid(), ProductId = item.ProductId, ProductUnitId = item.ProductUnitId,
                QuantityBaseUnit = -item.QuantityBaseUnit, MovementType = StockMovementType.Sale,
                ReferenceType = "SalesInvoice", ReferenceId = invoice.Id,
                UserId = _authService.CurrentUserId, Notes = $"ترحيل فاتورة بيع {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
        }

        if (invoice.Customer is not null)
        {
            await db.CustomerTransactions.AddAsync(new CustomerTransaction
            {
                Id = Guid.NewGuid(), CustomerId = invoice.Customer.Id,
                TransactionType = "SalesInvoice", Direction = "debit", Amount = invoice.TotalAmount,
                ReferenceType = "SalesInvoice", ReferenceId = invoice.Id,
                UserId = _authService.CurrentUserId, Notes = $"فاتورة بيع {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
            if (request.PaidAmount > 0)
            {
                await db.CustomerTransactions.AddAsync(new CustomerTransaction
                {
                    Id = Guid.NewGuid(), CustomerId = invoice.Customer.Id,
                    TransactionType = "SalesPayment", Direction = "credit", Amount = request.PaidAmount,
                    ReferenceType = "SalesInvoice", ReferenceId = invoice.Id,
                    UserId = _authService.CurrentUserId, Notes = $"دفعة فاتورة {invoice.InvoiceNo}", CreatedAt = now
                }, cancellationToken);
            }
            invoice.Customer.CurrentBalance += remaining;
            invoice.Customer.UpdatedAt = now;
        }

        if (request.PaidAmount > 0 && cashbox is not null)
        {
            await db.InvoicePayments.AddAsync(new InvoicePayment
            {
                Id = Guid.NewGuid(), PaymentNo = await GeneratePaymentNoAsync(db, cancellationToken),
                PaymentType = "sales", PaymentMethod = request.PaymentMethod,
                SalesInvoiceId = invoice.Id, CustomerId = invoice.CustomerId,
                CashboxId = cashbox.Id, Amount = request.PaidAmount, PaymentDate = now,
                UserId = _authService.CurrentUserId, Notes = $"تحصيل فاتورة {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
            await db.CashMovements.AddAsync(new CashMovement
            {
                Id = Guid.NewGuid(), CashboxId = cashbox.Id,
                MovementType = CashMovementType.SalePayment, Direction = CashDirection.In,
                Amount = request.PaidAmount, ReferenceType = "SalesInvoice", ReferenceId = invoice.Id,
                UserId = _authService.CurrentUserId, Notes = $"تحصيل فاتورة بيع {invoice.InvoiceNo}", CreatedAt = now
            }, cancellationToken);
            cashbox.CurrentBalance += request.PaidAmount;
            cashbox.UpdatedAt = now;
        }

        foreach (var serial in serials)
        {
            serial.Status = SerialStatus.Sold;
            serial.CustomerId = invoice.CustomerId;
            serial.SoldAt = now;
            serial.Notes = $"مباع في فاتورة {invoice.InvoiceNo}";
        }

        invoice.PaidAmount = RoundMoney(request.PaidAmount);
        invoice.RemainingAmount = RoundMoney(remaining);
        invoice.PaymentStatus = remaining == 0
            ? PaymentStatus.Paid
            : request.PaidAmount > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid;
        invoice.Status = InvoiceStatus.Active;
        invoice.UpdatedAt = now;
        await db.AuditLogs.AddAsync(NewAudit(invoice.Id, AuditAction.Update, "Post"), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task VoidDraftAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var invoice = await db.SalesInvoices.SingleOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("فاتورة البيع المطلوبة غير موجودة.");
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("الفواتير المُرحّلة تُعالج من شاشة المرتجعات ولا تُلغى مباشرة.");

        var serials = await db.ProductSerials.Where(s =>
            s.SalesInvoiceId == invoice.Id && s.Status == SerialStatus.Reserved)
            .ToListAsync(cancellationToken);
        foreach (var serial in serials)
        {
            serial.Status = SerialStatus.Available;
            serial.SalesInvoiceId = null;
            serial.Notes = "متاح للبيع بعد إلغاء المسودة";
        }
        invoice.Status = InvoiceStatus.Voided;
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.AuditLogs.AddAsync(NewAudit(invoice.Id, AuditAction.Void, "VoidDraft"), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<List<PreparedSalesLine>> PrepareLinesAsync(
        AppDbContext db,
        SalesDraftRequest request,
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var unitIds = request.Items.Select(i => i.ProductUnitId).Distinct().ToList();
        var units = await db.ProductUnits.Include(u => u.Product)
            .Where(u => unitIds.Contains(u.Id) && u.IsActive && u.Product.IsActive)
            .ToDictionaryAsync(u => u.Id, cancellationToken);
        if (units.Count != unitIds.Count)
            throw new InvalidOperationException("إحدى وحدات البيع غير موجودة أو غير نشطة.");

        var normalizedSerials = request.Items.SelectMany(i => i.SerialNumbers)
            .Select(Normalize).Where(x => x is not null).Cast<string>().ToList();
        if (normalizedSerials.Count != normalizedSerials.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException("يوجد سيريال مكرر داخل الفاتورة.");

        var serialEntities = normalizedSerials.Count == 0
            ? new List<ProductSerial>()
            : await db.ProductSerials.Where(s => normalizedSerials.Contains(s.SerialNumber) &&
                (s.Status == SerialStatus.Available ||
                 (s.Status == SerialStatus.Reserved && s.SalesInvoiceId == invoiceId)))
                .ToListAsync(cancellationToken);
        if (serialEntities.Count != normalizedSerials.Count)
            throw new InvalidOperationException("أحد أرقام السيريال غير موجود أو غير متاح للبيع.");

        var canSellBelowMin = _permissionService.Can("Sales.SellBelowMinPrice");
        var result = new List<PreparedSalesLine>();
        foreach (var input in request.Items)
        {
            if (!units.TryGetValue(input.ProductUnitId, out var unit) || unit.ProductId != input.ProductId)
                throw new InvalidOperationException("الوحدة المختارة لا تتبع الصنف المحدد.");
            var minimum = await db.ProductPrices
                .Where(p => p.ProductUnitId == input.ProductUnitId &&
                    (!request.PriceGroupId.HasValue || p.PriceGroupId == request.PriceGroupId.Value))
                .OrderByDescending(p => request.PriceGroupId.HasValue && p.PriceGroupId == request.PriceGroupId.Value)
                .Select(p => (decimal?)p.MinSalePrice)
                .FirstOrDefaultAsync(cancellationToken) ?? 0;
            if (!canSellBelowMin && input.UnitPrice < minimum)
                throw new InvalidOperationException($"سعر '{unit.Product.Name}' أقل من الحد الأدنى {minimum:N2}.");

            var serialNumbers = input.SerialNumbers.Select(Normalize)
                .Where(x => x is not null).Cast<string>().ToList();
            var lineSerials = serialEntities.Where(s => serialNumbers.Contains(s.SerialNumber)).ToList();
            var quantityBase = input.Quantity * unit.ConversionFactorToBase;
            if (unit.Product.IsSerialTracked)
            {
                if (unit.ConversionFactorToBase != 1)
                    throw new InvalidOperationException($"بيع الصنف المسلسل '{unit.Product.Name}' يجب أن يكون بوحدة القطعة الأساسية.");
                if (input.Quantity != decimal.Truncate(input.Quantity) || serialNumbers.Count != decimal.ToInt32(input.Quantity))
                    throw new InvalidOperationException($"أدخل سيريالًا لكل قطعة من '{unit.Product.Name}'.");
                if (lineSerials.Any(s => s.ProductId != input.ProductId))
                    throw new InvalidOperationException("أحد السيريالات لا يتبع الصنف المحدد.");
            }
            else if (serialNumbers.Count > 0)
            {
                throw new InvalidOperationException($"الصنف '{unit.Product.Name}' غير مفعّل لتتبع السيريال.");
            }

            result.Add(new PreparedSalesLine(
                input.ProductId, input.ProductUnitId, input.Quantity, quantityBase,
                RoundMoney(input.UnitPrice), unit.Product.IsSerialTracked, lineSerials));
        }
        return result;
    }

    private static SalesInvoiceItem NewItem(
        Guid invoiceId,
        PreparedSalesLine line,
        decimal quantity,
        decimal quantityBase,
        string? serialNumber)
        => new()
        {
            Id = Guid.NewGuid(), SalesInvoiceId = invoiceId,
            ProductId = line.ProductId, ProductUnitId = line.ProductUnitId,
            Quantity = quantity, QuantityBaseUnit = quantityBase,
            UnitPrice = line.UnitPrice, DiscountAmount = 0,
            LineTotal = RoundMoney(quantity * line.UnitPrice),
            SerialNumber = serialNumber, CreatedAt = DateTime.UtcNow
        };

    private static void ValidateDraft(SalesDraftRequest request)
    {
        if (request.Items.Count == 0)
            throw new InvalidOperationException("أضف صنفًا واحدًا على الأقل.");
        if (request.Items.Any(i => i.ProductId == Guid.Empty || i.ProductUnitId == Guid.Empty))
            throw new InvalidOperationException("بيانات أحد الأصناف غير مكتملة.");
        if (request.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
            throw new InvalidOperationException("الكميات يجب أن تكون موجبة والأسعار لا يمكن أن تكون سالبة.");
        if (request.DiscountAmount < 0)
            throw new InvalidOperationException("الخصم لا يمكن أن يكون سالبًا.");
        if (request.DueDate.HasValue && request.DueDate.Value.Date < request.InvoiceDate.Date)
            throw new InvalidOperationException("تاريخ الاستحقاق لا يمكن أن يسبق تاريخ الفاتورة.");
    }

    private async Task<string> GenerateInvoiceNoAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await db.SalesInvoices.CountAsync(i => i.InvoiceNo.StartsWith($"SAL-{date}-"), cancellationToken);
        return $"SAL-{date}-{count + 1:0000}";
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
            Id = Guid.NewGuid(), TableName = "SalesInvoices", RecordId = invoiceId,
            Action = action, ChangedByUserId = _authService.CurrentUserId,
            NewValuesJson = $"{{\"Operation\":\"{operation}\"}}", CreatedAt = DateTime.UtcNow
        };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime AsUtcDate(DateTime date)
        => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record PreparedSalesLine(
        Guid ProductId,
        Guid ProductUnitId,
        decimal Quantity,
        decimal QuantityBaseUnit,
        decimal UnitPrice,
        bool IsSerialTracked,
        List<ProductSerial> SerialEntities);
}
