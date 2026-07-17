using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Security;
using HardwarePaintShop.Application.Services;
using HardwarePaintShop.Infrastructure.Data;
using HardwarePaintShop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace HardwarePaintShop.IntegrationTests;

public sealed class DatabaseInitializationTests
{
    [Fact(Skip = "Set HARDWARE_PAINT_SHOP_TEST_CONNECTION_STRING to a dedicated PostgreSQL test database connection string, then remove this skip to run live database initialization verification.")]
    public async Task FreshDatabase_SeedsAdminAndPermissions_AndAuthenticates()
    {
        var connectionString = Environment.GetEnvironmentVariable("HARDWARE_PAINT_SHOP_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("HARDWARE_PAINT_SHOP_TEST_CONNECTION_STRING is required.");

        AssertDedicatedTestDatabase(connectionString);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var dbFactory = new TestDbContextFactory(options);
        var passwordHasher = new PasswordHasher();
        var initializer = new DatabaseInitializer(dbFactory, passwordHasher);

        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        await using (var db = dbFactory.CreateDbContext())
        {
            Assert.Equal(1, await db.Roles.CountAsync(x => x.Name == "Owner"));
            Assert.Equal(1, await db.Roles.CountAsync(x => x.Name == "Admin"));
            Assert.Equal(1, await db.Roles.CountAsync(x => x.Name == "Cashier"));
            Assert.Equal(1, await db.Roles.CountAsync(x => x.Name == "StoreKeeper"));
            Assert.Equal(1, await db.Cashboxes.CountAsync(x => x.Name == "الخزينة الرئيسية"));

            foreach (var permissionCode in PermissionCodes.All)
                Assert.Equal(1, await db.Permissions.CountAsync(x => x.Code == permissionCode));

            var ownerRole = await db.Roles.SingleAsync(x => x.Name == "Owner");
            Assert.Equal(
                PermissionCodes.All.Count,
                await db.RolePermissions.CountAsync(x => x.RoleId == ownerRole.Id));

            var admin = await db.Users.Include(x => x.Role).SingleAsync(x => x.Username == "admin");
            Assert.True(admin.IsActive);
            Assert.True(admin.ForcePasswordChange);
            Assert.Equal("Owner", admin.Role.Name);
            Assert.NotEqual("admin123", admin.PasswordHash);
            Assert.True(passwordHasher.VerifyPassword("admin123", admin.PasswordHash));
        }

        var authService = new AuthService(dbFactory, passwordHasher);
        var permissionService = new PermissionService(dbFactory, authService);

        var failedLogin = await authService.LoginAsync("admin", "wrong-password");
        Assert.False(failedLogin.Success);

        var login = await authService.LoginAsync("admin", "admin123");
        Assert.True(login.Success);
        Assert.True(login.ForcePasswordChange);

        await permissionService.LoadPermissionsAsync();
        Assert.True(permissionService.Can("Dashboard.View"));
        Assert.True(permissionService.Can("Category.View"));
        Assert.True(permissionService.Can("Unit.View"));
        Assert.True(permissionService.Can("PriceGroup.View"));
        Assert.True(permissionService.Can("ExpenseCategory.View"));
        Assert.True(permissionService.Can("User.View"));
        Assert.True(permissionService.Can("Role.View"));
        Assert.True(permissionService.Can("Purchase.View"));
        Assert.True(permissionService.Can("Cashbox.View"));
        Assert.True(permissionService.Can("Sales.View"));
    }

    private static void AssertDedicatedTestDatabase(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database ?? string.Empty;

        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase) &&
            !database.Contains("dev_verify", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run integration test against database '{database}'. Use a dedicated test database name containing 'test' or 'dev_verify'.");
        }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new(_options);
    }
}
