using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text.Json;

namespace HardwarePaintShop.Api;

public sealed class MobileCommandService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MobileCommandService(
        IDbContextFactory<AppDbContext> dbFactory,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<Guid> CreateProductAsync(
        ApiUserContext user,
        ProductCreateRequest request,
        Guid? operationId,
        CancellationToken cancellationToken)
    {
        Require(user, "Product.Create");
        var name = request.Name?.Trim() ?? string.Empty;
        var code = Normalize(request.ProductCode);
        var barcode = Normalize(request.Barcode);
        if (name.Length < 2) throw new ApiProblemException(400, "اسم المنتج مطلوب.");
        if (request.MinStockBaseQuantity < 0) throw new ApiProblemException(400, "الحد الأدنى لا يمكن أن يكون سالبًا.");
        if (request.SalePrice < 0 || request.MinSalePrice < 0) throw new ApiProblemException(400, "السعر لا يمكن أن يكون سالبًا.");
        if (request.MinSalePrice > request.SalePrice) throw new ApiProblemException(400, "أقل سعر بيع أكبر من سعر البيع.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        if (operationId.HasValue && await IsProcessedAsync(db, user.DeviceId, "product_create", operationId.Value, cancellationToken))
            return await ReadProcessedRecordIdAsync(db, user.DeviceId, "product_create", operationId.Value, cancellationToken);
        var unit = await db.Units.SingleOrDefaultAsync(u => u.Id == request.BaseUnitId && u.IsActive, cancellationToken)
            ?? throw new ApiProblemException(400, "الوحدة الأساسية غير موجودة أو غير نشطة.");
        if (code is not null && await db.Products.AnyAsync(p => p.ProductCode == code, cancellationToken))
            throw new ApiProblemException(409, "كود المنتج مستخدم بالفعل.");
        if (barcode is not null && await db.ProductBarcodes.AnyAsync(b => b.Barcode == barcode, cancellationToken))
            throw new ApiProblemException(409, "الباركود مستخدم بالفعل.");
        if (request.CategoryId.HasValue && !await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken))
            throw new ApiProblemException(400, "التصنيف غير موجود أو غير نشط.");
        if (request.MainSupplierId.HasValue && !await db.Suppliers.AnyAsync(s => s.Id == request.MainSupplierId && s.IsActive, cancellationToken))
            throw new ApiProblemException(400, "المورد غير موجود أو غير نشط.");

        var now = DateTime.UtcNow;
        var productId = Guid.NewGuid();
        var productUnitId = Guid.NewGuid();
        var imagePath = await SaveImageAsync(request.ImageBase64, productId, cancellationToken);
        db.Products.Add(new Product
        {
            Id = productId, ProductCode = code, Name = name, CategoryId = request.CategoryId,
            BaseUnitId = unit.Id, MainSupplierId = request.MainSupplierId,
            MinStockBaseQuantity = request.MinStockBaseQuantity,
            IsSerialTracked = request.IsSerialTracked, ImagePath = imagePath,
            Notes = Normalize(request.Notes), IsActive = true, CreatedAt = now, UpdatedAt = now
        });
        db.ProductUnits.Add(new ProductUnit
        {
            Id = productUnitId, ProductId = productId, UnitId = unit.Id,
            ConversionFactorToBase = 1, IsDefaultPurchase = true, IsDefaultSale = true,
            IsActive = true, CreatedAt = now, UpdatedAt = now
        });
        if (barcode is not null)
            db.ProductBarcodes.Add(new ProductBarcode
            {
                Id = Guid.NewGuid(), ProductId = productId, ProductUnitId = productUnitId,
                Barcode = barcode, CreatedAt = now
            });
        if (request.PriceGroupId.HasValue && request.SalePrice.HasValue)
        {
            if (!await db.PriceGroups.AnyAsync(g => g.Id == request.PriceGroupId && g.IsActive, cancellationToken))
                throw new ApiProblemException(400, "فئة السعر غير موجودة أو غير نشطة.");
            db.ProductPrices.Add(new ProductPrice
            {
                Id = Guid.NewGuid(), ProductId = productId, ProductUnitId = productUnitId,
                PriceGroupId = request.PriceGroupId.Value, SalePrice = request.SalePrice.Value,
                MinSalePrice = request.MinSalePrice ?? 0, CreatedAt = now, UpdatedAt = now
            });
        }
        db.AuditLogs.Add(NewAudit(user.UserId, "Products", productId, AuditAction.Insert,
            new { Source = "MobileApi", name, code }));
        if (operationId.HasValue) AddSyncLog(db, user.DeviceId, "product_create", operationId.Value, productId, "success");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return productId;
    }

    public async Task<Guid> CollectCustomerAsync(
        ApiUserContext user,
        Guid customerId,
        CustomerPaymentRequest request,
        Guid? operationId,
        CancellationToken cancellationToken)
    {
        Require(user, "Finance.CustomerCollection");
        if (request.Amount <= 0) throw new ApiProblemException(400, "المبلغ يجب أن يكون أكبر من صفر.");
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        if (operationId.HasValue && await IsProcessedAsync(db, user.DeviceId, "customer_payment", operationId.Value, cancellationToken))
            return await ReadProcessedRecordIdAsync(db, user.DeviceId, "customer_payment", operationId.Value, cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == customerId && c.IsActive, cancellationToken)
            ?? throw new ApiProblemException(404, "العميل غير موجود أو غير نشط.");
        if (request.Amount > customer.CurrentBalance)
            throw new ApiProblemException(400, "المبلغ أكبر من مديونية العميل الحالية.");
        var cashbox = await db.Cashboxes.SingleOrDefaultAsync(c => c.Id == request.CashboxId && c.IsActive, cancellationToken)
            ?? throw new ApiProblemException(400, "الخزينة غير موجودة أو غير نشطة.");
        var id = Guid.NewGuid();
        var date = AsUtc(request.OperationDate);
        var notes = Normalize(request.Notes);
        db.CustomerTransactions.Add(new CustomerTransaction
        {
            Id = id, CustomerId = customer.Id, TransactionType = "CustomerCollection",
            Direction = "credit", Amount = request.Amount, ReferenceType = "MobileCollection",
            ReferenceId = id, UserId = user.UserId, Notes = notes, CreatedAt = date
        });
        db.CashMovements.Add(new CashMovement
        {
            Id = Guid.NewGuid(), CashboxId = cashbox.Id, MovementType = CashMovementType.CustomerCollection,
            Direction = CashDirection.In, Amount = request.Amount, ReferenceType = "MobileCollection",
            ReferenceId = id, UserId = user.UserId, Notes = notes, CreatedAt = date
        });
        customer.CurrentBalance -= request.Amount; customer.UpdatedAt = DateTime.UtcNow;
        cashbox.CurrentBalance += request.Amount; cashbox.UpdatedAt = DateTime.UtcNow;
        db.AuditLogs.Add(NewAudit(user.UserId, "CustomerTransactions", id, AuditAction.Insert,
            new { Source = "MobileApi", customerId, request.Amount }));
        if (operationId.HasValue) AddSyncLog(db, user.DeviceId, "customer_payment", operationId.Value, id, "success");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async Task<Guid> AdjustStockAsync(
        ApiUserContext user,
        StockAdjustmentRequest request,
        Guid? operationId,
        CancellationToken cancellationToken)
    {
        Require(user, "Inventory.Adjust");
        if (request.QuantityBase == 0) throw new ApiProblemException(400, "فرق التسوية لا يمكن أن يكون صفرًا.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new ApiProblemException(400, "سبب التسوية مطلوب.");
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        if (operationId.HasValue && await IsProcessedAsync(db, user.DeviceId, "stock_adjustment", operationId.Value, cancellationToken))
            return await ReadProcessedRecordIdAsync(db, user.DeviceId, "stock_adjustment", operationId.Value, cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken)
            ?? throw new ApiProblemException(404, "المنتج غير موجود أو غير نشط.");
        var stock = await db.StockMovements.Where(m => m.ProductId == product.Id)
            .SumAsync(m => (decimal?)m.QuantityBaseUnit, cancellationToken) ?? 0;
        if (stock + request.QuantityBase < 0)
            throw new ApiProblemException(400, "التسوية ستجعل رصيد المنتج سالبًا.");
        var id = Guid.NewGuid();
        var date = AsUtc(request.OperationDate);
        db.StockMovements.Add(new StockMovement
        {
            Id = id, ProductId = product.Id, QuantityBaseUnit = request.QuantityBase,
            MovementType = StockMovementType.Adjustment, ReferenceType = "MobileAdjustment",
            ReferenceId = id, UserId = user.UserId, Notes = request.Reason.Trim(), CreatedAt = date
        });
        db.AuditLogs.Add(NewAudit(user.UserId, "StockMovements", id, AuditAction.Insert,
            new { Source = "MobileApi", productId = product.Id, request.QuantityBase, request.Reason }));
        if (operationId.HasValue) AddSyncLog(db, user.DeviceId, "stock_adjustment", operationId.Value, id, "success");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async Task<List<ClientOperationResult>> UploadAsync(
        ApiUserContext user,
        SyncUploadRequest request,
        CancellationToken cancellationToken)
    {
        Require(user, "Sync.Upload");
        if (request.Operations.Count > 100)
            throw new ApiProblemException(400, "الدفعة الواحدة لا يمكن أن تتجاوز 100 عملية.");
        var results = new List<ClientOperationResult>();
        foreach (var operation in request.Operations)
        {
            try
            {
                Guid id = operation.Type.ToLowerInvariant() switch
                {
                    "product_create" => await CreateProductAsync(user,
                        operation.Payload.Deserialize<ProductCreateRequest>(JsonOptions)
                            ?? throw new ApiProblemException(400, "بيانات المنتج غير صالحة."),
                        operation.OperationId, cancellationToken),
                    "customer_payment" => await CollectFromPayloadAsync(user, operation, cancellationToken),
                    "stock_adjustment" => await AdjustStockAsync(user,
                        operation.Payload.Deserialize<StockAdjustmentRequest>(JsonOptions)
                            ?? throw new ApiProblemException(400, "بيانات التسوية غير صالحة."),
                        operation.OperationId, cancellationToken),
                    _ => throw new ApiProblemException(400, $"نوع العملية غير مدعوم: {operation.Type}")
                };
                results.Add(new ClientOperationResult(operation.OperationId, "success", null, id));
            }
            catch (Exception ex)
            {
                results.Add(new ClientOperationResult(operation.OperationId, "failed", ex.Message, null));
            }
        }
        return results;
    }

    private async Task<Guid> CollectFromPayloadAsync(ApiUserContext user, ClientOperation operation, CancellationToken token)
    {
        var envelope = operation.Payload.Deserialize<CustomerPaymentEnvelope>(JsonOptions)
            ?? throw new ApiProblemException(400, "بيانات التحصيل غير صالحة.");
        return await CollectCustomerAsync(user, envelope.CustomerId,
            new CustomerPaymentRequest(envelope.Amount, envelope.CashboxId, envelope.OperationDate, envelope.Notes),
            operation.OperationId, token);
    }

    private async Task<string?> SaveImageAsync(string? encoded, Guid productId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(encoded)) return null;
        var raw = encoded.Contains(',') ? encoded[(encoded.IndexOf(',') + 1)..] : encoded;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(raw); }
        catch { throw new ApiProblemException(400, "صيغة صورة المنتج غير صالحة."); }
        var max = int.TryParse(_configuration["Api:MaxImageBytes"], out var configured) ? configured : 5 * 1024 * 1024;
        if (bytes.Length > max) throw new ApiProblemException(400, "حجم الصورة أكبر من الحد المسموح.");
        var extension = bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 ? ".png" : ".jpg";
        var folder = Path.Combine(_environment.ContentRootPath, "data", "product-images");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, productId + extension);
        await File.WriteAllBytesAsync(path, bytes, token);
        return path;
    }

    private static void Require(ApiUserContext user, string permission)
    {
        if (!user.Can(permission)) throw new ApiProblemException(403, $"الصلاحية المطلوبة: {permission}");
    }

    private static async Task<bool> IsProcessedAsync(
        AppDbContext db, Guid deviceId, string entity, Guid operationId, CancellationToken token)
        => await db.SyncLogs.AnyAsync(s => s.DeviceId == deviceId && s.Direction == SyncDirection.Upload &&
            s.EntityName == entity && s.RecordId == operationId && s.Status == "success", token);

    private static async Task<Guid> ReadProcessedRecordIdAsync(
        AppDbContext db, Guid deviceId, string entity, Guid operationId, CancellationToken token)
    {
        var message = await db.SyncLogs.AsNoTracking().Where(s => s.DeviceId == deviceId &&
            s.Direction == SyncDirection.Upload && s.EntityName == entity && s.RecordId == operationId && s.Status == "success")
            .Select(s => s.Message).FirstAsync(token);
        return Guid.TryParse(message, out var result) ? result : operationId;
    }

    private static void AddSyncLog(AppDbContext db, Guid deviceId, string entity, Guid operationId, Guid serverRecordId, string status)
        => db.SyncLogs.Add(new SyncLog
        {
            Id = Guid.NewGuid(), DeviceId = deviceId, Direction = SyncDirection.Upload,
            EntityName = entity, RecordId = operationId, Status = status,
            Message = serverRecordId.ToString(), CreatedAt = DateTime.UtcNow
        });

    private static AuditLog NewAudit(Guid userId, string table, Guid recordId, AuditAction action, object value)
        => new()
        {
            Id = Guid.NewGuid(), TableName = table, RecordId = recordId, Action = action,
            ChangedByUserId = userId, NewValuesJson = JsonSerializer.Serialize(value), CreatedAt = DateTime.UtcNow
        };

    private static DateTime AsUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class CustomerPaymentEnvelope
    {
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public Guid CashboxId { get; set; }
        public DateTime OperationDate { get; set; }
        public string? Notes { get; set; }
    }
}
