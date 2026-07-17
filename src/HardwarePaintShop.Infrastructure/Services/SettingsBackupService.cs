using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using HardwarePaintShop.Domain.Entities;
using HardwarePaintShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Diagnostics;

namespace HardwarePaintShop.Infrastructure.Services;

public sealed class SettingsBackupService : ISettingsBackupService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IConfiguration _configuration;

    public SettingsBackupService(IDbContextFactory<AppDbContext> dbFactory, IConfiguration configuration)
    {
        _dbFactory = dbFactory; _configuration = configuration;
    }

    public async Task<ShopSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var values = await db.AppSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
        return new ShopSettings
        {
            ShopName = Get(values, "Shop.Name", _configuration["AppSettings:ShopName"] ?? "محل الحدايد والبوهيات"),
            ShopPhone = Get(values, "Shop.Phone", _configuration["AppSettings:ShopPhone"] ?? string.Empty),
            ShopAddress = Get(values, "Shop.Address", _configuration["AppSettings:ShopAddress"] ?? string.Empty),
            TaxNumber = Get(values, "Shop.TaxNumber", string.Empty),
            ThermalPrinterWidth = GetInt(values, "Print.ThermalWidth", GetConfigurationInt("AppSettings:ThermalPrinterWidth", 80)),
            BackupFolder = Get(values, "Backup.Folder", _configuration["AppSettings:BackupFolder"] ?? @"C:\ShopBackups"),
            AutoBackupEnabled = GetBool(values, "Backup.AutoEnabled", GetConfigurationBool("AppSettings:AutoBackupEnabled", true)),
            BackupRetentionDays = GetInt(values, "Backup.RetentionDays", GetConfigurationInt("AppSettings:BackupRetentionDays", 30))
        };
    }

    public async Task SaveSettingsAsync(ShopSettings settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ShopName)) throw new InvalidOperationException("اسم المحل مطلوب.");
        if (settings.ThermalPrinterWidth is not (58 or 80)) throw new InvalidOperationException("عرض الطابعة الحرارية يجب أن يكون 58 أو 80 مم.");
        if (settings.BackupRetentionDays < 1) throw new InvalidOperationException("مدة الاحتفاظ يجب أن تكون يومًا واحدًا على الأقل.");
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await PutAsync(db, "Shop.Name", settings.ShopName.Trim(), "اسم المحل", cancellationToken);
        await PutAsync(db, "Shop.Phone", settings.ShopPhone?.Trim(), "هاتف المحل", cancellationToken);
        await PutAsync(db, "Shop.Address", settings.ShopAddress?.Trim(), "عنوان المحل", cancellationToken);
        await PutAsync(db, "Shop.TaxNumber", settings.TaxNumber?.Trim(), "الرقم الضريبي", cancellationToken);
        await PutAsync(db, "Print.ThermalWidth", settings.ThermalPrinterWidth.ToString(), "عرض ورق الطابعة الحرارية", cancellationToken);
        await PutAsync(db, "Backup.Folder", settings.BackupFolder?.Trim(), "مجلد النسخ الاحتياطي", cancellationToken);
        await PutAsync(db, "Backup.AutoEnabled", settings.AutoBackupEnabled.ToString(), "نسخ احتياطي تلقائي", cancellationToken);
        await PutAsync(db, "Backup.RetentionDays", settings.BackupRetentionDays.ToString(), "مدة الاحتفاظ بالنسخ", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<BackupListItem>> GetBackupHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.BackupLogs.AsNoTracking().OrderByDescending(b => b.StartedAt).Take(300)
            .Select(b => new BackupListItem(b.Id, b.FilePath, b.BackupType, b.Status,
                b.StartedAt, b.CompletedAt, b.ErrorMessage)).ToListAsync(cancellationToken);
    }

    public async Task<string> CreateBackupAsync(
        string? folder = null, string backupType = "manual", CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        folder = string.IsNullOrWhiteSpace(folder) ? settings.BackupFolder : folder.Trim();
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"HardwarePaintShop_{DateTime.Now:yyyyMMdd_HHmmss}.backup");
        var log = new BackupLog { Id = Guid.NewGuid(), FilePath = file, BackupType = backupType, Status = "running", StartedAt = DateTime.UtcNow };
        await SaveLogAsync(log, cancellationToken);
        try
        {
            var result = await RunPostgresToolAsync("pg_dump", new[] { "--format=custom", "--no-owner", "--no-privileges", $"--file={file}", $"--dbname={BuildPostgresUri()}" }, cancellationToken);
            if (result.ExitCode != 0) throw new InvalidOperationException(CleanError(result.Error));
            log.Status = "success"; log.CompletedAt = DateTime.UtcNow;
            await UpdateLogAsync(log, cancellationToken);
            CleanupOldBackups(folder, settings.BackupRetentionDays);
            return file;
        }
        catch (Exception ex)
        {
            log.Status = "failed"; log.CompletedAt = DateTime.UtcNow; log.ErrorMessage = ex.Message;
            await UpdateLogAsync(log, cancellationToken); throw;
        }
    }

    public async Task RestoreBackupAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود.", filePath);
        await CreateBackupAsync(backupType: "before_restore", cancellationToken: cancellationToken);
        var result = await RunPostgresToolAsync("pg_restore", new[]
        {
            "--clean", "--if-exists", "--no-owner", "--no-privileges", $"--dbname={BuildPostgresUri()}", filePath
        }, cancellationToken);
        if (result.ExitCode != 0) throw new InvalidOperationException(CleanError(result.Error));
    }

    public async Task RunAutomaticBackupIfDueAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        if (!settings.AutoBackupEnabled) return;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var last = await db.BackupLogs.AsNoTracking().Where(b => b.Status == "success")
            .OrderByDescending(b => b.CompletedAt).Select(b => b.CompletedAt).FirstOrDefaultAsync(cancellationToken);
        if (!last.HasValue || last.Value < DateTime.UtcNow.AddHours(-20))
            await CreateBackupAsync(settings.BackupFolder, "auto", cancellationToken);
    }

    private async Task PutAsync(AppDbContext db, string key, string? value, string description, CancellationToken token)
    {
        var row = await db.AppSettings.SingleOrDefaultAsync(s => s.Key == key, token);
        if (row is null) db.AppSettings.Add(new AppSetting { Key = key, Value = value, Description = description, UpdatedAt = DateTime.UtcNow });
        else { row.Value = value; row.Description = description; row.UpdatedAt = DateTime.UtcNow; }
    }

    private async Task SaveLogAsync(BackupLog log, CancellationToken token)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(token); db.BackupLogs.Add(log); await db.SaveChangesAsync(token);
    }
    private async Task UpdateLogAsync(BackupLog log, CancellationToken token)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(token); db.BackupLogs.Update(log); await db.SaveChangesAsync(token);
    }

    private string BuildPostgresUri()
    {
        var raw = Environment.GetEnvironmentVariable("HARDWARE_PAINT_SHOP_CONNECTION_STRING")
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection String غير موجود.");
        var cs = new NpgsqlConnectionStringBuilder(raw);
        var user = Uri.EscapeDataString(cs.Username ?? string.Empty);
        var password = Uri.EscapeDataString(cs.Password ?? string.Empty);
        var database = Uri.EscapeDataString(cs.Database ?? string.Empty);
        return $"postgresql://{user}:{password}@{cs.Host}:{cs.Port}/{database}";
    }

    private static async Task<ProcessResult> RunPostgresToolAsync(
        string tool, IEnumerable<string> arguments, CancellationToken token)
    {
        var executable = ResolvePostgresTool(tool);
        var start = new ProcessStartInfo { FileName = executable, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"تعذر تشغيل {tool}.");
        var outputTask = process.StandardOutput.ReadToEndAsync(token);
        var errorTask = process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token);
        return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
    }

    private static string ResolvePostgresTool(string tool)
    {
        var suffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
        var pgBin = Environment.GetEnvironmentVariable("PG_BIN");
        if (!string.IsNullOrWhiteSpace(pgBin)) return Path.Combine(pgBin, tool + suffix);
        if (OperatingSystem.IsWindows())
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL");
            if (Directory.Exists(root))
            {
                var candidate = Directory.EnumerateDirectories(root).OrderByDescending(x => x)
                    .Select(x => Path.Combine(x, "bin", tool + suffix)).FirstOrDefault(File.Exists);
                if (candidate is not null) return candidate;
            }
        }
        return tool + suffix;
    }

    private static void CleanupOldBackups(string folder, int retentionDays)
    {
        var cutoff = DateTime.Now.AddDays(-retentionDays);
        foreach (var file in Directory.EnumerateFiles(folder, "HardwarePaintShop_*.backup").Select(path => new FileInfo(path)).Where(f => f.CreationTime < cutoff))
            try { file.Delete(); } catch { /* cleanup failure must not invalidate a good backup */ }
    }

    private static string CleanError(string error) => string.IsNullOrWhiteSpace(error)
        ? "فشلت أداة PostgreSQL بدون تفاصيل. تأكد أن pg_dump وpg_restore موجودان في PATH أو عرّف PG_BIN."
        : error.Trim();
    private static string Get(Dictionary<string, string?> values, string key, string fallback) => values.GetValueOrDefault(key) ?? fallback;
    private static int GetInt(Dictionary<string, string?> values, string key, int fallback) => int.TryParse(values.GetValueOrDefault(key), out var result) ? result : fallback;
    private static bool GetBool(Dictionary<string, string?> values, string key, bool fallback) => bool.TryParse(values.GetValueOrDefault(key), out var result) ? result : fallback;
    private int GetConfigurationInt(string key, int fallback)
        => int.TryParse(_configuration[key], out var result) ? result : fallback;
    private bool GetConfigurationBool(string key, bool fallback)
        => bool.TryParse(_configuration[key], out var result) ? result : fallback;
    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
