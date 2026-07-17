using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class ReturnService : IReturnService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;

    public ReturnService(IDbContextFactory<AppDbContext> dbFactory, IAuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public async Task<List<ReturnSourceInvoice>> SearchSourceInvoicesAsync(
        ReturnType returnType, string? query = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var term = query?.Trim();
        var pattern = $"%{term}%";
        if (returnType == ReturnType.SalesReturn)
        {
            var invoices = db.SalesInvoices.AsNoTracking()
                .Where(i => i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Returned);
            if (!string.IsNullOrWhiteSpace(term))
                invoices = invoices.Where(i => EF.Functions.ILike(i.InvoiceNo, pattern) ||
                    (i.Customer != null && EF.Functions.ILike(i.Customer.Name, pattern)));
            return await invoices.OrderByDescending(i => i.InvoiceDate).Take(200)
                .Select(i => new ReturnSourceInvoice(i.Id, returnType, i.InvoiceNo,
                    i.Customer != null ? i.Customer.Name : "عميل نقدي", i.InvoiceDate,
                    i.TotalAmount, i.Status.ToString())).ToListAsync(cancellationToken);
        }

        var purchases = db.PurchaseInvoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Returned);
        if (!string.IsNullOrWhiteSpace(term))
            purchases = purchases.Where(i => EF.Functions.ILike(i.InvoiceNo, pattern) ||
                EF.Functions.ILike(i.Supplier.Name, pattern) ||
                (i.SupplierInvoiceNo != null && EF.Functions.ILike(i.SupplierInvoiceNo, pattern)));
        return await purchases.OrderByDescending(i => i.InvoiceDate).Take(200)
            .Select(i => new ReturnSourceInvoice(i.Id, returnType, i.InvoiceNo, i.Supplier.Name,
                i.InvoiceDate, i.TotalAmount, i.Status.ToString())).ToListAsync(cancellationToken);
    }

    public async Task<ReturnSourceDetails> GetSourceAsync(
        ReturnType returnType, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return returnType == ReturnType.SalesReturn
            ? await GetSalesSourceAsync(db, invoiceId, cancellationToken)
            : await GetPurchaseSourceAsync(db, invoiceId, cancellationToken);
    }

    public async Task<List<ReturnListItem>> SearchReturnsAsync(
        string? query = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var returns = db.Returns.AsNoTracking().AsQueryable();
        var term = query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            returns = returns.Where(r => EF.Functions.ILike(r.ReturnNo, pattern) ||
                (r.Customer != null && EF.Functions.ILike(r.Customer.Name, pattern)) ||
                (r.Supplier != null && EF.Functions.ILike(r.Supplier.Name, pattern)));
        }
        return await returns.OrderByDescending(r => r.CreatedAt).Take(300)
            .Select(r => new ReturnListItem(
                r.Id, r.ReturnNo, r.ReturnType,
                r.Customer != null ? r.Customer.Name : r.Supplier != null ? r.Supplier.Name : "نقدي",
                r.OriginalSalesInvoice != null ? r.OriginalSalesInvoice.InvoiceNo :
                    r.OriginalPurchaseInvoice != null ? r.OriginalPurchaseInvoice.InvoiceNo : "-",
                r.TotalAmount, r.RefundMethod ?? "-", r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> PostAsync(PostReturnRequest request, CancellationToken cancellationToken = default)
    {
        var requestedLines = request.Items.Where(i => i.Quantity > 0).ToList();
        if (request.OriginalInvoiceId == Guid.Empty)
            throw new InvalidOperationException("اختر الفاتورة الأصلية.");
        if (requestedLines.Count == 0)
            throw new InvalidOperationException("أدخل كمية مرتجع لصنف واحد على الأقل.");
        if (requestedLines.Any(i => i.UnitPrice < 0))
            throw new InvalidOperationException("سعر الصنف غير صالح.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var source = request.ReturnType == ReturnType.SalesReturn
            ? await GetSalesSourceAsync(db, request.OriginalInvoiceId, cancellationToken)
            : await GetPurchaseSourceAsync(db, request.OriginalInvoiceId, cancellationToken);
        ValidateAgainstSource(source, requestedLines);

        var now = DateTime.UtcNow;
        var returnId = Guid.NewGuid();
        var total = RoundMoney(requestedLines.Sum(i => i.Quantity * i.UnitPrice));
        var document = new Return
        {
            Id = returnId,
            ReturnNo = await GenerateReturnNoAsync(db, request.ReturnType, cancellationToken),
            ReturnType = request.ReturnType,
            OriginalSalesInvoiceId = request.ReturnType == ReturnType.SalesReturn ? request.OriginalInvoiceId : null,
            OriginalPurchaseInvoiceId = request.ReturnType == ReturnType.PurchaseReturn ? request.OriginalInvoiceId : null,
            CustomerId = request.ReturnType == ReturnType.SalesReturn ? source.PartyId : null,
            SupplierId = request.ReturnType == ReturnType.PurchaseReturn ? source.PartyId : null,
            TotalAmount = total,
            RefundMethod = request.RefundMethod,
            Status = "active",
            CreatedByUserId = _authService.CurrentUserId,
            Notes = Normalize(request.Notes),
            CreatedAt = now
        };
        await db.Returns.AddAsync(document, cancellationToken);

        foreach (var line in requestedLines)
        {
            var sourceLine = source.Lines.Single(x => x.ProductId == line.ProductId &&
                x.ProductUnitId == line.ProductUnitId && x.UnitPrice == line.UnitPrice);
            var quantityBase = line.Quantity * sourceLine.ConversionFactorToBase;
            var serials = NormalizeSerials(line.SerialNumbers);
            if (sourceLine.IsSerialTracked &&
                (line.Quantity != decimal.Truncate(line.Quantity) || serials.Count != decimal.ToInt32(line.Quantity)))
                throw new InvalidOperationException($"الصنف '{sourceLine.ProductName}' يحتاج كمية صحيحة وسيريال لكل قطعة مرتجعة.");

            await db.ReturnItems.AddAsync(new ReturnItem
            {
                Id = Guid.NewGuid(), ReturnId = returnId, ProductId = line.ProductId,
                ProductUnitId = line.ProductUnitId, Quantity = line.Quantity,
                QuantityBaseUnit = quantityBase, UnitPrice = line.UnitPrice,
                Total = RoundMoney(line.Quantity * line.UnitPrice), Condition = line.Condition,
                Restock = request.ReturnType == ReturnType.PurchaseReturn ? false : line.Restock,
                Notes = BuildLineNotes(line.Notes, serials)
            }, cancellationToken);

            if (request.ReturnType == ReturnType.SalesReturn)
            {
                if (line.Restock)
                    AddStockMovement(db, returnId, line.ProductId, line.ProductUnitId,
                        quantityBase, StockMovementType.SalesReturn, document.ReturnNo, now);
                await UpdateSalesSerialsAsync(db, request.OriginalInvoiceId, line.ProductId,
                    serials, line.Restock, document.ReturnNo, cancellationToken);
            }
            else
            {
                var currentStock = await db.StockMovements.Where(m => m.ProductId == line.ProductId)
                    .SumAsync(m => (decimal?)m.QuantityBaseUnit, cancellationToken) ?? 0;
                if (currentStock < quantityBase)
                    throw new InvalidOperationException($"الرصيد المتاح من '{sourceLine.ProductName}' لا يكفي لمرتجع الشراء.");
                AddStockMovement(db, returnId, line.ProductId, line.ProductUnitId,
                    -quantityBase, StockMovementType.PurchaseReturn, document.ReturnNo, now);
                await UpdatePurchaseSerialsAsync(db, request.OriginalInvoiceId, line.ProductId,
                    serials, document.ReturnNo, cancellationToken);
            }
        }

        if (request.ReturnType == ReturnType.SalesReturn)
            await ApplySalesRefundAsync(db, document, request, total, now, cancellationToken);
        else
            await ApplyPurchaseRefundAsync(db, document, request, total, now, cancellationToken);

        var fullyReturned = source.Lines.All(sourceLine =>
        {
            var inRequest = requestedLines.Where(i => i.ProductId == sourceLine.ProductId &&
                i.ProductUnitId == sourceLine.ProductUnitId && i.UnitPrice == sourceLine.UnitPrice).Sum(i => i.Quantity);
            return sourceLine.AlreadyReturnedQuantity + inRequest >= sourceLine.OriginalQuantity;
        });
        if (fullyReturned)
        {
            if (request.ReturnType == ReturnType.SalesReturn)
                (await db.SalesInvoices.SingleAsync(i => i.Id == request.OriginalInvoiceId, cancellationToken)).Status = InvoiceStatus.Returned;
            else
                (await db.PurchaseInvoices.SingleAsync(i => i.Id == request.OriginalInvoiceId, cancellationToken)).Status = InvoiceStatus.Returned;
        }

        await db.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), TableName = "Returns", RecordId = returnId,
            Action = AuditAction.Insert, ChangedByUserId = _authService.CurrentUserId,
            NewValuesJson = JsonSerializer.Serialize(new { document.ReturnNo, Type = document.ReturnType.ToString(), document.TotalAmount }),
            CreatedAt = now
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return returnId;
    }

    private async Task<ReturnSourceDetails> GetSalesSourceAsync(AppDbContext db, Guid invoiceId, CancellationToken token)
    {
        var invoice = await db.SalesInvoices.AsNoTracking().Include(i => i.Customer)
            .Include(i => i.Items).ThenInclude(i => i.Product)
            .Include(i => i.Items).ThenInclude(i => i.ProductUnit).ThenInclude(u => u.Unit)
            .SingleOrDefaultAsync(i => i.Id == invoiceId && i.Status != InvoiceStatus.Draft &&
                i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Returned, token)
            ?? throw new InvalidOperationException("فاتورة البيع غير متاحة للمرتجع.");
        var previous = await db.ReturnItems.AsNoTracking()
            .Where(i => i.Return.OriginalSalesInvoiceId == invoiceId && i.Return.Status == "active")
            .ToListAsync(token);
        var serials = await db.ProductSerials.AsNoTracking()
            .Where(s => s.SalesInvoiceId == invoiceId && s.Status == SerialStatus.Sold)
            .ToListAsync(token);
        var lines = invoice.Items.GroupBy(i => new
        {
            i.ProductId, i.ProductUnitId, i.UnitPrice, i.Product.Name,
            UnitName = i.ProductUnit.Unit.Name, i.ProductUnit.ConversionFactorToBase,
            i.Product.IsSerialTracked
        }).Select(lineGroup => new ReturnSourceLine
        {
            ProductId = lineGroup.Key.ProductId, ProductUnitId = lineGroup.Key.ProductUnitId,
            ProductName = lineGroup.Key.Name, UnitName = lineGroup.Key.UnitName,
            ConversionFactorToBase = lineGroup.Key.ConversionFactorToBase,
            OriginalQuantity = lineGroup.Sum(i => i.Quantity), UnitPrice = lineGroup.Key.UnitPrice,
            AlreadyReturnedQuantity = previous.Where(r => r.ProductId == lineGroup.Key.ProductId &&
                r.ProductUnitId == lineGroup.Key.ProductUnitId && r.UnitPrice == lineGroup.Key.UnitPrice).Sum(r => r.Quantity),
            IsSerialTracked = lineGroup.Key.IsSerialTracked,
            AvailableSerials = serials.Where(s => s.ProductId == lineGroup.Key.ProductId).Select(s => s.SerialNumber).ToList()
        }).Where(i => i.AvailableQuantity > 0).OrderBy(i => i.ProductName).ToList();
        return new ReturnSourceDetails
        {
            InvoiceId = invoice.Id, ReturnType = ReturnType.SalesReturn, InvoiceNo = invoice.InvoiceNo,
            PartyId = invoice.CustomerId, PartyName = invoice.Customer?.Name ?? "عميل نقدي", Lines = lines
        };
    }

    private async Task<ReturnSourceDetails> GetPurchaseSourceAsync(AppDbContext db, Guid invoiceId, CancellationToken token)
    {
        var invoice = await db.PurchaseInvoices.AsNoTracking().Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(i => i.Product)
            .Include(i => i.Items).ThenInclude(i => i.ProductUnit).ThenInclude(u => u.Unit)
            .SingleOrDefaultAsync(i => i.Id == invoiceId && i.Status != InvoiceStatus.Draft &&
                i.Status != InvoiceStatus.Voided && i.Status != InvoiceStatus.Returned, token)
            ?? throw new InvalidOperationException("فاتورة الشراء غير متاحة للمرتجع.");
        var previous = await db.ReturnItems.AsNoTracking()
            .Where(i => i.Return.OriginalPurchaseInvoiceId == invoiceId && i.Return.Status == "active")
            .ToListAsync(token);
        var serials = await db.ProductSerials.AsNoTracking()
            .Where(s => s.PurchaseInvoiceId == invoiceId && s.Status == SerialStatus.Available)
            .ToListAsync(token);
        var lines = invoice.Items.GroupBy(i => new
        {
            i.ProductId, i.ProductUnitId, UnitPrice = i.UnitPurchasePrice, i.Product.Name,
            UnitName = i.ProductUnit.Unit.Name, i.ProductUnit.ConversionFactorToBase,
            i.Product.IsSerialTracked
        }).Select(lineGroup => new ReturnSourceLine
        {
            ProductId = lineGroup.Key.ProductId, ProductUnitId = lineGroup.Key.ProductUnitId,
            ProductName = lineGroup.Key.Name, UnitName = lineGroup.Key.UnitName,
            ConversionFactorToBase = lineGroup.Key.ConversionFactorToBase,
            OriginalQuantity = lineGroup.Sum(i => i.Quantity), UnitPrice = lineGroup.Key.UnitPrice,
            AlreadyReturnedQuantity = previous.Where(r => r.ProductId == lineGroup.Key.ProductId &&
                r.ProductUnitId == lineGroup.Key.ProductUnitId && r.UnitPrice == lineGroup.Key.UnitPrice).Sum(r => r.Quantity),
            IsSerialTracked = lineGroup.Key.IsSerialTracked,
            AvailableSerials = serials.Where(s => s.ProductId == lineGroup.Key.ProductId).Select(s => s.SerialNumber).ToList()
        }).Where(i => i.AvailableQuantity > 0).OrderBy(i => i.ProductName).ToList();
        return new ReturnSourceDetails
        {
            InvoiceId = invoice.Id, ReturnType = ReturnType.PurchaseReturn, InvoiceNo = invoice.InvoiceNo,
            PartyId = invoice.SupplierId, PartyName = invoice.Supplier.Name, Lines = lines
        };
    }

    private static void ValidateAgainstSource(ReturnSourceDetails source, IReadOnlyCollection<ReturnLineInput> lines)
    {
        foreach (var line in lines)
        {
            var match = source.Lines.SingleOrDefault(x => x.ProductId == line.ProductId &&
                x.ProductUnitId == line.ProductUnitId && x.UnitPrice == line.UnitPrice)
                ?? throw new InvalidOperationException("أحد الأصناف لا ينتمي للفاتورة أو سبق إرجاعه بالكامل.");
            if (line.Quantity > match.AvailableQuantity)
                throw new InvalidOperationException($"كمية '{match.ProductName}' أكبر من المتاح للمرتجع ({match.AvailableQuantity:0.###}).");
        }
    }

    private async Task ApplySalesRefundAsync(AppDbContext db, Return document, PostReturnRequest request,
        decimal total, DateTime now, CancellationToken token)
    {
        if (request.RefundMethod == "customer_balance")
        {
            if (!document.CustomerId.HasValue)
                throw new InvalidOperationException("الفاتورة النقدية لا يمكن ردها إلى رصيد عميل.");
            var customer = await db.Customers.SingleAsync(c => c.Id == document.CustomerId.Value, token);
            if (customer.CurrentBalance < total)
                throw new InvalidOperationException("مديونية العميل أقل من قيمة المرتجع؛ اختر رد نقدي.");
            customer.CurrentBalance -= total;
            customer.UpdatedAt = now;
            db.CustomerTransactions.Add(new CustomerTransaction
            {
                Id = Guid.NewGuid(), CustomerId = customer.Id, TransactionType = "SalesReturn",
                Direction = "credit", Amount = total, ReferenceType = "Return", ReferenceId = document.Id,
                UserId = _authService.CurrentUserId, Notes = $"مرتجع {document.ReturnNo}", CreatedAt = now
            });
            return;
        }
        var cashbox = await RequireCashboxAsync(db, request.CashboxId, token);
        if (cashbox.CurrentBalance < total)
            throw new InvalidOperationException("رصيد الخزينة غير كافٍ لرد قيمة المرتجع.");
        cashbox.CurrentBalance -= total; cashbox.UpdatedAt = now;
        AddCashMovement(db, cashbox.Id, document.Id, total, CashMovementType.RefundToCustomer,
            CashDirection.Out, document.ReturnNo, now);
    }

    private async Task ApplyPurchaseRefundAsync(AppDbContext db, Return document, PostReturnRequest request,
        decimal total, DateTime now, CancellationToken token)
    {
        if (request.RefundMethod == "supplier_balance")
        {
            var supplier = await db.Suppliers.SingleAsync(s => s.Id == document.SupplierId!.Value, token);
            if (supplier.CurrentBalance < total)
                throw new InvalidOperationException("مستحقات المورد أقل من قيمة المرتجع؛ اختر رد نقدي.");
            supplier.CurrentBalance -= total; supplier.UpdatedAt = now;
            db.SupplierTransactions.Add(new SupplierTransaction
            {
                Id = Guid.NewGuid(), SupplierId = supplier.Id, TransactionType = "PurchaseReturn",
                Direction = "debit", Amount = total, ReferenceType = "Return", ReferenceId = document.Id,
                UserId = _authService.CurrentUserId, Notes = $"مرتجع {document.ReturnNo}", CreatedAt = now
            });
            return;
        }
        var cashbox = await RequireCashboxAsync(db, request.CashboxId, token);
        cashbox.CurrentBalance += total; cashbox.UpdatedAt = now;
        AddCashMovement(db, cashbox.Id, document.Id, total, CashMovementType.RefundFromSupplier,
            CashDirection.In, document.ReturnNo, now);
    }

    private async Task UpdateSalesSerialsAsync(AppDbContext db, Guid invoiceId, Guid productId,
        IReadOnlyCollection<string> numbers, bool restock, string returnNo, CancellationToken token)
    {
        if (numbers.Count == 0) return;
        var serials = await db.ProductSerials.Where(s => s.SalesInvoiceId == invoiceId &&
            s.ProductId == productId && numbers.Contains(s.SerialNumber) && s.Status == SerialStatus.Sold).ToListAsync(token);
        if (serials.Count != numbers.Count)
            throw new InvalidOperationException("سيريال مرتجع البيع غير صحيح أو سبق إرجاعه.");
        foreach (var serial in serials)
        {
            serial.Status = restock ? SerialStatus.Available : SerialStatus.Damaged;
            serial.CustomerId = null; serial.SoldAt = null;
            serial.Notes = $"مرتجع بيع {returnNo}";
        }
    }

    private async Task UpdatePurchaseSerialsAsync(AppDbContext db, Guid invoiceId, Guid productId,
        IReadOnlyCollection<string> numbers, string returnNo, CancellationToken token)
    {
        if (numbers.Count == 0) return;
        var serials = await db.ProductSerials.Where(s => s.PurchaseInvoiceId == invoiceId &&
            s.ProductId == productId && numbers.Contains(s.SerialNumber) && s.Status == SerialStatus.Available).ToListAsync(token);
        if (serials.Count != numbers.Count)
            throw new InvalidOperationException("سيريال مرتجع الشراء غير صحيح أو غير متاح.");
        foreach (var serial in serials)
        {
            serial.Status = SerialStatus.Cancelled;
            serial.Notes = $"مرتجع شراء {returnNo}";
        }
    }

    private void AddStockMovement(AppDbContext db, Guid returnId, Guid productId, Guid unitId,
        decimal quantity, StockMovementType type, string returnNo, DateTime now)
        => db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(), ProductId = productId, ProductUnitId = unitId,
            QuantityBaseUnit = quantity, MovementType = type, ReferenceType = "Return",
            ReferenceId = returnId, UserId = _authService.CurrentUserId,
            Notes = $"مرتجع {returnNo}", CreatedAt = now
        });

    private void AddCashMovement(AppDbContext db, Guid cashboxId, Guid returnId, decimal amount,
        CashMovementType type, CashDirection direction, string returnNo, DateTime now)
        => db.CashMovements.Add(new CashMovement
        {
            Id = Guid.NewGuid(), CashboxId = cashboxId, MovementType = type, Direction = direction,
            Amount = amount, ReferenceType = "Return", ReferenceId = returnId,
            UserId = _authService.CurrentUserId, Notes = $"مرتجع {returnNo}", CreatedAt = now
        });

    private static async Task<Cashbox> RequireCashboxAsync(AppDbContext db, Guid? cashboxId, CancellationToken token)
    {
        if (!cashboxId.HasValue) throw new InvalidOperationException("اختر الخزينة.");
        return await db.Cashboxes.SingleOrDefaultAsync(c => c.Id == cashboxId && c.IsActive, token)
            ?? throw new InvalidOperationException("الخزينة غير موجودة أو غير نشطة.");
    }

    private static List<string> NormalizeSerials(IEnumerable<string> values)
        => values.SelectMany(v => v.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(v => v.Trim()).Where(v => v.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string? BuildLineNotes(string? notes, IReadOnlyCollection<string> serials)
    {
        var clean = Normalize(notes);
        var serialText = serials.Count == 0 ? null : $"Serials: {string.Join(", ", serials)}";
        return string.Join(" | ", new[] { clean, serialText }.Where(x => x != null));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static decimal RoundMoney(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static async Task<string> GenerateReturnNoAsync(AppDbContext db, ReturnType type, CancellationToken token)
    {
        var prefix = type == ReturnType.SalesReturn ? "SRT" : "PRT";
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await db.Returns.CountAsync(r => r.ReturnNo.StartsWith($"{prefix}-{date}-"), token);
        return $"{prefix}-{date}-{count + 1:0000}";
    }
}
