namespace HardwarePaintShop.Application.Models;

public sealed record ProductSearchCriteria(
    string? Query = null,
    Guid? CategoryId = null,
    bool IncludeInactive = false,
    Guid? SupplierId = null);

public sealed record ProductListItem(
    Guid Id,
    string? ProductCode,
    string Name,
    string? CategoryName,
    string BaseUnitName,
    decimal StockBaseQuantity,
    bool IsSerialTracked,
    bool IsActive,
    string? PrimaryBarcode,
    string? MainSupplierName = null);

public sealed class ProductDetails
{
    public Guid Id { get; init; }
    public string? ProductCode { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public Guid? MainSupplierId { get; init; }
    public Guid BaseUnitId { get; init; }
    public string? ImagePath { get; init; }
    public decimal MinStockBaseQuantity { get; init; }
    public bool IsSerialTracked { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
    public List<ProductUnitData> Units { get; init; } = new();
    public List<ProductPriceData> Prices { get; init; } = new();
    public List<ProductBarcodeData> Barcodes { get; init; } = new();
}

public sealed record ProductUnitData(
    Guid Id,
    Guid UnitId,
    string UnitName,
    decimal ConversionFactorToBase,
    bool IsDefaultPurchase,
    bool IsDefaultSale,
    bool IsActive);

public sealed record ProductPriceData(
    Guid Id,
    Guid UnitId,
    Guid PriceGroupId,
    decimal SalePrice,
    decimal MinSalePrice);

public sealed record ProductBarcodeData(
    Guid Id,
    Guid? UnitId,
    string Barcode);

public sealed class ProductSaveRequest
{
    public Guid? Id { get; init; }
    public string? ProductCode { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public Guid? MainSupplierId { get; init; }
    public Guid BaseUnitId { get; init; }
    public string? ImagePath { get; init; }
    public decimal MinStockBaseQuantity { get; init; }
    public bool IsSerialTracked { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Notes { get; init; }
    public IReadOnlyCollection<ProductUnitInput> Units { get; init; } = Array.Empty<ProductUnitInput>();
    public IReadOnlyCollection<ProductPriceInput> Prices { get; init; } = Array.Empty<ProductPriceInput>();
    public IReadOnlyCollection<ProductBarcodeInput> Barcodes { get; init; } = Array.Empty<ProductBarcodeInput>();
}

public sealed record ProductUnitInput(
    Guid? Id,
    Guid UnitId,
    decimal ConversionFactorToBase,
    bool IsDefaultPurchase,
    bool IsDefaultSale);

public sealed record ProductPriceInput(
    Guid? Id,
    Guid UnitId,
    Guid PriceGroupId,
    decimal SalePrice,
    decimal MinSalePrice);

public sealed record ProductBarcodeInput(
    Guid? Id,
    Guid? UnitId,
    string Barcode);

public sealed record PriceInquiryResult(
    Guid ProductId,
    string? ProductCode,
    string ProductName,
    string UnitName,
    decimal ConversionFactorToBase,
    string PriceGroupName,
    decimal? SalePrice,
    decimal? MinSalePrice,
    decimal StockBaseQuantity,
    string? Barcode,
    bool IsSerialTracked,
    string MatchSource,
    string? CategoryName = null,
    string? MainSupplierName = null,
    string? ImagePath = null,
    string? StockDisplay = null,
    decimal? LastPurchasePriceBaseUnit = null);
