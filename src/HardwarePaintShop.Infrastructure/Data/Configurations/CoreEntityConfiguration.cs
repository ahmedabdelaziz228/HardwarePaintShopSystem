using HardwarePaintShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwarePaintShop.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasOne(x => x.ParentCategory)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LookupConfiguration :
    IEntityTypeConfiguration<Unit>,
    IEntityTypeConfiguration<PriceGroup>,
    IEntityTypeConfiguration<ExpenseCategory>,
    IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PriceGroup> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(x => x.Key);
    }
}

public class UserSecurityConfiguration :
    IEntityTypeConfiguration<Role>,
    IEntityTypeConfiguration<Permission>,
    IEntityTypeConfiguration<RolePermission>,
    IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        builder.HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.Username).IsUnique();
        builder.HasOne(x => x.Role)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration :
    IEntityTypeConfiguration<Product>,
    IEntityTypeConfiguration<ProductUnit>,
    IEntityTypeConfiguration<ProductPrice>,
    IEntityTypeConfiguration<ProductCost>,
    IEntityTypeConfiguration<ProductBarcode>,
    IEntityTypeConfiguration<ProductSerial>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(x => x.ProductCode).IsUnique();
        builder.HasIndex(x => x.Name);
        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MainSupplier)
            .WithMany()
            .HasForeignKey(x => x.MainSupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.HasIndex(x => new { x.ProductId, x.UnitId }).IsUnique();
        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductUnits)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.HasIndex(x => new { x.ProductUnitId, x.PriceGroupId }).IsUnique();
        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductPrices)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PriceGroup)
            .WithMany()
            .HasForeignKey(x => x.PriceGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ProductCost> builder)
    {
        builder.HasIndex(x => x.ProductId).IsUnique();
        builder.HasOne(x => x.Product)
            .WithOne(x => x.ProductCost)
            .HasForeignKey<ProductCost>(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ProductBarcode> builder)
    {
        builder.HasIndex(x => x.Barcode).IsUnique();
        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductBarcodes)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ProductSerial> builder)
    {
        builder.HasIndex(x => x.SerialNumber).IsUnique();
        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductSerials)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SalesInvoice)
            .WithMany()
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PartyConfiguration :
    IEntityTypeConfiguration<Customer>,
    IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Phone);
        builder.HasOne(x => x.PriceGroup)
            .WithMany()
            .HasForeignKey(x => x.PriceGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Phone);
    }
}

public class InvoiceConfiguration :
    IEntityTypeConfiguration<SalesInvoice>,
    IEntityTypeConfiguration<SalesInvoiceItem>,
    IEntityTypeConfiguration<PurchaseInvoice>,
    IEntityTypeConfiguration<PurchaseInvoiceItem>,
    IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.HasIndex(x => x.InvoiceNo).IsUnique();
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.SalesInvoices)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<SalesInvoiceItem> builder)
    {
        builder.Property(x => x.GrossProfit).HasPrecision(18, 2);
        builder.HasOne(x => x.SalesInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.HasIndex(x => x.InvoiceNo).IsUnique();
        builder.HasIndex(x => new { x.SupplierId, x.SupplierInvoiceNo }).IsUnique();
        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.PurchaseInvoices)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
    {
        builder.HasOne(x => x.PurchaseInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.HasIndex(x => x.PaymentNo).IsUnique();
        builder.HasOne(x => x.SalesInvoice)
            .WithMany()
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cashbox)
            .WithMany()
            .HasForeignKey(x => x.CashboxId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FinanceConfiguration :
    IEntityTypeConfiguration<Cashbox>,
    IEntityTypeConfiguration<CashMovement>,
    IEntityTypeConfiguration<Expense>,
    IEntityTypeConfiguration<OwnerWithdrawal>,
    IEntityTypeConfiguration<StockMovement>,
    IEntityTypeConfiguration<CustomerTransaction>,
    IEntityTypeConfiguration<SupplierTransaction>
{
    public void Configure(EntityTypeBuilder<Cashbox> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.MovementType);
        builder.HasOne(x => x.Cashbox)
            .WithMany(x => x.CashMovements)
            .HasForeignKey(x => x.CashboxId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasOne(x => x.ExpenseCategory)
            .WithMany()
            .HasForeignKey(x => x.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Cashbox)
            .WithMany()
            .HasForeignKey(x => x.CashboxId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<OwnerWithdrawal> builder)
    {
        builder.HasOne(x => x.Cashbox)
            .WithMany()
            .HasForeignKey(x => x.CashboxId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.MovementType);
        builder.HasOne(x => x.Product)
            .WithMany(x => x.StockMovements)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<CustomerTransaction> builder)
    {
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<SupplierTransaction> builder)
    {
        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Supplier)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReturnInventorySystemConfiguration :
    IEntityTypeConfiguration<Return>,
    IEntityTypeConfiguration<ReturnItem>,
    IEntityTypeConfiguration<InventoryCount>,
    IEntityTypeConfiguration<InventoryCountItem>,
    IEntityTypeConfiguration<Alert>,
    IEntityTypeConfiguration<BackupLog>,
    IEntityTypeConfiguration<SyncDevice>,
    IEntityTypeConfiguration<SyncLog>,
    IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<Return> builder)
    {
        builder.HasIndex(x => x.ReturnNo).IsUnique();
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OriginalSalesInvoice)
            .WithMany()
            .HasForeignKey(x => x.OriginalSalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OriginalPurchaseInvoice)
            .WithMany()
            .HasForeignKey(x => x.OriginalPurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.HasOne(x => x.Return)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ReturnId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductUnit)
            .WithMany()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.HasIndex(x => x.CountNo).IsUnique();
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<InventoryCountItem> builder)
    {
        builder.HasOne(x => x.InventoryCount)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InventoryCountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasIndex(x => x.IsRead);
        builder.HasIndex(x => x.AlertType);
    }

    public void Configure(EntityTypeBuilder<BackupLog> builder)
    {
        builder.HasIndex(x => x.StartedAt);
    }

    public void Configure(EntityTypeBuilder<SyncDevice> builder)
    {
        builder.HasIndex(x => x.DeviceToken).IsUnique();
    }

    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.HasOne(x => x.SyncDevice)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(x => new { x.TableName, x.RecordId });
        builder.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
