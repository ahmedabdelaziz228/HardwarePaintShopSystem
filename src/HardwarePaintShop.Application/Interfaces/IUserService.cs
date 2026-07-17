using HardwarePaintShop.Domain.Entities;

namespace HardwarePaintShop.Application.Interfaces;

public interface IUserService
{
    Task<List<User>>  GetUsersAsync();
    Task<List<Role>>  GetRolesAsync();
    Task<User>        AddUserAsync(string fullName, string username, string password, Guid roleId);
    Task              UpdateUserAsync(Guid id, string fullName, string username, Guid roleId, bool isActive);
    Task              ChangePasswordAsync(Guid id, string newPassword);
    Task              DeactivateUserAsync(Guid id);

    Task<List<Permission>> GetAllPermissionsAsync();
    Task<List<Guid>>       GetRolePermissionIdsAsync(Guid roleId);
    Task                   SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    Task<Role>             AddRoleAsync(string name, string? description);
    Task                   UpdateRoleAsync(Guid id, string name, string? description);
}
