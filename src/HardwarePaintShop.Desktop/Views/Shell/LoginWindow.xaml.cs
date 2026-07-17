using HardwarePaintShop.Desktop.ViewModels.Shell;
using HardwarePaintShop.Desktop.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace HardwarePaintShop.Desktop.Views.Shell;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;

        // Wire up PasswordBox (can't bind directly — WPF security restriction)
        PasswordBox.PasswordChanged += (s, e) => _vm.Password = PasswordBox.Password;

        // Listen for successful login
        _vm.LoginSucceeded += OnLoginSucceeded;
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var mainWindow = App.Services.GetRequiredService<MainWindow>();
        System.Windows.Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }
}
