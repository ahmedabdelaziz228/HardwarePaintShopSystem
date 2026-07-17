namespace HardwarePaintShop.Application.Interfaces;

public record LoginResult(
    bool Success,
    string? ErrorMessage = null,
    Guid? UserId = null,
    bool ForcePasswordChange = false);

public interface IAuthService
{
    /// <summary>Validates credentials and stores the current session.</summary>
    Task<LoginResult> LoginAsync(string username, string password);

    /// <summary>Clears the current session.</summary>
    void Logout();

    /// <summary>Returns the user ID of the currently logged-in user, or null.</summary>
    Guid? CurrentUserId { get; }

    /// <summary>Returns the display name of the current user.</summary>
    string? CurrentUserName { get; }

    /// <summary>Returns true when the current user must change password before normal use.</summary>
    bool CurrentUserMustChangePassword { get; }
}
