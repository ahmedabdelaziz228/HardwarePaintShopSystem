using System.Windows.Controls;
using System.Windows.Input;
namespace HardwarePaintShop.Desktop.Views.Inventory;

public partial class InventoryView : UserControl
{
    public InventoryView() => InitializeComponent();

    private void InventorySearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            InventorySearchBox.SelectAll();
        }
    }
}
