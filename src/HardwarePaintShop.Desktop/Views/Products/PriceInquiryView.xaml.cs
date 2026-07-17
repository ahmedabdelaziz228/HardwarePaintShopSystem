using HardwarePaintShop.Desktop.ViewModels.Products;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HardwarePaintShop.Desktop.Views.Products;

public partial class PriceInquiryView : UserControl
{
    public PriceInquiryView()
    {
        InitializeComponent();
        Loaded += (_, _) => InquirySearchBox.Focus();
    }

    private void InquirySearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PriceInquiryViewModel vm && vm.SearchCommand.CanExecute(null))
        {
            vm.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }
}
