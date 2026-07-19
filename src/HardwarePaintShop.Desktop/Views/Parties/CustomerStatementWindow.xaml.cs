using System.Windows;

namespace HardwarePaintShop.Desktop.Views.Parties;

/// <summary>Hosts the customer statement and delegates all ledger work to its view-model.</summary>
public partial class CustomerStatementWindow : Window
{
    public CustomerStatementWindow() => InitializeComponent();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
