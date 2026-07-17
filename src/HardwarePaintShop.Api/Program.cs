using HardwarePaintShop.Api;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Services;
using HardwarePaintShop.Infrastructure.Data;
using HardwarePaintShop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Api:Urls"] ?? "http://0.0.0.0:5000");

var connectionString = Environment.GetEnvironmentVariable("HARDWARE_PAINT_SHOP_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string is missing.");

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
builder.Services.AddScoped<ApiSessionService>();
builder.Services.AddScoped<MobileCommandService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "API database initialization failed");
    throw;
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<ApiAuthenticationMiddleware>();
app.MapGet("/", () => Results.Ok(new
{
    service = "HardwarePaintShop Local API",
    version = "1.1-mobile-rc1",
    health = "/api/health"
}));
app.MapHealthChecks("/api/health");
app.MapHardwarePaintShopApi();

app.Run();

public partial class Program { }
