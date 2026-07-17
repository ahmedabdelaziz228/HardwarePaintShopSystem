using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace HardwarePaintShop.Infrastructure.Data;

/// <summary>
/// Used by the EF Core CLI tools (dotnet ef migrations add, dotnet ef database update).
/// Run from the Infrastructure project directory.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var current = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(current, "src", "HardwarePaintShop.Desktop"),
            Path.Combine(current, "..", "HardwarePaintShop.Desktop"),
            Path.Combine(current, "..", "..", "src", "HardwarePaintShop.Desktop")
        };

        var basePath = candidates
            .Select(Path.GetFullPath)
            .FirstOrDefault(path => File.Exists(Path.Combine(path, "appsettings.json")))
            ?? throw new FileNotFoundException("Could not locate Desktop appsettings.json for EF design-time services.");

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString =
            Environment.GetEnvironmentVariable("HARDWARE_PAINT_SHOP_CONNECTION_STRING")
            ?? config.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
