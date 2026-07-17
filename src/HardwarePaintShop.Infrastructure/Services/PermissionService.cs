using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuthService _authService;
    private HashSet<string> _permissionCodes = new(StringComparer.OrdinalIgnoreCase);

    public PermissionService(IDbContextFactory<AppDbContext> dbFactory, IAuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public async Task LoadPermissionsAsync()
    {
        if (_authService.CurrentUserId is null)
        {
            _permissionCodes.Clear();
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var codes = await db.Users
            .Where(u => u.Id == _authService.CurrentUserId)
            .SelectMany(u => u.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .ToListAsync();

        _permissionCodes = codes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public bool Can(string permissionCode) => _permissionCodes.Contains(permissionCode);
}
