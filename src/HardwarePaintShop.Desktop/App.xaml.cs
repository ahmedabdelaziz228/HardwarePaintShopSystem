using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Desktop.Views.Shell;
using HardwarePaintShop.Desktop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using System.Windows;

namespace HardwarePaintShop.Desktop;

public partial class App : System.Windows.Application
{
    private LocalApiProcessManager? _apiProcess;
    public static IServiceProvider Services { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // --- Configuration ---
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // --- Logging ---
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        Log.Information("Application starting up");

        Services = DesktopServiceRegistration.BuildServiceProvider(Configuration);
        if (!await InitializeDatabaseWithSetupAsync())
        {
            Shutdown();
            return;
        }
        if (!await EnsureValidLicenseAsync())
        {
            Shutdown();
            return;
        }
        _apiProcess = new LocalApiProcessManager(Configuration);
        _apiProcess.StartIfAvailable();
        _ = RunAutomaticBackupSafelyAsync();

        // --- Launch Login Window ---
        var loginWindow = Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    private async Task RunAutomaticBackupSafelyAsync()
    {
        try { await Services.GetRequiredService<ISettingsBackupService>().RunAutomaticBackupIfDueAsync(); }
        catch (Exception ex) { Log.Warning(ex, "Automatic backup could not be completed"); }
    }

    private async Task<bool> InitializeDatabaseWithSetupAsync()
    {
        try
        {
            await Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            return true;
        }
        catch (Exception firstError)
        {
            Log.Error(firstError, "Database initialization failed");
            var setup = new DatabaseSetupWindow(firstError.Message);
            if (setup.ShowDialog() != true) return false;
            try
            {
                Services = DesktopServiceRegistration.BuildServiceProvider(Configuration);
                await Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
                return true;
            }
            catch (Exception retryError)
            {
                Log.Error(retryError, "Database initialization failed after setup");
                MessageBox.Show("تم حفظ الاتصال لكن تهيئة قاعدة البيانات فشلت:\n" + retryError.Message,
                    "فشل التهيئة", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

    private async Task<bool> EnsureValidLicenseAsync()
    {
        try
        {
            var service = Services.GetRequiredService<ILicenseService>();
            var status = await service.GetStatusAsync();
            if (status.IsValid) return true;
            return new ActivationWindow(service, status).ShowDialog() == true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "License validation failed");
            MessageBox.Show("تعذر التحقق من ترخيص البرنامج:\n" + ex.Message,
                "خطأ الترخيص", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _apiProcess?.Dispose();
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
