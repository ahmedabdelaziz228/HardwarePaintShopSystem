using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Security;
using HardwarePaintShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Data;

public class DatabaseInitializer : IDatabaseInitializer
{
    private static readonly string[] RoleNames = { "Owner", "Admin", "Cashier", "StoreKeeper" };

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseInitializer(IDbContextFactory<AppDbContext> dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        foreach (var roleName in RoleNames)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName, cancellationToken))
            {
                await db.Roles.AddAsync(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    Description = roleName,
                    IsSystemRole = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        if (!await db.Cashboxes.AnyAsync(cancellationToken))
        {
            var cashboxId = Guid.NewGuid();
            await db.Cashboxes.AddAsync(new Cashbox
            {
                Id = cashboxId,
                Name = "الخزينة الرئيسية",
                OpeningBalance = 0,
                CurrentBalance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var permissionCode in PermissionCodes.All)
        {
            if (!await db.Permissions.AnyAsync(p => p.Code == permissionCode, cancellationToken))
            {
                await db.Permissions.AddAsync(new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = permissionCode,
                    Name = permissionCode,
                    Module = permissionCode.Split('.')[0],
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var ownerRole = await db.Roles.SingleAsync(r => r.Name == "Owner", cancellationToken);
        var ownerPermissionIds = await db.RolePermissions
            .Where(rp => rp.RoleId == ownerRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);
        var missingOwnerPermissions = await db.Permissions
            .Where(p => !ownerPermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        foreach (var permissionId in missingOwnerPermissions)
        {
            await db.RolePermissions.AddAsync(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = ownerRole.Id,
                PermissionId = permissionId
            }, cancellationToken);
        }

        if (!await db.Users.AnyAsync(u => u.Username == "admin", cancellationToken))
        {
            await db.Users.AddAsync(new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                FullName = "System Administrator",
                PasswordHash = _passwordHasher.HashPassword("admin123"),
                RoleId = ownerRole.Id,
                IsActive = true,
                ForcePasswordChange = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
