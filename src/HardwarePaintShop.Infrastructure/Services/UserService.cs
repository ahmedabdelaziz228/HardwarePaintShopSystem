using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IDbContextFactory<AppDbContext> dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.Include(u => u.Role).OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task<List<Role>> GetRolesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Roles.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<User> AddUserAsync(string fullName, string username, string password, Guid roleId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException($"اسم المستخدم '{username}' مستخدم بالفعل.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Username = username,
            PasswordHash = _passwordHasher.HashPassword(password),
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUserAsync(Guid id, string fullName, string username, Guid roleId, bool isActive)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(id) ?? throw new KeyNotFoundException();
        if (await db.Users.AnyAsync(u => u.Username == username && u.Id != id))
            throw new InvalidOperationException($"اسم المستخدم '{username}' مستخدم بالفعل.");

        user.FullName = fullName;
        user.Username = username;
        user.RoleId = roleId;
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid id, string newPassword)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(id) ?? throw new KeyNotFoundException();
        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.ForcePasswordChange = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeactivateUserAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(id) ?? throw new KeyNotFoundException();
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<List<Permission>> GetAllPermissionsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Code).ToListAsync();
    }

    public async Task<List<Guid>> GetRolePermissionIdsAsync(Guid roleId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    public async Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        db.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in permissionIds.Distinct())
        {
            await db.RolePermissions.AddAsync(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task<Role> AddRoleAsync(string name, string? description)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Roles.AddAsync(role);
        await db.SaveChangesAsync();
        return role;
    }

    public async Task UpdateRoleAsync(Guid id, string name, string? description)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var role = await db.Roles.FindAsync(id) ?? throw new KeyNotFoundException();
        role.Name = name;
        role.Description = description;
        role.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
