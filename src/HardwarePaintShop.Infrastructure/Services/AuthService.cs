using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _passwordHasher;

    public Guid? CurrentUserId { get; private set; }
    public string? CurrentUserName { get; private set; }
    public Guid? CurrentRoleId { get; private set; }
    public bool CurrentUserMustChangePassword { get; private set; }

    public AuthService(IDbContextFactory<AppDbContext> dbFactory, IPasswordHasher passwordHasher)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var user = await db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user is null)
                return new LoginResult(false, "اسم المستخدم غير موجود أو الحساب معطل.");

            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
                return new LoginResult(false, "كلمة المرور غير صحيحة.");

            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            CurrentUserId = user.Id;
            CurrentUserName = user.FullName;
            CurrentRoleId = user.RoleId;
            CurrentUserMustChangePassword = user.ForcePasswordChange;

            return new LoginResult(true, null, user.Id, user.ForcePasswordChange);
        }
        catch (Exception ex)
        {
            return new LoginResult(false, $"خطأ في الاتصال بقاعدة البيانات: {ex.Message}");
        }
    }

    public void Logout()
    {
        CurrentUserId = null;
        CurrentUserName = null;
        CurrentRoleId = null;
        CurrentUserMustChangePassword = false;
    }
}
