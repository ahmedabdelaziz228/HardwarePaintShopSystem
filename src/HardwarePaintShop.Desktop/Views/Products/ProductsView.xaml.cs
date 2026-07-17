using HardwarePaintShop.Desktop.ViewModels.Products;
using System.Windows.Controls;
using System.Windows.Input;

namespace HardwarePaintShop.Desktop.Views.Products;

public partial class ProductsView : UserControl
{
    public ProductsView() => InitializeComponent();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ProductsViewModel vm && vm.LoadCommand.CanExecute(null))
        {
            vm.LoadCommand.Execute(null);
            e.Handled = true;
        }
    }
}
