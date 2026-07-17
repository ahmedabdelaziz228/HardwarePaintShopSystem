using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class PartyService : IPartyService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PartyService(IDbContextFactory<AppDbContext> dbFactory)
        => _dbFactory = dbFactory;

    public async Task<List<CustomerListItem>> SearchCustomersAsync(
        PartySearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Customers.AsNoTracking();
        if (!criteria.IncludeInactive)
            query = query.Where(c => c.IsActive);

        var term = criteria.Query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, pattern)) ||
                (c.Address != null && EF.Functions.ILike(c.Address, pattern)));
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CustomerListItem(
                c.Id,
                c.Name,
                c.Phone,
                c.CustomerType,
                c.PriceGroup != null ? c.PriceGroup.Name : null,
                c.CreditLimit,
                c.CurrentBalance,
                c.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDetails> GetCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new CustomerDetails
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Address = c.Address,
                CustomerType = c.CustomerType,
                PriceGroupId = c.PriceGroupId,
                CreditLimit = c.CreditLimit,
                CurrentBalance = c.CurrentBalance,
                Notes = c.Notes,
                IsActive = c.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("العميل المطلوب غير موجود.");
    }

    public async Task<Guid> SaveCustomerAsync(
        CustomerSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("اسم العميل مطلوب.");
        if (request.CreditLimit < 0)
            throw new InvalidOperationException("حد الائتمان لا يمكن أن يكون سالبًا.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        if (request.PriceGroupId.HasValue && !await db.PriceGroups.AnyAsync(
                p => p.Id == request.PriceGroupId.Value && p.IsActive,
                cancellationToken))
        {
            throw new InvalidOperationException("فئة السعر المختارة غير موجودة أو غير نشطة.");
        }

        Customer customer;
        if (request.Id.HasValue)
        {
            customer = await db.Customers.SingleOrDefaultAsync(
                c => c.Id == request.Id.Value,
                cancellationToken)
                ?? throw new KeyNotFoundException("العميل المطلوب تعديله غير موجود.");
        }
        else
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                CurrentBalance = 0,
                CreatedAt = DateTime.UtcNow
            };
            await db.Customers.AddAsync(customer, cancellationToken);
        }

        customer.Name = request.Name.Trim();
        customer.Phone = Normalize(request.Phone);
        customer.Address = Normalize(request.Address);
        customer.CustomerType = Normalize(request.CustomerType);
        customer.PriceGroupId = request.PriceGroupId;
        customer.CreditLimit = request.CreditLimit;
        customer.Notes = Normalize(request.Notes);
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }

    public async Task SetCustomerActiveAsync(
        Guid customerId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Id == customerId, cancellationToken)
            ?? throw new KeyNotFoundException("العميل المطلوب غير موجود.");
        customer.IsActive = isActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SupplierListItem>> SearchSuppliersAsync(
        PartySearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Suppliers.AsNoTracking();
        if (!criteria.IncludeInactive)
            query = query.Where(s => s.IsActive);

        var term = criteria.Query?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, pattern) ||
                (s.Phone != null && EF.Functions.ILike(s.Phone, pattern)) ||
                (s.Address != null && EF.Functions.ILike(s.Address, pattern)));
        }

        return await query
            .OrderBy(s => s.Name)
            .Select(s => new SupplierListItem(
                s.Id,
                s.Name,
                s.Phone,
                s.Address,
                s.CurrentBalance,
                s.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierDetails> GetSupplierAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == supplierId)
            .Select(s => new SupplierDetails
            {
                Id = s.Id,
                Name = s.Name,
                Phone = s.Phone,
                Address = s.Address,
                CurrentBalance = s.CurrentBalance,
                Notes = s.Notes,
                IsActive = s.IsActive
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("المورد المطلوب غير موجود.");
    }

    public async Task<Guid> SaveSupplierAsync(
        SupplierSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("اسم المورد مطلوب.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        Supplier supplier;
        if (request.Id.HasValue)
        {
            supplier = await db.Suppliers.SingleOrDefaultAsync(
                s => s.Id == request.Id.Value,
                cancellationToken)
                ?? throw new KeyNotFoundException("المورد المطلوب تعديله غير موجود.");
        }
        else
        {
            supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                CurrentBalance = 0,
                CreatedAt = DateTime.UtcNow
            };
            await db.Suppliers.AddAsync(supplier, cancellationToken);
        }

        supplier.Name = request.Name.Trim();
        supplier.Phone = Normalize(request.Phone);
        supplier.Address = Normalize(request.Address);
        supplier.Notes = Normalize(request.Notes);
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return supplier.Id;
    }

    public async Task SetSupplierActiveAsync(
        Guid supplierId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var supplier = await db.Suppliers.SingleOrDefaultAsync(s => s.Id == supplierId, cancellationToken)
            ?? throw new KeyNotFoundException("المورد المطلوب غير موجود.");
        supplier.IsActive = isActive;
        supplier.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
