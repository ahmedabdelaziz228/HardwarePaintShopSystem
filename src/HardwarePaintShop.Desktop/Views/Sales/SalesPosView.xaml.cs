using HardwarePaintShop.Desktop.ViewModels.Sales;
using System.Windows.Controls;
using System.Windows.Input;

namespace HardwarePaintShop.Desktop.Views.Sales;

public partial class SalesPosView : UserControl
{
    public SalesPosView()
    {
        InitializeComponent();
        Loaded += (_, _) => BarcodeBox.Focus();
    }

    private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SalesPosViewModel vm && vm.SearchProductsCommand.CanExecute(null))
        {
            vm.SearchProductsCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void InvoiceSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SalesPosViewModel vm && vm.LoadInvoicesCommand.CanExecute(null))
        {
            vm.LoadInvoicesCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void SalesPosView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not SalesPosViewModel vm)
            return;

        if (e.Key == Key.F2 && vm.NewSaleCommand.CanExecute(null))
        {
            vm.NewSaleCommand.Execute(null);
            BarcodeBox.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.F9 && vm.PostCommand.CanExecute(null))
        {
            vm.PostCommand.Execute(null);
            e.Handled = true;
        }
    }
}
