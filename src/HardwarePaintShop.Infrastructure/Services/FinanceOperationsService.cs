using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Domain.Enums;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class FinanceOperationsService : IFinanceOperationsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;

    public FinanceOperationsService(IDbContextFactory<AppDbContext> dbFactory, IAuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public async Task<List<CashMovementListItem>> GetMovementsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.CashMovements.AsNoTracking().AsQueryable();
        if (from.HasValue)
            query = query.Where(m => m.CreatedAt >= ToUtcStart(from.Value));
        if (to.HasValue)
            query = query.Where(m => m.CreatedAt < ToUtcStart(to.Value).AddDays(1));

        return await query.OrderByDescending(m => m.CreatedAt).Take(1000)
            .Select(m => new CashMovementListItem(
                m.Id, m.CreatedAt, m.Cashbox.Name, m.MovementType.ToString(),
                m.Direction.ToString(), m.Amount, m.ReferenceType, m.Notes,
                m.User != null ? m.User.FullName : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExpenseListItem>> GetExpensesAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= ToUtcStart(from.Value));
        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate < ToUtcStart(to.Value).AddDays(1));

        return await query.OrderByDescending(e => e.ExpenseDate).Take(1000)
            .Select(e => new ExpenseListItem(
                e.Id, e.ExpenseDate, e.ExpenseCategory.Name, e.Cashbox.Name, e.Amount, e.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task ExecuteAsync(
        FinanceOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CashboxId == Guid.Empty)
            throw new InvalidOperationException("اختر الخزينة.");
        if (request.Amount <= 0)
            throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var source = await db.Cashboxes.SingleOrDefaultAsync(
            c => c.Id == request.CashboxId && c.IsActive,
            cancellationToken) ?? throw new InvalidOperationException("الخزينة غير موجودة أو غير نشطة.");
        var referenceId = Guid.NewGuid();
        var operationDate = ToUtcDateTime(request.OperationDate);
        var notes = Normalize(request.Notes);

        switch (request.OperationType)
        {
            case FinanceOperationType.Expense:
                await RecordExpenseAsync(db, source, request, referenceId, operationDate, notes, cancellationToken);
                break;
            case FinanceOperationType.CustomerCollection:
                await RecordCustomerCollectionAsync(db, source, request, referenceId, operationDate, notes, cancellationToken);
                break;
            case FinanceOperationType.SupplierPayment:
                await RecordSupplierPaymentAsync(db, source, request, referenceId, operationDate, notes, cancellationToken);
                break;
            case FinanceOperationType.OwnerDeposit:
                AddCashMovement(db, source, CashMovementType.OwnerDeposit, CashDirection.In,
                    request.Amount, "OwnerDeposit", referenceId, operationDate, notes);
                source.CurrentBalance += request.Amount;
                break;
            case FinanceOperationType.OwnerWithdrawal:
                EnsureSufficient(source, request.Amount);
                await db.OwnerWithdrawals.AddAsync(new OwnerWithdrawal
                {
                    Id = referenceId, CashboxId = source.Id, Amount = request.Amount,
                    WithdrawalDate = operationDate, UserId = _authService.CurrentUserId,
                    Notes = notes, CreatedAt = DateTime.UtcNow
                }, cancellationToken);
                AddCashMovement(db, source, CashMovementType.OwnerWithdrawal, CashDirection.Out,
                    request.Amount, "OwnerWithdrawal", referenceId, operationDate, notes);
                source.CurrentBalance -= request.Amount;
                break;
            case FinanceOperationType.CashTransfer:
                await RecordTransferAsync(db, source, request, referenceId, operationDate, notes, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("نوع الحركة غير مدعوم.");
        }

        source.UpdatedAt = DateTime.UtcNow;
        await db.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), TableName = "CashMovements", RecordId = referenceId,
            Action = AuditAction.Insert, ChangedByUserId = _authService.CurrentUserId,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Operation = request.OperationType.ToString(),
                request.Amount
            }),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RecordExpenseAsync(
        AppDbContext db, Cashbox cashbox, FinanceOperationRequest request, Guid referenceId,
        DateTime operationDate, string? notes, CancellationToken cancellationToken)
    {
        if (!request.ExpenseCategoryId.HasValue || !await db.ExpenseCategories.AnyAsync(
                c => c.Id == request.ExpenseCategoryId.Value && c.IsActive, cancellationToken))
            throw new InvalidOperationException("اختر نوع مصروف نشط.");
        EnsureSufficient(cashbox, request.Amount);
        await db.Expenses.AddAsync(new Expense
        {
            Id = referenceId, ExpenseCategoryId = request.ExpenseCategoryId.Value,
            CashboxId = cashbox.Id, Amount = request.Amount, ExpenseDate = operationDate,
            UserId = _authService.CurrentUserId, Notes = notes, CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        AddCashMovement(db, cashbox, CashMovementType.Expense, CashDirection.Out,
            request.Amount, "Expense", referenceId, operationDate, notes);
        cashbox.CurrentBalance -= request.Amount;
    }

    private async Task RecordCustomerCollectionAsync(
        AppDbContext db, Cashbox cashbox, FinanceOperationRequest request, Guid referenceId,
        DateTime operationDate, string? notes, CancellationToken cancellationToken)
    {
        if (!request.CustomerId.HasValue)
            throw new InvalidOperationException("اختر العميل.");
        var customer = await db.Customers.SingleOrDefaultAsync(
            c => c.Id == request.CustomerId.Value && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("العميل غير موجود أو غير نشط.");
        if (request.Amount > customer.CurrentBalance)
            throw new InvalidOperationException("المبلغ أكبر من مديونية العميل الحالية.");
        await db.CustomerTransactions.AddAsync(new CustomerTransaction
        {
            Id = referenceId, CustomerId = customer.Id, TransactionType = "CustomerCollection",
            Direction = "credit", Amount = request.Amount, ReferenceType = "CollectionVoucher",
            ReferenceId = referenceId, UserId = _authService.CurrentUserId,
            Notes = notes, CreatedAt = operationDate
        }, cancellationToken);
        AddCashMovement(db, cashbox, CashMovementType.CustomerCollection, CashDirection.In,
            request.Amount, "CollectionVoucher", referenceId, operationDate, notes);
        customer.CurrentBalance -= request.Amount;
        customer.UpdatedAt = DateTime.UtcNow;
        cashbox.CurrentBalance += request.Amount;
    }

    private async Task RecordSupplierPaymentAsync(
        AppDbContext db, Cashbox cashbox, FinanceOperationRequest request, Guid referenceId,
        DateTime operationDate, string? notes, CancellationToken cancellationToken)
    {
        if (!request.SupplierId.HasValue)
            throw new InvalidOperationException("اختر المورد.");
        EnsureSufficient(cashbox, request.Amount);
        var supplier = await db.Suppliers.SingleOrDefaultAsync(
            s => s.Id == request.SupplierId.Value && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("المورد غير موجود أو غير نشط.");
        if (request.Amount > supplier.CurrentBalance)
            throw new InvalidOperationException("المبلغ أكبر من مستحقات المورد الحالية.");
        await db.SupplierTransactions.AddAsync(new SupplierTransaction
        {
            Id = referenceId, SupplierId = supplier.Id, TransactionType = "SupplierPayment",
            Direction = "debit", Amount = request.Amount, ReferenceType = "PaymentVoucher",
            ReferenceId = referenceId, UserId = _authService.CurrentUserId,
            Notes = notes, CreatedAt = operationDate
        }, cancellationToken);
        AddCashMovement(db, cashbox, CashMovementType.SupplierPayment, CashDirection.Out,
            request.Amount, "PaymentVoucher", referenceId, operationDate, notes);
        supplier.CurrentBalance -= request.Amount;
        supplier.UpdatedAt = DateTime.UtcNow;
        cashbox.CurrentBalance -= request.Amount;
    }

    private async Task RecordTransferAsync(
        AppDbContext db, Cashbox source, FinanceOperationRequest request, Guid referenceId,
        DateTime operationDate, string? notes, CancellationToken cancellationToken)
    {
        if (!request.DestinationCashboxId.HasValue || request.DestinationCashboxId == source.Id)
            throw new InvalidOperationException("اختر خزينة وجهة مختلفة.");
        EnsureSufficient(source, request.Amount);
        var destination = await db.Cashboxes.SingleOrDefaultAsync(
            c => c.Id == request.DestinationCashboxId.Value && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("خزينة الوجهة غير موجودة أو غير نشطة.");
        AddCashMovement(db, source, CashMovementType.CashTransferOut, CashDirection.Out,
            request.Amount, "CashTransfer", referenceId, operationDate, notes);
        AddCashMovement(db, destination, CashMovementType.CashTransferIn, CashDirection.In,
            request.Amount, "CashTransfer", referenceId, operationDate, notes);
        source.CurrentBalance -= request.Amount;
        destination.CurrentBalance += request.Amount;
        destination.UpdatedAt = DateTime.UtcNow;
    }

    private void AddCashMovement(
        AppDbContext db, Cashbox cashbox, CashMovementType movementType, CashDirection direction,
        decimal amount, string referenceType, Guid referenceId, DateTime date, string? notes)
        => db.CashMovements.Add(new CashMovement
        {
            Id = Guid.NewGuid(), CashboxId = cashbox.Id, MovementType = movementType,
            Direction = direction, Amount = amount, ReferenceType = referenceType,
            ReferenceId = referenceId, UserId = _authService.CurrentUserId,
            Notes = notes, CreatedAt = date
        });

    private static void EnsureSufficient(Cashbox cashbox, decimal amount)
    {
        if (cashbox.CurrentBalance < amount)
            throw new InvalidOperationException($"رصيد خزينة '{cashbox.Name}' غير كافٍ.");
    }

    private static DateTime ToUtcStart(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static DateTime ToUtcDateTime(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
