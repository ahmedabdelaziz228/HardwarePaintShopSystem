namespace HardwarePaintShop.Application.Interfaces;

/// <summary>
/// Generic repository interface for data access.
/// Provides basic CRUD operations; use Query() for custom queries.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Returns an IQueryable for composing custom queries with LINQ.</summary>
    IQueryable<T> Query();

    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> ListAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task SaveChangesAsync();
}
