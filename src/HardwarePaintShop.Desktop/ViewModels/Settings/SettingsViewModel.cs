using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Desktop.ViewModels.Settings;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsBackupService _settingsService;
    [ObservableProperty] private ShopSettings _settings = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    public bool CanUpdate { get; }
    public int[] PrinterWidths { get; } = { 58, 80 };

    public SettingsViewModel(ISettingsBackupService settingsService, IPermissionService permissionService)
    {
        _settingsService = settingsService; CanUpdate = permissionService.Can("Settings.Update"); _ = LoadAsync();
    }
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try { Settings = await _settingsService.GetSettingsAsync(); }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanUpdate) { SetError("ليس لديك صلاحية تعديل الإعدادات."); return; }
        IsBusy = true;
        try { await _settingsService.SaveSettingsAsync(Settings); StatusMessage = "تم حفظ الإعدادات. سيظهر اسم المحل الجديد بعد إعادة فتح البرنامج."; }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }
}
