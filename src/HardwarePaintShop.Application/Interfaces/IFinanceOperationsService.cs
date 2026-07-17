using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Application.Interfaces;

public interface IFinanceOperationsService
{
    Task<List<CashMovementListItem>> GetMovementsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<List<ExpenseListItem>> GetExpensesAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(
        FinanceOperationRequest request,
        CancellationToken cancellationToken = default);
}
