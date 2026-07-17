namespace HardwarePaintShop.Domain.Entities;

/// <summary>
/// Product category with optional parent for hierarchical categorization.
/// </summary>
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Parent category for sub-categorization (nullable for root categories).</summary>
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Unit of measure (e.g. kg, bag, piece, liter).
/// </summary>
public class Unit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Short abbreviation displayed on invoices (e.g. "kg", "pcs").</summary>
    public string? ShortName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Customer price tier (e.g. Retail, Wholesale, Contractor).
/// </summary>
public class PriceGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Category for shop expenses (e.g. Rent, Electricity, Labour).
/// </summary>
public class ExpenseCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System-wide key/value settings store.
/// Key is the primary key.
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
