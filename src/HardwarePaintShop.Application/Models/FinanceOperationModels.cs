namespace HardwarePaintShop.Application.Models;

public enum FinanceOperationType
{
    Expense,
    CustomerCollection,
    SupplierPayment,
    OwnerDeposit,
    OwnerWithdrawal,
    CashTransfer
}

public sealed record FinanceOperationOption(FinanceOperationType Value, string Name);

public sealed class FinanceOperationRequest
{
    public FinanceOperationType OperationType { get; init; }
    public Guid CashboxId { get; init; }
    public Guid? DestinationCashboxId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? ExpenseCategoryId { get; init; }
    public decimal Amount { get; init; }
    public DateTime OperationDate { get; init; }
    public string? Notes { get; init; }
}

public sealed record CashMovementListItem(
    Guid Id,
    DateTime CreatedAt,
    string CashboxName,
    string MovementType,
    string Direction,
    decimal Amount,
    string? ReferenceType,
    string? Notes,
    string? UserName);

public sealed record ExpenseListItem(
    Guid Id,
    DateTime ExpenseDate,
    string CategoryName,
    string CashboxName,
    decimal Amount,
    string? Notes);
