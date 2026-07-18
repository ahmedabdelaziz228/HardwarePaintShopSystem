using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class LicenseService : ILicenseService
{
    private const int TrialDays = 30;
    private const string PublicKey = """
-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEApDCnwQ0Wf0vn02116REh
982aNOhZGc5l6mNYvlgkSG+ED+CxIfMRN4vKkJdGFDVIfrcXdcdcHL+Iay9T0pE5
T5xtNAJKSrzJoiDXlkCYz3htUKvBef6D1yUj30y0srYJPNB/d3/1x6kb+0P9g5qx
e0d8Kb8ea3+6jQ9BRf9EQj+j77/oFf0LAXZ1LIvRXVEC/2cFUbyZlx/p3xjhqYgc
vTQXx5664jpyt8bKJcp+D4ypSoqQXHvEgxuU6PNf/hFLRfLOv6jGa+8MmWoO6YJB
K9HxJmaCNt9HkHgi+C3fnTDhV7C0nJXDIoO97b800ClTC1vx/JwKPpxjAlnP4c6D
WcpUCLxnv8dzYa2eCaXCEfKl0T0oxdAWExWDQb9GAU35wVpy+yOZbr4PA5wSEgMw
T64vR4Uhj/91JWEzxmdWnc6RWxd9JciDgAzTlBE54JpL7QRB+4AZZB/6ooCjOSex
sJHog6RPcq3bjMfLL6lmNdQUTIpUPIL6v71ddigF+ObhAgMBAAE=
-----END PUBLIC KEY-----
""";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    public LicenseService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public string GetMachineId()
    {
        var machineGuid = OperatingSystem.IsWindows()
            ? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null)?.ToString()
            : null;
        var source = $"{machineGuid ?? Environment.MachineName}|{Environment.OSVersion.Platform}|HPS-2026";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash)[..20];
    }

    public async Task<LicenseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var code = await db.AppSettings.AsNoTracking().Where(s => s.Key == "License.Code")
            .Select(s => s.Value).SingleOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(code)) return Validate(code);

        var trialText = await db.AppSettings.Where(s => s.Key == "License.TrialStartedAt")
            .SingleOrDefaultAsync(cancellationToken);
        DateTime started;
        if (trialText is null)
        {
            started = DateTime.UtcNow;
            db.AppSettings.Add(new AppSetting
            {
                Key = "License.TrialStartedAt", Value = started.ToString("O"),
                Description = "بداية الفترة التجريبية", UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!DateTime.TryParse(trialText.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out started))
            started = DateTime.UtcNow.AddDays(-TrialDays - 1);
        var expires = started.AddDays(TrialDays);
        var remaining = Math.Max(0, (int)Math.Ceiling((expires - DateTime.UtcNow).TotalDays));
        return new LicenseStatus(remaining > 0, true, GetMachineId(), "نسخة تجريبية", "Trial",
            expires, remaining, remaining > 0 ? $"الفترة التجريبية: متبقي {remaining} يوم." : "انتهت الفترة التجريبية. أدخل كود تفعيل صالح.");
    }

    public async Task<LicenseStatus> ActivateAsync(string licenseCode, CancellationToken cancellationToken = default)
    {
        var clean = licenseCode?.Trim() ?? string.Empty;
        var status = Validate(clean);
        if (!status.IsValid) throw new InvalidOperationException(status.Message);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.AppSettings.SingleOrDefaultAsync(s => s.Key == "License.Code", cancellationToken);
        if (row is null)
            db.AppSettings.Add(new AppSetting { Key = "License.Code", Value = clean, Description = "كود ترخيص موقّع", UpdatedAt = DateTime.UtcNow });
        else { row.Value = clean; row.UpdatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(cancellationToken);
        return status;
    }

    private LicenseStatus Validate(string code)
    {
        try
        {
            var parts = code.Split('.');
            if (parts.Length != 2) return Invalid("صيغة كود التفعيل غير صحيحة.");
            var payloadBytes = Base64UrlDecode(parts[0]);
            var signature = Base64UrlDecode(parts[1]);
            using var rsa = RSA.Create(); rsa.ImportFromPem(PublicKey);
            if (!rsa.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                return Invalid("توقيع كود التفعيل غير صالح.");
            var payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException();
            if (!string.Equals(payload.MachineId, GetMachineId(), StringComparison.OrdinalIgnoreCase))
                return Invalid("الكود صادر لجهاز آخر. أرسل رقم الجهاز الظاهر للبائع.");
            var expires = payload.ExpiresAt?.ToUniversalTime();
            if (expires.HasValue && expires.Value < DateTime.UtcNow)
                return Invalid("انتهت صلاحية الترخيص.", payload);
            var remaining = expires.HasValue ? Math.Max(0, (int)Math.Ceiling((expires.Value - DateTime.UtcNow).TotalDays)) : int.MaxValue;
            return new LicenseStatus(true, false, GetMachineId(), payload.CustomerName ?? "عميل",
                payload.Edition ?? "Retail", expires, remaining,
                expires.HasValue ? $"الترخيص صالح حتى {expires.Value:dd/MM/yyyy}." : "الترخيص دائم وصالح.");
        }
        catch { return Invalid("تعذر قراءة كود التفعيل أو التحقق منه."); }
    }

    private LicenseStatus Invalid(string message, LicensePayload? payload = null)
        => new(false, false, GetMachineId(), payload?.CustomerName ?? string.Empty,
            payload?.Edition ?? string.Empty, payload?.ExpiresAt, 0, message);
    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
    private sealed class LicensePayload
    {
        public string? CustomerName { get; set; }
        public string? MachineId { get; set; }
        public string? Edition { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
