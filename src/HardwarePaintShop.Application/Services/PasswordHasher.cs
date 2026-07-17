using HardwarePaintShop.Application.Interfaces;

namespace HardwarePaintShop.Application.Services;

/// <summary>
/// BCrypt password hasher using BCrypt.Net-Next library.
/// Work factor 12 provides a good balance between security and performance.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc/>
    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    /// <inheritdoc/>
    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
