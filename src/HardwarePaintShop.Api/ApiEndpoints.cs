using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace HardwarePaintShop.Api;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapHardwarePaintShopApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.MapPost("/auth/login", LoginAsync);
        api.MapPost("/auth/logout", LogoutAsync);
        api.MapGet("/auth/me", (HttpContext http) => Results.Ok(User(http)));
        api.MapGet("/dashboard", DashboardAsync);
        api.MapGet("/lookups", LookupsAsync);
        api.MapGet("/settings/mobile", MobileSettingsAsync);
        api.MapGet("/products/search", SearchProductsAsync);
        api.MapGet("/products/barcode/{barcode}", ProductByBarcodeAsync);
        api.MapGet("/products/serial/{serial}", ProductBySerialAsync);
        api.MapGet("/products/{id:guid}", ProductDetailsAsync);
        api.MapGet("/products/{id:guid}/image", ProductImageAsync);
        api.MapPost("/products", CreateProductAsync);
        api.MapGet("/customers/search", SearchCustomersAsync);
        api.MapGet("/customers/{id:guid}/statement", CustomerStatementAsync);
        api.MapPost("/customers/{id:guid}/payments", CustomerPaymentAsync);
        api.MapGet("/alerts", AlertsAsync);
        api.MapPost("/alerts/{id:guid}/read", MarkAlertReadAsync);
        api.MapPost("/stock/adjustment", StockAdjustmentAsync);
        api.MapGet("/sync/download", SyncDownloadAsync);
        api.MapPost("/sync/upload", SyncUploadAsync);
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, ApiSessionService sessions, CancellationToken token)
        => Results.Ok(await sessions.LoginAsync(request, token));

    private static async Task<IResult> LogoutAsync(
        HttpContext http, ApiSessionService sessions, CancellationToken token)
    {
        await sessions.LogoutAsync(ApiSessionService.ReadBearerToken(http.Request), User(http), token);
        return Results.NoContent();
    }

    private static async Task<IResult> DashboardAsync(
        HttpContext http, IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Dashboard.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);
        var sales = await db.SalesInvoices.AsNoTracking().Where(i => i.InvoiceDate >= start && i.InvoiceDate < end &&
            i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Voided)
            .SumAsync(i => (decimal?)i.TotalAmount, token) ?? 0;
        var collected = await db.CashMovements.AsNoTracking().Where(m => m.CreatedAt >= start && m.CreatedAt < end &&
            m.Direction == CashDirection.In).SumAsync(m => (decimal?)m.Amount, token) ?? 0;
        var expenses = await db.Expenses.AsNoTracking().Where(e => e.ExpenseDate >= start && e.ExpenseDate < end)
            .SumAsync(e => (decimal?)e.Amount, token) ?? 0;
        var cash = await db.Cashboxes.AsNoTracking().Where(c => c.IsActive)
            .SumAsync(c => (decimal?)c.CurrentBalance, token) ?? 0;
        var customerDebt = await db.Customers.AsNoTracking().Where(c => c.IsActive)
            .SumAsync(c => (decimal?)c.CurrentBalance, token) ?? 0;
        var lowStock = await db.Products.AsNoTracking().Where(p => p.IsActive)
            .CountAsync(p => (p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0)
                <= p.MinStockBaseQuantity, token);
        var unread = await VisibleAlerts(db, User(http)).CountAsync(a => !a.IsRead, token);
        return Results.Ok(new
        {
            salesToday = sales,
            collectedToday = collected,
            expensesToday = expenses,
            cashBalance = cash,
            customerDebt,
            lowStockCount = lowStock,
            unreadAlerts = unread,
            serverTime = DateTime.UtcNow
        });
    }

    private static async Task<IResult> LookupsAsync(
        HttpContext http, IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Api.Access");
        await using var db = await factory.CreateDbContextAsync(token);
        var units = await db.Units.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.ShortName }).ToListAsync(token);
        var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name }).ToListAsync(token);
        var priceGroups = await db.PriceGroups.AsNoTracking().Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.IsDefault }).ToListAsync(token);
        var suppliers = await db.Suppliers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name }).ToListAsync(token);
        var cashboxes = await db.Cashboxes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.CurrentBalance }).ToListAsync(token);
        return Results.Ok(new { units, categories, priceGroups, suppliers, cashboxes });
    }

    private static async Task<IResult> MobileSettingsAsync(
        HttpContext http, IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Api.Access");
        await using var db = await factory.CreateDbContextAsync(token);
        var values = await db.AppSettings.AsNoTracking().Where(s => s.Key.StartsWith("Shop."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, token);
        return Results.Ok(new
        {
            shopName = values.GetValueOrDefault("Shop.Name") ?? "محل الحدايد والبوهيات",
            phone = values.GetValueOrDefault("Shop.Phone") ?? string.Empty,
            address = values.GetValueOrDefault("Shop.Address") ?? string.Empty,
            taxNumber = values.GetValueOrDefault("Shop.TaxNumber") ?? string.Empty
        });
    }

    private static async Task<IResult> SearchProductsAsync(
        HttpContext http, string? query, int? limit,
        IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        RequireAny(http, "Product.View", "PriceInquiry.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var products = db.Products.AsNoTracking().Where(p => p.IsActive);
        var term = query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            products = products.Where(p => EF.Functions.ILike(p.Name, pattern) ||
                (p.ProductCode != null && EF.Functions.ILike(p.ProductCode, pattern)) ||
                p.ProductBarcodes.Any(b => EF.Functions.ILike(b.Barcode, pattern)) ||
                p.ProductSerials.Any(s => EF.Functions.ILike(s.SerialNumber, pattern)));
        }
        var result = await products.OrderBy(p => p.Name).Take(Math.Clamp(limit ?? 60, 1, 200))
            .Select(p => new
            {
                p.Id,
                p.ProductCode,
                p.Name,
                category = p.Category != null ? p.Category.Name : null,
                baseUnit = p.BaseUnit.Name,
                stockBase = p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0,
                p.MinStockBaseQuantity,
                p.IsSerialTracked,
                hasImage = p.ImagePath != null,
                defaultPrice = p.ProductPrices.OrderByDescending(x => x.PriceGroup.IsDefault)
                    .ThenBy(x => x.SalePrice).Select(x => (decimal?)x.SalePrice).FirstOrDefault(),
                p.UpdatedAt
            }).ToListAsync(token);
        return Results.Ok(result);
    }

    private static Task<IResult> ProductByBarcodeAsync(
        HttpContext http, string barcode, IDbContextFactory<AppDbContext> factory, CancellationToken token)
        => FindSingleProductAsync(http, factory, token, barcode, null);

    private static Task<IResult> ProductBySerialAsync(
        HttpContext http, string serial, IDbContextFactory<AppDbContext> factory, CancellationToken token)
        => FindSingleProductAsync(http, factory, token, null, serial);

    private static async Task<IResult> FindSingleProductAsync(
        HttpContext http, IDbContextFactory<AppDbContext> factory, CancellationToken token,
        string? barcode, string? serial)
    {
        RequireAny(http, "Product.View", "PriceInquiry.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var id = barcode is not null
            ? await db.ProductBarcodes.AsNoTracking().Where(b => b.Barcode == barcode.Trim())
                .Select(b => (Guid?)b.ProductId).FirstOrDefaultAsync(token)
            : await db.ProductSerials.AsNoTracking().Where(s => s.SerialNumber == serial!.Trim())
                .Select(s => (Guid?)s.ProductId).FirstOrDefaultAsync(token);
        return id.HasValue
            ? await ProductDetailsCoreAsync(http, id.Value, db, token)
            : Results.NotFound(new { error = "لم يتم العثور على المنتج." });
    }

    private static async Task<IResult> ProductDetailsAsync(
        HttpContext http, Guid id, IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        return await ProductDetailsCoreAsync(http, id, db, token);
    }

    private static async Task<IResult> ProductDetailsCoreAsync(
        HttpContext http, Guid id, AppDbContext db, CancellationToken token)
    {
        RequireAny(http, "Product.View", "PriceInquiry.View");
        var product = await db.Products.AsNoTracking().Where(p => p.Id == id && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.ProductCode,
                p.Name,
                p.CategoryId,
                category = p.Category != null ? p.Category.Name : null,
                p.BaseUnitId,
                baseUnit = p.BaseUnit.Name,
                p.MainSupplierId,
                mainSupplier = p.MainSupplier != null ? p.MainSupplier.Name : null,
                p.MinStockBaseQuantity,
                p.IsSerialTracked,
                p.Notes,
                stockBase = p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0,
                hasImage = p.ImagePath != null,
                p.UpdatedAt,
                units = p.ProductUnits.Where(u => u.IsActive).OrderByDescending(u => u.IsDefaultSale)
                    .Select(u => new
                    {
                        u.Id, u.UnitId, unitName = u.Unit.Name, u.Unit.ShortName,
                        u.ConversionFactorToBase, u.IsDefaultSale, u.IsDefaultPurchase
                    }).ToList(),
                prices = p.ProductPrices.OrderByDescending(x => x.PriceGroup.IsDefault)
                    .ThenBy(x => x.PriceGroup.Name).Select(x => new
                    {
                        x.Id, x.ProductUnitId, x.PriceGroupId, priceGroup = x.PriceGroup.Name,
                        x.SalePrice, x.MinSalePrice
                    }).ToList(),
                barcodes = p.ProductBarcodes.Select(b => new { b.Id, b.Barcode, b.ProductUnitId }).ToList(),
                availableSerials = p.ProductSerials.Count(s => s.Status == SerialStatus.Available)
            }).SingleOrDefaultAsync(token);
        return product is null
            ? Results.NotFound(new { error = "المنتج غير موجود." })
            : Results.Ok(product);
    }

    private static async Task<IResult> ProductImageAsync(
        HttpContext http, Guid id, IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        RequireAny(http, "Product.View", "PriceInquiry.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var path = await db.Products.AsNoTracking().Where(p => p.Id == id && p.IsActive)
            .Select(p => p.ImagePath).SingleOrDefaultAsync(token);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return Results.NotFound();
        var contentType = Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? "image/png" : "image/jpeg";
        return Results.File(path, contentType, enableRangeProcessing: true);
    }

    private static async Task<IResult> CreateProductAsync(
        HttpContext http, ProductCreateRequest request, MobileCommandService commands, CancellationToken token)
        => Results.Created("/api/products",
            new { id = await commands.CreateProductAsync(User(http), request, null, token) });

    private static async Task<IResult> SearchCustomersAsync(
        HttpContext http, string? query, int? limit,
        IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Customer.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var customers = db.Customers.AsNoTracking().Where(c => c.IsActive);
        var term = query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            customers = customers.Where(c => EF.Functions.ILike(c.Name, pattern) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, pattern)));
        }
        var result = await customers.OrderBy(c => c.Name).Take(Math.Clamp(limit ?? 80, 1, 200))
            .Select(c => new
            {
                c.Id, c.Name, c.Phone, c.Address, c.CustomerType,
                c.CurrentBalance, c.CreditLimit, c.PriceGroupId,
                priceGroup = c.PriceGroup != null ? c.PriceGroup.Name : null,
                c.UpdatedAt
            }).ToListAsync(token);
        return Results.Ok(result);
    }

    private static async Task<IResult> CustomerStatementAsync(
        HttpContext http, Guid id, IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Customer.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var customer = await db.Customers.AsNoTracking().Where(c => c.Id == id).Select(c => new
        {
            c.Id, c.Name, c.Phone, c.Address, c.CurrentBalance, c.CreditLimit, c.IsActive
        }).SingleOrDefaultAsync(token);
        if (customer is null) return Results.NotFound(new { error = "العميل غير موجود." });
        var transactions = await db.CustomerTransactions.AsNoTracking().Where(t => t.CustomerId == id)
            .OrderByDescending(t => t.CreatedAt).Take(300).Select(t => new
            {
                t.Id, t.TransactionType, t.Direction, t.Amount,
                t.ReferenceType, t.ReferenceId, t.Notes, t.CreatedAt
            }).ToListAsync(token);
        var invoices = await db.SalesInvoices.AsNoTracking().Where(i => i.CustomerId == id)
            .OrderByDescending(i => i.InvoiceDate).Take(150).Select(i => new
            {
                i.Id, i.InvoiceNo, i.InvoiceDate, i.DueDate, i.TotalAmount,
                i.PaidAmount, i.RemainingAmount, i.PaymentStatus, i.Status
            }).ToListAsync(token);
        return Results.Ok(new { customer, transactions, invoices });
    }

    private static async Task<IResult> CustomerPaymentAsync(
        HttpContext http, Guid id, CustomerPaymentRequest request,
        MobileCommandService commands, CancellationToken token)
        => Results.Ok(new { id = await commands.CollectCustomerAsync(User(http), id, request, null, token) });

    private static async Task<IResult> AlertsAsync(
        HttpContext http, bool? unreadOnly,
        IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Alert.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var rows = VisibleAlerts(db, User(http));
        if (unreadOnly == true) rows = rows.Where(a => !a.IsRead);
        var result = await rows.OrderBy(a => a.IsRead).ThenByDescending(a => a.CreatedAt).Take(500)
            .Select(a => new
            {
                a.Id, a.AlertType, a.Title, a.Message, a.Severity, a.IsRead,
                a.ReferenceType, a.ReferenceId, a.CreatedAt, a.ReadAt
            }).ToListAsync(token);
        return Results.Ok(result);
    }

    private static async Task<IResult> MarkAlertReadAsync(
        HttpContext http, Guid id, IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Alert.View");
        await using var db = await factory.CreateDbContextAsync(token);
        var user = User(http);
        var alert = await db.Alerts.SingleOrDefaultAsync(a => a.Id == id &&
            (a.UserId == null || a.UserId == user.UserId) &&
            (a.RoleId == null || a.RoleId == user.RoleId), token);
        if (alert is null) return Results.NotFound();
        alert.IsRead = true;
        alert.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return Results.NoContent();
    }

    private static async Task<IResult> StockAdjustmentAsync(
        HttpContext http, StockAdjustmentRequest request,
        MobileCommandService commands, CancellationToken token)
        => Results.Ok(new { id = await commands.AdjustStockAsync(User(http), request, null, token) });

    private static async Task<IResult> SyncDownloadAsync(
        HttpContext http, DateTime? since,
        IDbContextFactory<AppDbContext> factory, CancellationToken token)
    {
        Require(http, "Sync.Download");
        var from = since?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-30);
        if (from < DateTime.UtcNow.AddYears(-2)) from = DateTime.UtcNow.AddYears(-2);
        await using var db = await factory.CreateDbContextAsync(token);
        var products = await db.Products.AsNoTracking().Where(p => p.UpdatedAt > from)
            .OrderBy(p => p.UpdatedAt).Take(5000).Select(p => new
            {
                p.Id, p.ProductCode, p.Name, p.CategoryId, p.BaseUnitId,
                p.MinStockBaseQuantity, p.IsSerialTracked, p.IsActive, p.UpdatedAt,
                category = p.Category != null ? p.Category.Name : null,
                baseUnit = p.BaseUnit.Name,
                stockBase = p.StockMovements.Select(m => (decimal?)m.QuantityBaseUnit).Sum() ?? 0,
                hasImage = p.ImagePath != null,
                defaultPrice = p.ProductPrices.OrderByDescending(x => x.PriceGroup.IsDefault)
                    .ThenBy(x => x.SalePrice).Select(x => (decimal?)x.SalePrice).FirstOrDefault()
            }).ToListAsync(token);
        var prices = await db.ProductPrices.AsNoTracking().Where(p => p.UpdatedAt > from)
            .OrderBy(p => p.UpdatedAt).Take(10000).Select(p => new
            {
                p.Id, p.ProductId, p.ProductUnitId, p.PriceGroupId,
                p.SalePrice, p.MinSalePrice, p.UpdatedAt
            }).ToListAsync(token);
        var barcodes = await db.ProductBarcodes.AsNoTracking().Where(b => b.CreatedAt > from)
            .OrderBy(b => b.CreatedAt).Take(10000).Select(b => new
            {
                b.Id, b.ProductId, b.ProductUnitId, b.Barcode, b.CreatedAt
            }).ToListAsync(token);
        var customers = await db.Customers.AsNoTracking().Where(c => c.UpdatedAt > from)
            .OrderBy(c => c.UpdatedAt).Take(5000).Select(c => new
            {
                c.Id, c.Name, c.Phone, c.Address, c.CustomerType, c.PriceGroupId,
                c.CreditLimit, c.CurrentBalance, c.IsActive, c.UpdatedAt
            }).ToListAsync(token);
        var alerts = await VisibleAlerts(db, User(http)).Where(a => a.CreatedAt > from)
            .OrderBy(a => a.CreatedAt).Take(2000).Select(a => new
            {
                a.Id, a.AlertType, a.Title, a.Message, a.Severity, a.IsRead,
                a.ReferenceType, a.ReferenceId, a.CreatedAt
            }).ToListAsync(token);
        var device = await db.SyncDevices.SingleAsync(d => d.Id == User(http).DeviceId, token);
        device.LastSyncAt = DateTime.UtcNow;
        db.SyncLogs.Add(new SyncLog
        {
            Id = Guid.NewGuid(), DeviceId = device.Id, Direction = SyncDirection.Download,
            EntityName = "snapshot", RecordId = Guid.NewGuid(), Status = "success",
            Message = $"from={from:O}", CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(token);
        return Results.Ok(new { serverTime = DateTime.UtcNow, products, prices, barcodes, customers, alerts });
    }

    private static async Task<IResult> SyncUploadAsync(
        HttpContext http, SyncUploadRequest request,
        MobileCommandService commands, CancellationToken token)
        => Results.Ok(new
        {
            results = await commands.UploadAsync(User(http), request, token),
            serverTime = DateTime.UtcNow
        });

    private static IQueryable<Alert> VisibleAlerts(AppDbContext db, ApiUserContext user)
        => db.Alerts.Where(a =>
            (a.UserId == null || a.UserId == user.UserId) &&
            (a.RoleId == null || a.RoleId == user.RoleId));

    private static ApiUserContext User(HttpContext http)
        => ApiUserContext.From(http)
            ?? throw new ApiProblemException(401, "جلسة الدخول مطلوبة.");

    private static void Require(HttpContext http, string permission)
    {
        if (!User(http).Can(permission))
            throw new ApiProblemException(403, $"الصلاحية المطلوبة: {permission}");
    }

    private static void RequireAny(HttpContext http, params string[] permissions)
    {
        var user = User(http);
        if (!permissions.Any(user.Can))
            throw new ApiProblemException(403, "الحساب لا يملك صلاحية لهذه العملية.");
    }
}
