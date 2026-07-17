namespace HardwarePaintShop.Application.Interfaces;

/// <summary>
/// Abstraction over BCrypt password hashing.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plain-text password using BCrypt.</summary>
    string HashPassword(string password);

    /// <summary>Verifies a plain-text password against a stored BCrypt hash.</summary>
    bool VerifyPassword(string password, string hash);
}
