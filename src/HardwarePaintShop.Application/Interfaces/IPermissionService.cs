namespace HardwarePaintShop.Application.Interfaces;

public interface IPermissionService
{
    /// <summary>Returns true if the current user's role includes the given permission code.</summary>
    bool Can(string permissionCode);

    /// <summary>Loads permissions for the current session from the database.</summary>
    Task LoadPermissionsAsync();
}
