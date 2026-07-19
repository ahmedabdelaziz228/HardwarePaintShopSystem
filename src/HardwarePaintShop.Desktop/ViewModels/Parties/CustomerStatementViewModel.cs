using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace HardwarePaintShop.Desktop.ViewModels.Parties;

/// <summary>
/// Loads and prints a dated customer ledger without mixing account logic into the view.
/// </summary>
public partial class CustomerStatementViewModel : BaseViewModel
{
    private readonly IPartyService _partyService;
    private readonly ISettingsBackupService _settingsService;
    private Guid _customerId;

    [ObservableProperty] private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private CustomerStatementData _statement = new();
    [ObservableProperty] private string _statusMessage = string.Empty;

    public CustomerStatementViewModel(
        IPartyService partyService,
        ISettingsBackupService settingsService)
    {
        _partyService = partyService;
        _settingsService = settingsService;
    }

    public async Task InitializeAsync(Guid customerId)
    {
        _customerId = customerId;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_customerId == Guid.Empty)
            return;
        IsBusy = true;
        try
        {
            Statement = await _partyService.GetCustomerStatementAsync(_customerId, FromDate, ToDate);
            StatusMessage = $"تم تحميل {Statement.Lines.Count} حركة.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (_customerId == Guid.Empty)
            return;
        try
        {
            Statement = await _partyService.GetCustomerStatementAsync(_customerId, FromDate, ToDate);
            var settings = await _settingsService.GetSettingsAsync();
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
                return;
            var document = BuildDocument(settings);
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(35);
            document.ColumnWidth = Math.Max(500, dialog.PrintableAreaWidth - 70);
            dialog.PrintDocument(
                ((IDocumentPaginatorSource)document).DocumentPaginator,
                $"كشف حساب {Statement.CustomerName}");
            StatusMessage = "تم إرسال كشف الحساب للطباعة. اختر Microsoft Print to PDF للحفظ كملف PDF.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private FlowDocument BuildDocument(ShopSettings settings)
    {
        var currency = settings.CurrencySymbol;
        var document = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11
        };
        document.Blocks.Add(new Paragraph(new Run(settings.ShopName))
        {
            TextAlignment = TextAlignment.Center,
            FontSize = 21,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 3)
        });
        document.Blocks.Add(Centered("كشف حساب عميل"));
        document.Blocks.Add(Centered($"من {Statement.From:dd/MM/yyyy} إلى {Statement.To:dd/MM/yyyy}"));
        document.Blocks.Add(new Paragraph(new Run(
            $"العميل: {Statement.CustomerName}    الهاتف: {Statement.Phone ?? "—"}    رصيد أول المدة: {Statement.OpeningBalance:N2} {currency}"))
        { Margin = new Thickness(0, 12, 0, 8), FontWeight = FontWeights.SemiBold });

        var table = new Table { CellSpacing = 0 };
        foreach (var width in new[] { 95d, 210d, 90d, 90d, 90d, 100d })
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        AddRow(group, true, "التاريخ", "البيان", "المرجع", "مدين", "دائن", "الرصيد");
        foreach (var line in Statement.Lines)
        {
            AddRow(group, false,
                line.Date.ToString("dd/MM/yyyy HH:mm"),
                line.Description,
                line.ReferenceNo ?? "—",
                line.Debit == 0 ? "—" : line.Debit.ToString("N2"),
                line.Credit == 0 ? "—" : line.Credit.ToString("N2"),
                line.RunningBalance.ToString("N2"));
        }
        document.Blocks.Add(table);
        document.Blocks.Add(new Paragraph(new Run($"الرصيد الختامي: {Statement.ClosingBalance:N2} {currency}"))
        { FontSize = 16, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 12, 0, 0) });
        return document;
    }

    private static Paragraph Centered(string text)
        => new(new Run(text)) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 1, 0, 1) };

    private static void AddRow(TableRowGroup group, bool header, params string[] values)
    {
        var row = new TableRow
        {
            FontWeight = header ? FontWeights.Bold : FontWeights.Normal,
            Background = header ? new SolidColorBrush(Color.FromRgb(239, 246, 255)) : Brushes.Transparent
        };
        foreach (var value in values)
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(value)) { Margin = new Thickness(0) })
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(5)
            });
        }
        group.Rows.Add(row);
    }
}
