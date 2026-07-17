using HardwarePaintShop.Desktop.ViewModels.Shell;
using System.Windows;

namespace HardwarePaintShop.Desktop.Views.Shell;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.Initialize();
    }
}
