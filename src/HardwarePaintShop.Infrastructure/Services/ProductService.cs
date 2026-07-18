using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Helpers;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class ProductService : IProductService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPermissionService _permissionService;
    private readonly IAuthService? _authService;

    public ProductService(
        IDbContextFactory<AppDbContext> dbFactory,
        IPermissionService permissionService,
        IAuthService? authService = null)
    {
        _dbFactory = dbFactory;
        _permissionService = permissionService;
        _authService = authService;
    }

    public async Task<List<ProductListItem>> SearchAsync(
        ProductSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Products.AsNoTracking();

        if (!criteria.IncludeInactive)
            query = query.Where(p => p.IsActive);

        if (criteria.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == criteria.CategoryId.Value);

        if (criteria.SupplierId.HasValue)
            query = query.Where(p => p.MainSupplierId == criteria.SupplierId.Value);

        var term = criteria.Query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            query = query.Where(p =>
                (p.ProductCode != null && EF.Functions.ILike(p.ProductCode, pattern)) ||
                EF.Functions.ILike(p.Name, pattern) ||
                p.ProductBarcodes.Any(b => EF.Functions.ILike(b.Barcode, pattern)) ||
                p.ProductSerials.Any(s => EF.Functions.ILike(s.SerialNumber, pattern)));
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductListItem(
                p.Id,
                p.ProductCode,
                p.Name,
                p.Category != null ? p.Category.Name : null,
                p.BaseUnit.Name,
                p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0,
                p.IsSerialTracked,
                p.IsActive,
                p.ProductBarcodes.OrderBy(b => b.CreatedAt).Select(b => b.Barcode).FirstOrDefault(),
                p.MainSupplier != null ? p.MainSupplier.Name : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetails> GetAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.ProductUnits).ThenInclude(pu => pu.Unit)
            .Include(p => p.ProductPrices).ThenInclude(pp => pp.ProductUnit)
            .Include(p => p.ProductBarcodes)
            .SingleOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("المنتج المطلوب غير موجود.");

        return new ProductDetails
        {
            Id = product.Id,
            ProductCode = product.ProductCode,
            Name = product.Name,
            CategoryId = product.CategoryId,
            MainSupplierId = product.MainSupplierId,
            BaseUnitId = product.BaseUnitId,
            ImagePath = product.ImagePath,
            MinStockBaseQuantity = product.MinStockBaseQuantity,
            IsSerialTracked = product.IsSerialTracked,
            IsActive = product.IsActive,
            Notes = product.Notes,
            Units = product.ProductUnits
                .OrderByDescending(u => u.UnitId == product.BaseUnitId)
                .ThenBy(u => u.Unit.Name)
                .Select(u => new ProductUnitData(
                    u.Id,
                    u.UnitId,
                    u.Unit.Name,
                    u.ConversionFactorToBase,
                    u.IsDefaultPurchase,
                    u.IsDefaultSale,
                    u.IsActive))
                .ToList(),
            Prices = product.ProductPrices
                .OrderBy(p => p.ProductUnit.UnitId)
                .ThenBy(p => p.PriceGroupId)
                .Select(p => new ProductPriceData(
                    p.Id,
                    p.ProductUnit.UnitId,
                    p.PriceGroupId,
                    p.SalePrice,
                    p.MinSalePrice))
                .ToList(),
            Barcodes = product.ProductBarcodes
                .OrderBy(b => b.CreatedAt)
                .Select(b => new ProductBarcodeData(
                    b.Id,
                    b.ProductUnitId.HasValue
                        ? product.ProductUnits.First(u => u.Id == b.ProductUnitId.Value).UnitId
                        : null,
                    b.Barcode))
                .ToList()
        };
    }

    public async Task<Guid> SaveAsync(
        ProductSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var code = NullIfWhiteSpace(request.ProductCode);
        if (code is not null && await db.Products.AnyAsync(
                p => p.ProductCode == code && (!request.Id.HasValue || p.Id != request.Id.Value),
                cancellationToken))
        {
            throw new InvalidOperationException($"كود المنتج '{code}' مستخدم بالفعل.");
        }

        if (request.CategoryId.HasValue && !await db.Categories.AnyAsync(
                c => c.Id == request.CategoryId.Value && c.IsActive,
                cancellationToken))
        {
            throw new InvalidOperationException("التصنيف المختار غير موجود أو غير نشط.");
        }

        if (request.MainSupplierId.HasValue && !await db.Suppliers.AnyAsync(
                s => s.Id == request.MainSupplierId.Value && s.IsActive,
                cancellationToken))
        {
            throw new InvalidOperationException("المورد الرئيسي المختار غير موجود أو غير نشط.");
        }

        var requestedUnitIds = request.Units.Select(u => u.UnitId)
            .Append(request.BaseUnitId)
            .Distinct()
            .ToList();
        var validUnitIds = await db.Units
            .Where(u => requestedUnitIds.Contains(u.Id) && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        if (validUnitIds.Count != requestedUnitIds.Count)
            throw new InvalidOperationException("إحدى وحدات المنتج غير موجودة أو غير نشطة.");

        var requestedPriceGroupIds = request.Prices.Select(p => p.PriceGroupId).Distinct().ToList();
        if (requestedPriceGroupIds.Count > 0)
        {
            var validPriceGroupCount = await db.PriceGroups.CountAsync(
                p => requestedPriceGroupIds.Contains(p.Id) && p.IsActive,
                cancellationToken);
            if (validPriceGroupCount != requestedPriceGroupIds.Count)
                throw new InvalidOperationException("إحدى فئات الأسعار غير موجودة أو غير نشطة.");
        }

        var normalizedBarcodes = request.Barcodes
            .Select(b => b.Barcode.Trim())
            .Where(b => b.Length > 0)
            .ToList();
        if (normalizedBarcodes.Count != normalizedBarcodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException("لا يمكن تكرار نفس الباركود داخل المنتج.");

        if (normalizedBarcodes.Count > 0 && await db.ProductBarcodes.AnyAsync(
                b => normalizedBarcodes.Contains(b.Barcode) &&
                     (!request.Id.HasValue || b.ProductId != request.Id.Value),
                cancellationToken))
        {
            throw new InvalidOperationException("أحد أرقام الباركود مستخدم لمنتج آخر.");
        }

        Product product;
        var isNew = !request.Id.HasValue;
        if (request.Id.HasValue)
        {
            product = await db.Products
                .Include(p => p.ProductUnits)
                .Include(p => p.ProductPrices)
                .Include(p => p.ProductBarcodes)
                .SingleOrDefaultAsync(p => p.Id == request.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException("المنتج المطلوب تعديله غير موجود.");

            if (product.IsSerialTracked && !request.IsSerialTracked &&
                await db.ProductSerials.AnyAsync(s => s.ProductId == product.Id, cancellationToken))
            {
                throw new InvalidOperationException("لا يمكن إلغاء تتبع السيريال لمنتج لديه أرقام سيريال مسجلة.");
            }
        }
        else
        {
            product = new Product
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            await db.Products.AddAsync(product, cancellationToken);
        }

        product.ProductCode = code;
        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.MainSupplierId = request.MainSupplierId;
        product.BaseUnitId = request.BaseUnitId;
        product.ImagePath = NullIfWhiteSpace(request.ImagePath);
        product.MinStockBaseQuantity = request.MinStockBaseQuantity;
        product.IsSerialTracked = request.IsSerialTracked;
        product.IsActive = request.IsActive;
        product.Notes = NullIfWhiteSpace(request.Notes);
        product.UpdatedAt = DateTime.UtcNow;

        SynchronizeUnits(product, request);
        SynchronizePrices(product, request, db);
        SynchronizeBarcodes(product, request, db);

        if (isNew)
        {
            if (request.OpeningCostBaseUnit > 0)
            {
                product.ProductCost = new ProductCost
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    LastPurchasePriceBaseUnit = request.OpeningCostBaseUnit,
                    AverageCostBaseUnit = request.OpeningCostBaseUnit,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            if (request.OpeningQuantityBase > 0)
            {
                var baseProductUnit = product.ProductUnits.Single(u =>
                    u.UnitId == request.BaseUnitId && u.IsActive);
                db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductUnitId = baseProductUnit.Id,
                    QuantityBaseUnit = request.OpeningQuantityBase,
                    MovementType = StockMovementType.OpeningBalance,
                    ReferenceType = "ProductOpeningBalance",
                    UserId = _authService?.CurrentUserId,
                    Notes = "رصيد افتتاحي عند إنشاء المنتج",
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TableName = "Products",
                RecordId = product.Id,
                Action = AuditAction.Insert,
                ChangedByUserId = _authService?.CurrentUserId,
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    product.Name,
                    product.ProductCode,
                    request.OpeningQuantityBase,
                    request.OpeningCostBaseUnit
                }),
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return product.Id;
    }

    public async Task SetActiveAsync(
        Guid productId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new KeyNotFoundException("المنتج المطلوب غير موجود.");
        product.IsActive = isActive;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<PriceInquiryResult>> InquirePriceAsync(
        string query,
        Guid? priceGroupId = null,
        CancellationToken cancellationToken = default)
    {
        var term = query.Trim();
        if (term.Length == 0)
            return new List<PriceInquiryResult>();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var pattern = $"%{term}%";
        var products = await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive &&
                ((p.ProductCode != null && EF.Functions.ILike(p.ProductCode, pattern)) ||
                 EF.Functions.ILike(p.Name, pattern) ||
                 p.ProductBarcodes.Any(b => EF.Functions.ILike(b.Barcode, pattern)) ||
                 p.ProductSerials.Any(s => EF.Functions.ILike(s.SerialNumber, pattern))))
            .Include(p => p.ProductUnits).ThenInclude(u => u.Unit)
            .Include(p => p.ProductPrices).ThenInclude(p => p.PriceGroup)
            .Include(p => p.ProductBarcodes)
            .Include(p => p.ProductSerials)
            .Include(p => p.Category)
            .Include(p => p.MainSupplier)
            .Include(p => p.ProductCost)
            .OrderByDescending(p => p.ProductCode == term)
            .ThenBy(p => p.Name)
            .Take(50)
            .ToListAsync(cancellationToken);

        var productIds = products.Select(p => p.Id).ToList();
        var canViewCost = _permissionService.Can("Product.ViewCost");
        var stocks = await db.StockMovements
            .AsNoTracking()
            .Where(m => productIds.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(m => m.QuantityBaseUnit) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);

        var results = new List<PriceInquiryResult>();
        foreach (var product in products)
        {
            var stock = stocks.GetValueOrDefault(product.Id);
            var matchSource = GetMatchSource(product, term);
            foreach (var unit in product.ProductUnits.Where(u => u.IsActive).OrderBy(u => u.Unit.Name))
            {
                var prices = product.ProductPrices
                    .Where(p => p.ProductUnitId == unit.Id &&
                                (!priceGroupId.HasValue || p.PriceGroupId == priceGroupId.Value))
                    .OrderBy(p => p.PriceGroup.Name)
                    .ToList();
                var barcode = product.ProductBarcodes
                    .Where(b => b.ProductUnitId == unit.Id || b.ProductUnitId == null)
                    .OrderByDescending(b => b.ProductUnitId == unit.Id)
                    .Select(b => b.Barcode)
                    .FirstOrDefault();

                if (prices.Count == 0)
                {
                    results.Add(new PriceInquiryResult(
                        product.Id, product.ProductCode, product.Name, unit.Unit.Name,
                        unit.ConversionFactorToBase, "— غير مسعّر —", null, null, stock,
                        barcode, product.IsSerialTracked, matchSource,
                        product.Category?.Name, product.MainSupplier?.Name, product.ImagePath,
                        StockDisplayFormatter.FormatCompound(stock, product.ProductUnits
                            .Where(u => u.IsActive)
                            .Select(u => (u.Unit.Name, u.ConversionFactorToBase))),
                        canViewCost ? product.ProductCost?.LastPurchasePriceBaseUnit : null));
                    continue;
                }

                results.AddRange(prices.Select(price => new PriceInquiryResult(
                    product.Id, product.ProductCode, product.Name, unit.Unit.Name,
                    unit.ConversionFactorToBase, price.PriceGroup.Name, price.SalePrice,
                    price.MinSalePrice, stock, barcode, product.IsSerialTracked, matchSource,
                    product.Category?.Name, product.MainSupplier?.Name, product.ImagePath,
                    StockDisplayFormatter.FormatCompound(stock, product.ProductUnits
                        .Where(u => u.IsActive)
                        .Select(u => (u.Unit.Name, u.ConversionFactorToBase))),
                    canViewCost ? product.ProductCost?.LastPurchasePriceBaseUnit : null)));
            }
        }

        return results;
    }

    private static void ValidateRequest(ProductSaveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("اسم المنتج مطلوب.");
        if (request.BaseUnitId == Guid.Empty)
            throw new InvalidOperationException("الوحدة الأساسية مطلوبة.");
        if (request.MinStockBaseQuantity < 0)
            throw new InvalidOperationException("حد الطلب لا يمكن أن يكون سالبًا.");
        if (request.OpeningQuantityBase < 0 || request.OpeningCostBaseUnit < 0)
            throw new InvalidOperationException("الرصيد الافتتاحي والتكلفة لا يمكن أن يكونا سالبين.");
        if (request.Id.HasValue && (request.OpeningQuantityBase != 0 || request.OpeningCostBaseUnit != 0))
            throw new InvalidOperationException("الرصيد الافتتاحي يُسجل عند إنشاء المنتج فقط.");
        if (request.IsSerialTracked && request.OpeningQuantityBase > 0)
            throw new InvalidOperationException("المنتج المتتبع بالسيريال يُنشأ برصيد صفر، ثم تُسجل أرقام السيريال من فاتورة الشراء.");

        var units = request.Units.ToList();
        if (units.GroupBy(u => u.UnitId).Any(g => g.Count() > 1))
            throw new InvalidOperationException("لا يمكن تكرار نفس الوحدة داخل المنتج.");
        if (units.Any(u => u.ConversionFactorToBase <= 0))
            throw new InvalidOperationException("معامل تحويل الوحدة يجب أن يكون أكبر من صفر.");
        if (units.Count(u => u.IsDefaultPurchase) > 1 || units.Count(u => u.IsDefaultSale) > 1)
            throw new InvalidOperationException("اختر وحدة افتراضية واحدة فقط للبيع وواحدة فقط للشراء.");

        var prices = request.Prices.ToList();
        if (prices.GroupBy(p => new { p.UnitId, p.PriceGroupId }).Any(g => g.Count() > 1))
            throw new InvalidOperationException("سعر الوحدة في فئة السعر مكرر.");
        if (prices.Any(p => p.SalePrice < 0 || p.MinSalePrice < 0))
            throw new InvalidOperationException("الأسعار لا يمكن أن تكون سالبة.");
        if (prices.Any(p => p.MinSalePrice > p.SalePrice))
            throw new InvalidOperationException("أقل سعر بيع لا يمكن أن يتجاوز سعر البيع.");

        var availableUnitIds = units.Select(u => u.UnitId).Append(request.BaseUnitId).ToHashSet();
        if (prices.Any(p => !availableUnitIds.Contains(p.UnitId)))
            throw new InvalidOperationException("يوجد سعر مرتبط بوحدة غير مضافة للمنتج.");
        if (request.Barcodes.Any(b => b.UnitId.HasValue && !availableUnitIds.Contains(b.UnitId.Value)))
            throw new InvalidOperationException("يوجد باركود مرتبط بوحدة غير مضافة للمنتج.");
    }

    private static void SynchronizeUnits(Product product, ProductSaveRequest request)
    {
        var inputs = request.Units.ToList();
        if (inputs.All(u => u.UnitId != request.BaseUnitId))
        {
            inputs.Add(new ProductUnitInput(
                null,
                request.BaseUnitId,
                1,
                inputs.All(u => !u.IsDefaultPurchase),
                inputs.All(u => !u.IsDefaultSale)));
        }

        var defaultPurchaseUnitId = inputs.FirstOrDefault(u => u.IsDefaultPurchase)?.UnitId
            ?? request.BaseUnitId;
        var defaultSaleUnitId = inputs.FirstOrDefault(u => u.IsDefaultSale)?.UnitId
            ?? request.BaseUnitId;
        var requestedUnitIds = inputs.Select(i => i.UnitId).ToHashSet();

        foreach (var existing in product.ProductUnits)
        {
            if (!requestedUnitIds.Contains(existing.UnitId))
            {
                existing.IsActive = false;
                existing.IsDefaultPurchase = false;
                existing.IsDefaultSale = false;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        foreach (var input in inputs)
        {
            var unit = product.ProductUnits.SingleOrDefault(u => u.UnitId == input.UnitId);
            if (unit is null)
            {
                unit = new ProductUnit
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    UnitId = input.UnitId,
                    CreatedAt = DateTime.UtcNow
                };
                product.ProductUnits.Add(unit);
            }

            unit.ConversionFactorToBase = input.UnitId == request.BaseUnitId
                ? 1
                : input.ConversionFactorToBase;
            unit.IsDefaultPurchase = input.UnitId == defaultPurchaseUnitId;
            unit.IsDefaultSale = input.UnitId == defaultSaleUnitId;
            unit.IsActive = true;
            unit.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void SynchronizePrices(Product product, ProductSaveRequest request, AppDbContext db)
    {
        var activeUnits = product.ProductUnits.Where(u => u.IsActive).ToDictionary(u => u.UnitId);
        var requestedKeys = request.Prices
            .Select(p => (activeUnits[p.UnitId].Id, p.PriceGroupId))
            .ToHashSet();
        var removed = product.ProductPrices
            .Where(p => !requestedKeys.Contains((p.ProductUnitId, p.PriceGroupId)))
            .ToList();
        db.ProductPrices.RemoveRange(removed);

        foreach (var input in request.Prices)
        {
            var productUnit = activeUnits[input.UnitId];
            var price = product.ProductPrices.SingleOrDefault(
                p => p.ProductUnitId == productUnit.Id && p.PriceGroupId == input.PriceGroupId);
            if (price is null)
            {
                price = new ProductPrice
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductUnitId = productUnit.Id,
                    PriceGroupId = input.PriceGroupId,
                    CreatedAt = DateTime.UtcNow
                };
                product.ProductPrices.Add(price);
            }

            price.SalePrice = input.SalePrice;
            price.MinSalePrice = input.MinSalePrice;
            price.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static void SynchronizeBarcodes(Product product, ProductSaveRequest request, AppDbContext db)
    {
        var activeUnits = product.ProductUnits.Where(u => u.IsActive).ToDictionary(u => u.UnitId);
        var requested = request.Barcodes
            .Where(b => !string.IsNullOrWhiteSpace(b.Barcode))
            .Select(b => new
            {
                Input = b,
                Barcode = b.Barcode.Trim(),
                ProductUnitId = b.UnitId.HasValue ? activeUnits[b.UnitId.Value].Id : (Guid?)null
            })
            .ToList();
        var requestedValues = requested.Select(x => x.Barcode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removed = product.ProductBarcodes
            .Where(b => !requestedValues.Contains(b.Barcode))
            .ToList();
        db.ProductBarcodes.RemoveRange(removed);

        foreach (var item in requested)
        {
            var barcode = product.ProductBarcodes.SingleOrDefault(
                b => string.Equals(b.Barcode, item.Barcode, StringComparison.OrdinalIgnoreCase));
            if (barcode is null)
            {
                barcode = new ProductBarcode
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    CreatedAt = DateTime.UtcNow
                };
                product.ProductBarcodes.Add(barcode);
            }

            barcode.Barcode = item.Barcode;
            barcode.ProductUnitId = item.ProductUnitId;
        }
    }

    private static string GetMatchSource(Product product, string term)
    {
        if (product.ProductSerials.Any(s => string.Equals(s.SerialNumber, term, StringComparison.OrdinalIgnoreCase)))
            return "سيريال";
        if (product.ProductBarcodes.Any(b => string.Equals(b.Barcode, term, StringComparison.OrdinalIgnoreCase)))
            return "باركود";
        if (string.Equals(product.ProductCode, term, StringComparison.OrdinalIgnoreCase))
            return "كود";
        return "اسم / بحث جزئي";
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
