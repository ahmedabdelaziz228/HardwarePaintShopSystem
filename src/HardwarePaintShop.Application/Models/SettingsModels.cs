namespace HardwarePaintShop.Application.Models;

public sealed class ShopSettings
{
    public string ShopName { get; set; } = "محل الحدايد والبوهيات";
    public string ShopPhone { get; set; } = string.Empty;
    public string ShopAddress { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string CommercialRegistration { get; set; } = string.Empty;
    public string InvoiceTitle { get; set; } = "فاتورة بيع";
    public string InvoiceFooter { get; set; } = "شكرًا لتعاملكم معنا";
    public string CurrencySymbol { get; set; } = "ج.م";
    public string LogoPath { get; set; } = string.Empty;
    public string InvoicePaperSize { get; set; } = "A4";
    public int ThermalPrinterWidth { get; set; } = 80;
    public string BackupFolder { get; set; } = @"C:\ShopBackups";
    public bool AutoBackupEnabled { get; set; } = true;
    public int BackupRetentionDays { get; set; } = 30;
}

public sealed record BackupListItem(
    Guid Id, string FilePath, string BackupType, string Status,
    DateTime StartedAt, DateTime? CompletedAt, string? ErrorMessage);
