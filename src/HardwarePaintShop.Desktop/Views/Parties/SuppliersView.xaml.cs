using HardwarePaintShop.Desktop.ViewModels.Parties;
using System.Windows.Controls;
using System.Windows.Input;

namespace HardwarePaintShop.Desktop.Views.Parties;

public partial class SuppliersView : UserControl
{
    public SuppliersView() => InitializeComponent();

    private void SupplierSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SuppliersViewModel vm && vm.LoadCommand.CanExecute(null))
        {
            vm.LoadCommand.Execute(null);
            e.Handled = true;
        }
    }
}
