using HardwarePaintShop.Desktop.ViewModels.Parties;
using System.Windows.Controls;
using System.Windows.Input;

namespace HardwarePaintShop.Desktop.Views.Parties;

public partial class CustomersView : UserControl
{
    public CustomersView() => InitializeComponent();

    private void CustomerSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is CustomersViewModel vm && vm.LoadCommand.CanExecute(null))
        {
            vm.LoadCommand.Execute(null);
            e.Handled = true;
        }
    }
}
