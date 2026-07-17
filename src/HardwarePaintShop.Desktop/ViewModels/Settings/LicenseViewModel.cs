using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;

namespace HardwarePaintShop.Desktop.ViewModels.Settings;

public partial class LicenseViewModel : BaseViewModel
{
    private readonly ILicenseService _licenseService;
    [ObservableProperty] private LicenseStatus _status;
    [ObservableProperty] private string _licenseCode = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    public bool CanActivate { get; }
    public string MachineId => _licenseService.GetMachineId();

    public LicenseViewModel(ILicenseService licenseService, IPermissionService permissionService)
    {
        _licenseService = licenseService; CanActivate = permissionService.Can("License.Activate");
        _status = new(false, false, MachineId, string.Empty, string.Empty, null, 0, string.Empty); _ = LoadAsync();
    }
    [RelayCommand] private async Task LoadAsync() { try { Status = await _licenseService.GetStatusAsync(); } catch (Exception ex) { SetError(ex.Message); } }
    [RelayCommand]
    private async Task ActivateAsync()
    {
        if (!CanActivate) { SetError("ليس لديك صلاحية التفعيل."); return; }
        try { Status = await _licenseService.ActivateAsync(LicenseCode); StatusMessage = "تم تفعيل النسخة بنجاح."; LicenseCode = string.Empty; }
        catch (Exception ex) { SetError(ex.Message); }
    }
}
