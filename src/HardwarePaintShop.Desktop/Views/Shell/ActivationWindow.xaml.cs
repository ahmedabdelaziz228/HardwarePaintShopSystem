using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using System.Windows;

namespace HardwarePaintShop.Desktop.Views.Shell;

public partial class ActivationWindow : Window
{
    private readonly ILicenseService _licenseService;
    public ActivationWindow(ILicenseService licenseService, LicenseStatus status)
    {
        InitializeComponent(); _licenseService = licenseService;
        MachineIdText.Text = status.MachineId; StatusText.Text = status.Message;
    }
    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        ActivateButton.IsEnabled = false;
        try
        {
            var status = await _licenseService.ActivateAsync(LicenseCodeBox.Text);
            StatusText.Text = status.Message; DialogResult = true; Close();
        }
        catch (Exception ex) { StatusText.Text = ex.Message; }
        finally { ActivateButton.IsEnabled = true; }
    }
    private void Close_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
