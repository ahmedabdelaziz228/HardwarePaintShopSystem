using HardwarePaintShop.Domain.Enums;

namespace HardwarePaintShop.Domain.Entities;

/// <summary>
/// A product sold/purchased in the shop.
/// Stock quantity is NEVER stored here — always computed from StockMovements.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string? ProductCode { get; set; }
    public string Name { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>The base unit of measure (e.g. kg). All stock quantities are stored in this unit.</summary>
    public Guid BaseUnitId { get; set; }
    public Unit BaseUnit { get; set; } = null!;

    public Guid? MainSupplierId { get; set; }
    public Supplier? MainSupplier { get; set; }

    public string? ImagePath { get; set; }

    /// <summary>Minimum stock level (in base unit) before a low-stock alert is triggered.</summary>
    public decimal MinStockBaseQuantity { get; set; }

    /// <summary>Whether this product tracks individual serial numbers.</summary>
    public bool IsSerialTracked { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductUnit>    ProductUnits    { get; set; } = new List<ProductUnit>();
    public ICollection<ProductPrice>   ProductPrices   { get; set; } = new List<ProductPrice>();
    public ICollection<ProductBarcode> ProductBarcodes { get; set; } = new List<ProductBarcode>();
    public ICollection<ProductSerial>  ProductSerials  { get; set; } = new List<ProductSerial>();
    public ICollection<StockMovement>  StockMovements  { get; set; } = new List<StockMovement>();

    /// <summary>Single cost record per product (one-to-one).</summary>
    public ProductCost? ProductCost { get; set; }
}

/// <summary>
/// A selling/purchasing unit for a product with a conversion factor to the base unit.
/// Example: base=kg, ProductUnit=bag with factor=25 means 1 bag = 25 kg.
/// </summary>
public class ProductUnit
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    /// <summary>
    /// How many base units does 1 of this unit equal?
    /// e.g. 1 bag = 25 kg → ConversionFactorToBase = 25.
    /// Always > 0. Base unit itself has factor = 1.
    /// </summary>
    public decimal ConversionFactorToBase { get; set; } = 1;

    public bool IsDefaultPurchase { get; set; }
    public bool IsDefaultSale { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sale price for a product unit in a price group.
/// </summary>
public class ProductPrice
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid ProductUnitId { get; set; }
    public ProductUnit ProductUnit { get; set; } = null!;
    public Guid PriceGroupId { get; set; }
    public PriceGroup PriceGroup { get; set; } = null!;
    public decimal SalePrice { get; set; }
    public decimal MinSalePrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tracks the latest and average cost of a product (in base unit).
/// One record per product — updated on each purchase.
/// </summary>
public class ProductCost
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Last purchase price expressed in base unit terms.</summary>
    public decimal LastPurchasePriceBaseUnit { get; set; }

    /// <summary>Weighted average cost expressed in base unit terms.</summary>
    public decimal AverageCostBaseUnit { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Barcode associated with a product or specific product unit.
/// </summary>
public class ProductBarcode
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Null means the barcode is for the base product regardless of unit.</summary>
    public Guid? ProductUnitId { get; set; }
    public ProductUnit? ProductUnit { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual serialised unit of a product.
/// SerialNumber must be globally unique across all products.
/// </summary>
public class ProductSerial
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string SerialNumber { get; set; } = string.Empty;
    public SerialStatus Status { get; set; } = SerialStatus.Available;

    public Guid? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public Guid? SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SoldAt { get; set; }
}
