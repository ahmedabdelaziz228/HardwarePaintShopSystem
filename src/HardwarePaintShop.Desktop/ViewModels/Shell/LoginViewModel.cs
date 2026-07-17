using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Desktop.ViewModels;
using System.Windows;

namespace HardwarePaintShop.Desktop.ViewModels.Shell;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public event EventHandler? LoginSucceeded;

    [ObservableProperty]
    private string _username = string.Empty;

    // Password is set from code-behind (PasswordBox security)
    public string Password { get; set; } = string.Empty;

    public LoginViewModel(IAuthService authService, IPermissionService permissionService)
    {
        _authService = authService;
        _permissionService = permissionService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("يرجى إدخال اسم المستخدم وكلمة المرور");
            return;
        }

        ClearError();
        IsBusy = true;

        try
        {
            var result = await _authService.LoginAsync(Username, Password);

            if (result.Success)
            {
                await _permissionService.LoadPermissionsAsync();
                if (result.ForcePasswordChange)
                {
                    SetError("يجب تغيير كلمة المرور الافتراضية من شاشة المستخدمين.");
                    MessageBox.Show(
                        "يجب تغيير كلمة المرور الافتراضية من شاشة المستخدمين.",
                        "تنبيه أمان",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                LoginSucceeded?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SetError(result.ErrorMessage ?? "اسم المستخدم أو كلمة المرور غير صحيحة");
            }
        }
        catch (Exception ex)
        {
            SetError($"حدث خطأ: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
