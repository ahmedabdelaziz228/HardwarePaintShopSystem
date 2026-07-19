using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace HardwarePaintShop.Desktop.ViewModels.Reports;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    [ObservableProperty] private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private BusinessReportData _report = new();
    [ObservableProperty] private string _statusMessage = string.Empty;
    public bool CanExport { get; }
    public bool CanPrint { get; }

    public ReportsViewModel(IReportService reportService, IPermissionService permissionService)
    {
        _reportService = reportService;
        CanExport = permissionService.Can("Report.Export");
        CanPrint = permissionService.Can("Report.Print");
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true; ClearError();
        try { Report = await _reportService.GetBusinessReportAsync(FromDate, ToDate); StatusMessage = "تم تحديث التقرير."; }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (!CanExport) { SetError("ليس لديك صلاحية تصدير التقارير."); return; }
        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = $"shop-report-{FromDate:yyyyMMdd}-{ToDate:yyyyMMdd}.csv",
            AddExtension = true, DefaultExt = ".csv"
        };
        if (dialog.ShowDialog() != true) return;
        var csv = new StringBuilder();
        csv.AppendLine("القسم;البيان;القيمة");
        Add(csv, "ملخص", "صافي المبيعات", Report.NetSales);
        Add(csv, "ملخص", "المشتريات", Report.Purchases - Report.PurchaseReturns);
        Add(csv, "ملخص", "المصروفات", Report.Expenses);
        Add(csv, "ملخص", "تكلفة البضاعة المباعة بعد المرتجعات", Report.EstimatedCostOfSales);
        Add(csv, "ملخص", "مجمل الربح", Report.EstimatedGrossProfit);
        Add(csv, "ملخص", "صافي الربح التقديري", Report.EstimatedNetProfit);
        Add(csv, "ملخص", "مديونية العملاء", Report.CustomerDebt);
        Add(csv, "ملخص", "مستحقات الموردين", Report.SupplierDebt);
        Add(csv, "ملخص", "قيمة المخزون", Report.StockValue);
        csv.AppendLine(); csv.AppendLine("المبيعات اليومية;التاريخ;عدد الفواتير;المبيعات;المدفوع;المتبقي");
        foreach (var row in Report.DailySales)
            csv.AppendLine($";{row.Date:yyyy-MM-dd};{row.InvoiceCount};{N(row.Sales)};{N(row.Paid)};{N(row.Remaining)}");
        csv.AppendLine(); csv.AppendLine("الأصناف الأكثر مبيعًا;الصنف;الكمية الأساسية;الإيراد;التكلفة;مجمل الربح");
        foreach (var row in Report.TopProducts)
            csv.AppendLine($";{Q(row.ProductName)};{N(row.QuantityBase)};{N(row.Revenue)};{N(row.Cost)};{N(row.GrossProfit)}");
        csv.AppendLine(); csv.AppendLine("أرصدة العملاء;العميل;الهاتف;الرصيد;حد الائتمان");
        foreach (var row in Report.CustomerBalances)
            csv.AppendLine($";{Q(row.PartyName)};{Q(row.Phone)};{N(row.Balance)};{N(row.Limit)}");
        csv.AppendLine(); csv.AppendLine("أرصدة الموردين;المورد;الهاتف;الرصيد");
        foreach (var row in Report.SupplierBalances)
            csv.AppendLine($";{Q(row.PartyName)};{Q(row.Phone)};{N(row.Balance)}");
        File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
        StatusMessage = $"تم تصدير التقرير: {dialog.FileName}";
    }

    [RelayCommand]
    private void Print()
    {
        if (!CanPrint) { SetError("ليس لديك صلاحية الطباعة."); return; }
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;
        var document = BuildDocument();
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PagePadding = new Thickness(45);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "تقرير المحل");
        StatusMessage = "تم إرسال التقرير إلى الطابعة.";
    }

    private FlowDocument BuildDocument()
    {
        var document = new FlowDocument { FlowDirection = FlowDirection.RightToLeft, FontFamily = new FontFamily("Segoe UI"), FontSize = 12 };
        document.Blocks.Add(new Paragraph(new Run("تقرير أداء المحل")) { FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
        document.Blocks.Add(new Paragraph(new Run($"من {FromDate:dd/MM/yyyy} إلى {ToDate:dd/MM/yyyy}")) { TextAlignment = TextAlignment.Center });
        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(260) });
        table.Columns.Add(new TableColumn { Width = new GridLength(140) });
        var group = new TableRowGroup(); table.RowGroups.Add(group);
        AddRow(group, "صافي المبيعات", Report.NetSales);
        AddRow(group, "صافي المشتريات", Report.Purchases - Report.PurchaseReturns);
        AddRow(group, "المصروفات", Report.Expenses);
        AddRow(group, "تكلفة البضاعة المباعة", Report.EstimatedCostOfSales);
        AddRow(group, "مجمل الربح", Report.EstimatedGrossProfit);
        AddRow(group, "الربح التقديري", Report.EstimatedNetProfit);
        AddRow(group, "مديونية العملاء", Report.CustomerDebt);
        AddRow(group, "مستحقات الموردين", Report.SupplierDebt);
        AddRow(group, "قيمة المخزون", Report.StockValue);
        document.Blocks.Add(table);
        document.Blocks.Add(new Paragraph(new Run("التكلفة والربح محفوظان وقت ترحيل كل فاتورة وفق متوسط التكلفة وقت البيع.")) { FontStyle = FontStyles.Italic, Foreground = Brushes.DimGray });
        return document;
    }

    private static void AddRow(TableRowGroup group, string label, decimal value)
    {
        var row = new TableRow();
        row.Cells.Add(new TableCell(new Paragraph(new Run(label))) { Padding = new Thickness(6), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) });
        row.Cells.Add(new TableCell(new Paragraph(new Run(value.ToString("N2")))) { Padding = new Thickness(6), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1) });
        group.Rows.Add(row);
    }

    private static void Add(StringBuilder csv, string section, string label, decimal value)
        => csv.AppendLine($"{section};{label};{N(value)}");
    private static string N(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Q(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
