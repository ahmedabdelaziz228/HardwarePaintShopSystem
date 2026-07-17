using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Repositories;

/// <summary>
/// Generic EF Core repository implementation.
/// Implements the Application-layer IRepository interface.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet   = context.Set<T>();
    }

    /// <inheritdoc/>
    public IQueryable<T> Query()
        => _dbSet.AsQueryable();

    /// <inheritdoc/>
    public async Task<T?> GetByIdAsync(Guid id)
        => await _dbSet.FindAsync(id);

    /// <inheritdoc/>
    public async Task<List<T>> ListAsync()
        => await _dbSet.ToListAsync();

    /// <inheritdoc/>
    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    /// <inheritdoc/>
    public Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
