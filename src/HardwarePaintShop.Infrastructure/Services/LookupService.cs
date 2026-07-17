using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public class LookupService : ILookupService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public LookupService(IDbContextFactory<AppDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<List<Category>> GetCategoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Categories
            .Where(c => c.IsActive)
            .Include(c => c.Children)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category> AddCategoryAsync(string name, Guid? parentId, string? notes)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentCategoryId = parentId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Categories.AddAsync(category);
        await db.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(Guid id, string name, Guid? parentId, string? notes)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var category = await db.Categories.FindAsync(id) ?? throw new KeyNotFoundException();
        category.Name = name;
        category.ParentCategoryId = parentId;
        category.Notes = notes;
        category.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeactivateCategoryAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var category = await db.Categories.FindAsync(id) ?? throw new KeyNotFoundException();
        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<Unit>> GetUnitsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Units.Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<Unit> AddUnitAsync(string name, string? symbol, string? notes)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Name = name,
            ShortName = symbol,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Units.AddAsync(unit);
        await db.SaveChangesAsync();
        return unit;
    }

    public async Task UpdateUnitAsync(Guid id, string name, string? symbol, string? notes)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var unit = await db.Units.FindAsync(id) ?? throw new KeyNotFoundException();
        unit.Name = name;
        unit.ShortName = symbol;
        unit.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeactivateUnitAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var unit = await db.Units.FindAsync(id) ?? throw new KeyNotFoundException();
        unit.IsActive = false;
        unit.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<PriceGroup>> GetPriceGroupsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PriceGroups.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<PriceGroup> AddPriceGroupAsync(string name, string? description, bool isDefault)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (isDefault)
            await db.PriceGroups.Where(pg => pg.IsDefault).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));

        var priceGroup = new PriceGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.PriceGroups.AddAsync(priceGroup);
        await db.SaveChangesAsync();
        return priceGroup;
    }

    public async Task UpdatePriceGroupAsync(Guid id, string name, string? description, bool isDefault)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (isDefault)
        {
            await db.PriceGroups
                .Where(pg => pg.IsDefault && pg.Id != id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
        }

        var priceGroup = await db.PriceGroups.FindAsync(id) ?? throw new KeyNotFoundException();
        priceGroup.Name = name;
        priceGroup.Description = description;
        priceGroup.IsDefault = isDefault;
        priceGroup.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeactivatePriceGroupAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var priceGroup = await db.PriceGroups.FindAsync(id) ?? throw new KeyNotFoundException();
        priceGroup.IsActive = false;
        priceGroup.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ExpenseCategories.Where(e => e.IsActive).OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<ExpenseCategory> AddExpenseCategoryAsync(string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.ExpenseCategories.AddAsync(category);
        await db.SaveChangesAsync();
        return category;
    }

    public async Task UpdateExpenseCategoryAsync(Guid id, string name)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var category = await db.ExpenseCategories.FindAsync(id) ?? throw new KeyNotFoundException();
        category.Name = name;
        category.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeactivateExpenseCategoryAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var category = await db.ExpenseCategories.FindAsync(id) ?? throw new KeyNotFoundException();
        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
