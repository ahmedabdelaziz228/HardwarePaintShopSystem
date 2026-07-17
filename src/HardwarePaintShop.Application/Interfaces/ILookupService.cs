using HardwarePaintShop.Domain.Entities;

namespace HardwarePaintShop.Application.Interfaces;

public interface ILookupService
{
    // Categories
    Task<List<Category>> GetCategoriesAsync();
    Task<Category> AddCategoryAsync(string name, Guid? parentId, string? notes);
    Task UpdateCategoryAsync(Guid id, string name, Guid? parentId, string? notes);
    Task DeactivateCategoryAsync(Guid id);

    // Units
    Task<List<Unit>> GetUnitsAsync();
    Task<Unit> AddUnitAsync(string name, string? symbol, string? notes);
    Task UpdateUnitAsync(Guid id, string name, string? symbol, string? notes);
    Task DeactivateUnitAsync(Guid id);

    // Price Groups
    Task<List<PriceGroup>> GetPriceGroupsAsync();
    Task<PriceGroup> AddPriceGroupAsync(string name, string? description, bool isDefault);
    Task UpdatePriceGroupAsync(Guid id, string name, string? description, bool isDefault);
    Task DeactivatePriceGroupAsync(Guid id);

    // Expense Categories
    Task<List<ExpenseCategory>> GetExpenseCategoriesAsync();
    Task<ExpenseCategory> AddExpenseCategoryAsync(string name);
    Task UpdateExpenseCategoryAsync(Guid id, string name);
    Task DeactivateExpenseCategoryAsync(Guid id);
}
