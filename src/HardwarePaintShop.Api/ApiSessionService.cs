using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HardwarePaintShop.Api;

public sealed class ApiSessionService
{
    private const string SessionPrefix = "Api.Session.";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public ApiSessionService(
        IDbContextFactory<AppDbContext> dbFactory,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _dbFactory = dbFactory;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        if (username.Length == 0 || string.IsNullOrEmpty(request.Password))
            throw new ApiProblemException(400, "اسم المستخدم وكلمة المرور مطلوبان.");
        if (string.IsNullOrWhiteSpace(request.DeviceName))
            throw new ApiProblemException(400, "اسم الجهاز مطلوب.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.Include(u => u.Role).ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken)
            ?? throw new ApiProblemException(401, "بيانات الدخول غير صحيحة.");
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new ApiProblemException(401, "بيانات الدخول غير صحيحة.");
        if (user.ForcePasswordChange)
            throw new ApiProblemException(403, "غيّر كلمة المرور من برنامج الكمبيوتر أولًا.");

        var permissions = user.Role.RolePermissions.Select(rp => rp.Permission.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!permissions.Contains("Api.Access"))
            throw new ApiProblemException(403, "الحساب لا يملك صلاحية استخدام تطبيق الموبايل.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = HashToken(token);
        var device = await db.SyncDevices.SingleOrDefaultAsync(d => d.DeviceToken == tokenHash, cancellationToken);
        if (device is null)
        {
            device = new SyncDevice
            {
                Id = Guid.NewGuid(),
                DeviceName = request.DeviceName.Trim(),
                DeviceToken = tokenHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.SyncDevices.Add(device);
        }
        var days = int.TryParse(_configuration["Api:SessionDays"], out var configuredDays)
            ? Math.Clamp(configuredDays, 1, 365) : 30;
        var expires = DateTime.UtcNow.AddDays(days);
        db.AppSettings.Add(new AppSetting
        {
            Key = SessionPrefix + tokenHash,
            Value = JsonSerializer.Serialize(new StoredApiSession
            {
                UserId = user.Id,
                DeviceId = device.Id,
                ExpiresAt = expires
            }),
            Description = "Mobile API session",
            UpdatedAt = DateTime.UtcNow
        });
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new LoginResponse(token, expires, user.Id, user.FullName, device.Id, permissions.OrderBy(x => x).ToList());
    }

    public async Task<ApiUserContext?> ResolveAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var tokenHash = HashToken(token);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var sessionRow = await db.AppSettings.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Key == SessionPrefix + tokenHash, cancellationToken);
        if (sessionRow?.Value is null) return null;
        StoredApiSession? session;
        try { session = JsonSerializer.Deserialize<StoredApiSession>(sessionRow.Value); }
        catch { return null; }
        if (session is null || session.ExpiresAt <= DateTime.UtcNow) return null;
        var deviceActive = await db.SyncDevices.AsNoTracking().AnyAsync(
            d => d.Id == session.DeviceId && d.DeviceToken == tokenHash && d.IsActive,
            cancellationToken);
        if (!deviceActive) return null;
        var user = await db.Users.AsNoTracking().Where(u => u.Id == session.UserId && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.RoleId,
                Permissions = u.Role.RolePermissions.Select(rp => rp.Permission.Code).ToList()
            }).SingleOrDefaultAsync(cancellationToken);
        if (user is null || !user.Permissions.Contains("Api.Access")) return null;
        return new ApiUserContext(user.Id, user.FullName, user.RoleId, session.DeviceId, session.ExpiresAt,
            user.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase));
    }

    public async Task LogoutAsync(string token, ApiUserContext user, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.AppSettings.SingleOrDefaultAsync(s => s.Key == SessionPrefix + tokenHash, cancellationToken);
        if (session is not null) db.AppSettings.Remove(session);
        var device = await db.SyncDevices.SingleOrDefaultAsync(d => d.Id == user.DeviceId, cancellationToken);
        if (device is not null) device.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public static string ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers["Authorization"].ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : string.Empty;
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public sealed class ApiAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    public ApiAuthenticationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ApiSessionService sessions)
    {
        var path = context.Request.Path;
        if (path == "/" || path.StartsWithSegments("/api/health") || path.StartsWithSegments("/api/auth/login"))
        {
            await _next(context);
            return;
        }
        var token = ApiSessionService.ReadBearerToken(context.Request);
        var user = await sessions.ResolveAsync(token, context.RequestAborted);
        if (user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "جلسة الدخول غير صالحة أو منتهية." }, context.RequestAborted);
            return;
        }
        context.Items[ApiUserContext.ItemKey] = user;
        await _next(context);
    }
}

public sealed class ApiProblemException : Exception
{
    public int StatusCode { get; }
    public ApiProblemException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiProblemException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message }, context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API request error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new { error = "حدث خطأ داخلي. راجع سجل API على الكمبيوتر." },
                context.RequestAborted);
        }
    }
}
