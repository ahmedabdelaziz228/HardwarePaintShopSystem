using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Settings;

public partial class BackupViewModel : BaseViewModel
{
    private readonly ISettingsBackupService _backupService;
    [ObservableProperty] private ObservableCollection<BackupListItem> _history = new();
    [ObservableProperty] private string _backupFolder = string.Empty;
    [ObservableProperty] private string _restoreFile = string.Empty;
    [ObservableProperty] private bool _confirmRestore;
    [ObservableProperty] private string _statusMessage = string.Empty;
    public bool CanCreate { get; }
    public bool CanRestore { get; }

    public BackupViewModel(ISettingsBackupService backupService, IPermissionService permissionService)
    {
        _backupService = backupService; CanCreate = permissionService.Can("Backup.Create");
        CanRestore = permissionService.Can("Backup.Restore"); _ = LoadAsync();
    }
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var settings = await _backupService.GetSettingsAsync(); BackupFolder = settings.BackupFolder;
            History = new ObservableCollection<BackupListItem>(await _backupService.GetBackupHistoryAsync());
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }
    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!CanCreate) { SetError("ليس لديك صلاحية إنشاء نسخة احتياطية."); return; }
        IsBusy = true; ClearError();
        try
        {
            var file = await _backupService.CreateBackupAsync(BackupFolder);
            StatusMessage = $"تم إنشاء النسخة بنجاح: {file}";
            History = new ObservableCollection<BackupListItem>(await _backupService.GetBackupHistoryAsync());
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }
    [RelayCommand]
    private void BrowseRestore()
    {
        var dialog = new OpenFileDialog { Filter = "PostgreSQL Backup (*.backup)|*.backup|All files (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog() == true) RestoreFile = dialog.FileName;
    }
    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (!CanRestore) { SetError("ليس لديك صلاحية استرجاع النسخ الاحتياطية."); return; }
        if (!ConfirmRestore) { SetError("فعّل مربع التأكيد أولًا. الاسترجاع يستبدل البيانات الحالية."); return; }
        IsBusy = true; ClearError();
        try
        {
            await _backupService.RestoreBackupAsync(RestoreFile);
            StatusMessage = "تم الاسترجاع. أغلق البرنامج وافتحه من جديد قبل متابعة العمل.";
            ConfirmRestore = false;
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }
}
