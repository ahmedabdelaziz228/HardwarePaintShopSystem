using Microsoft.Extensions.Configuration;
using Serilog;
using System.Diagnostics;
using System.IO;

namespace HardwarePaintShop.Desktop.Services;

public sealed class LocalApiProcessManager : IDisposable
{
    private readonly IConfiguration _configuration;
    private Process? _process;

    public LocalApiProcessManager(IConfiguration configuration) => _configuration = configuration;

    public void StartIfAvailable()
    {
        if (!bool.TryParse(_configuration["Api:AutoStart"], out var autoStart) || !autoStart) return;
        var executable = Path.Combine(AppContext.BaseDirectory, "Api", "HardwarePaintShop.Api.exe");
        if (!File.Exists(executable))
        {
            Log.Information("Local API executable was not found at {Path}; skipping automatic start", executable);
            return;
        }

        try
        {
            var port = int.TryParse(_configuration["Api:Port"], out var configuredPort)
                ? Math.Clamp(configuredPort, 1024, 65535) : 5000;
            _process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = Path.GetDirectoryName(executable)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Environment =
                {
                    ["ASPNETCORE_URLS"] = $"http://0.0.0.0:{port}",
                    ["ConnectionStrings__DefaultConnection"] =
                        _configuration.GetConnectionString("DefaultConnection") ?? string.Empty
                }
            });
            Log.Information("Local mobile API started on port {Port}", port);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Local mobile API could not be started");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
            _process?.Dispose();
        }
        catch (Exception ex) { Log.Warning(ex, "Local mobile API could not be stopped cleanly"); }
    }
}
