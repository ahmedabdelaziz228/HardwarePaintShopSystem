namespace HardwarePaintShop.Application.Models;

public sealed record CashboxListItem(
    Guid Id,
    string Name,
    decimal OpeningBalance,
    decimal CurrentBalance,
    bool IsActive,
    DateTime CreatedAt);

public sealed class CashboxSaveRequest
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal OpeningBalance { get; init; }
    public bool IsActive { get; init; } = true;
}
