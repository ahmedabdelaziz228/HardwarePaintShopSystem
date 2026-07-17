namespace HardwarePaintShop.Application.Models;

public sealed record PartySearchCriteria(
    string? Query = null,
    bool IncludeInactive = false);

public sealed record CustomerListItem(
    Guid Id,
    string Name,
    string? Phone,
    string? CustomerType,
    string? PriceGroupName,
    decimal CreditLimit,
    decimal CurrentBalance,
    bool IsActive);

public sealed class CustomerDetails
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? CustomerType { get; init; }
    public Guid? PriceGroupId { get; init; }
    public decimal CreditLimit { get; init; }
    public decimal CurrentBalance { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CustomerSaveRequest
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? CustomerType { get; init; }
    public Guid? PriceGroupId { get; init; }
    public decimal CreditLimit { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record SupplierListItem(
    Guid Id,
    string Name,
    string? Phone,
    string? Address,
    decimal CurrentBalance,
    bool IsActive);

public sealed class SupplierDetails
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public decimal CurrentBalance { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}

public sealed class SupplierSaveRequest
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}
