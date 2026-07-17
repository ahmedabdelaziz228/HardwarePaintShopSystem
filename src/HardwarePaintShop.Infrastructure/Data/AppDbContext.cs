using HardwarePaintShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HardwarePaintShop.Infrastructure.Data;

/// <summary>
/// Main EF Core DbContext for the Hardware Paint Shop system.
/// All DbSets are declared here; configuration is done via Fluent API in Configurations/.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Lookup tables ──────────────────────────────────────────────────────
    public DbSet<Category>        Categories       { get; set; } = null!;
    public DbSet<Unit>            Units            { get; set; } = null!;
    public DbSet<PriceGroup>      PriceGroups      { get; set; } = null!;
    public DbSet<ExpenseCategory> ExpenseCategories{ get; set; } = null!;

    // ── Users & Roles ──────────────────────────────────────────────────────
    public DbSet<Role>            Roles            { get; set; } = null!;
    public DbSet<Permission>      Permissions      { get; set; } = null!;
    public DbSet<RolePermission>  RolePermissions  { get; set; } = null!;
    public DbSet<User>            Users            { get; set; } = null!;

    // ── Business Partners ─────────────────────────────────────────────────
    public DbSet<Customer>        Customers        { get; set; } = null!;
    public DbSet<Supplier>        Suppliers        { get; set; } = null!;

    // ── Products ──────────────────────────────────────────────────────────
    public DbSet<Product>         Products         { get; set; } = null!;
    public DbSet<ProductUnit>     ProductUnits     { get; set; } = null!;
    public DbSet<ProductPrice>    ProductPrices    { get; set; } = null!;
    public DbSet<ProductCost>     ProductCosts     { get; set; } = null!;
    public DbSet<ProductBarcode>  ProductBarcodes  { get; set; } = null!;
    public DbSet<ProductSerial>   ProductSerials   { get; set; } = null!;

    // ── Invoices ──────────────────────────────────────────────────────────
    public DbSet<SalesInvoice>         SalesInvoices        { get; set; } = null!;
    public DbSet<SalesInvoiceItem>     SalesInvoiceItems    { get; set; } = null!;
    public DbSet<PurchaseInvoice>      PurchaseInvoices     { get; set; } = null!;
    public DbSet<PurchaseInvoiceItem>  PurchaseInvoiceItems { get; set; } = null!;
    public DbSet<InvoicePayment>       InvoicePayments      { get; set; } = null!;

    // ── Accounting ────────────────────────────────────────────────────────
    public DbSet<CustomerTransaction>  CustomerTransactions { get; set; } = null!;
    public DbSet<SupplierTransaction>  SupplierTransactions { get; set; } = null!;

    // ── Stock & Cash ──────────────────────────────────────────────────────
    public DbSet<StockMovement>   StockMovements   { get; set; } = null!;
    public DbSet<Cashbox>         Cashboxes        { get; set; } = null!;
    public DbSet<CashMovement>    CashMovements    { get; set; } = null!;
    public DbSet<Expense>         Expenses         { get; set; } = null!;
    public DbSet<OwnerWithdrawal> OwnerWithdrawals { get; set; } = null!;

    // ── Returns ───────────────────────────────────────────────────────────
    public DbSet<Return>          Returns          { get; set; } = null!;
    public DbSet<ReturnItem>      ReturnItems      { get; set; } = null!;

    // ── Inventory Counts ──────────────────────────────────────────────────
    public DbSet<InventoryCount>      InventoryCounts      { get; set; } = null!;
    public DbSet<InventoryCountItem>  InventoryCountItems  { get; set; } = null!;

    // ── System ────────────────────────────────────────────────────────────
    public DbSet<Alert>           Alerts           { get; set; } = null!;
    public DbSet<BackupLog>       BackupLogs       { get; set; } = null!;
    public DbSet<AppSetting>      AppSettings      { get; set; } = null!;
    public DbSet<AuditLog>        AuditLogs        { get; set; } = null!;
    public DbSet<SyncDevice>      SyncDevices      { get; set; } = null!;
    public DbSet<SyncLog>         SyncLogs         { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                if (foreignKey.DeleteBehavior != DeleteBehavior.Cascade)
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }

            foreach (var property in entityType.GetProperties())
            {
                var entityBuilder = modelBuilder.Entity(entityType.ClrType);

                if (property.ClrType.IsEnum)
                    entityBuilder.Property(property.Name).HasConversion<string>();

                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    var name = property.Name;
                    if (name.Contains("Quantity", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Qty", StringComparison.OrdinalIgnoreCase))
                    {
                        entityBuilder.Property(property.Name).HasPrecision(18, 3);
                    }
                    else if (name.Contains("ConversionFactor", StringComparison.OrdinalIgnoreCase))
                    {
                        entityBuilder.Property(property.Name).HasPrecision(18, 4);
                    }
                    else if (name.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Cost", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Balance", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Limit", StringComparison.OrdinalIgnoreCase))
                    {
                        entityBuilder.Property(property.Name).HasPrecision(18, 2);
                    }
                }
            }
        }
    }
}
