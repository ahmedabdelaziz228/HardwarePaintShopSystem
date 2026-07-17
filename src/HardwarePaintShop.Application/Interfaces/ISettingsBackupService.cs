using HardwarePaintShop.Application.Models;
namespace HardwarePaintShop.Application.Interfaces;
public interface ISettingsBackupService
{
    Task<ShopSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(ShopSettings settings, CancellationToken cancellationToken = default);
    Task<List<BackupListItem>> GetBackupHistoryAsync(CancellationToken cancellationToken = default);
    Task<string> CreateBackupAsync(string? folder = null, string backupType = "manual", CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string filePath, CancellationToken cancellationToken = default);
    Task RunAutomaticBackupIfDueAsync(CancellationToken cancellationToken = default);
}
