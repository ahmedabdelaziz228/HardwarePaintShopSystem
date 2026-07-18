using HardwarePaintShop.Desktop.ViewModels.Users;
using System.ComponentModel;
using System.Windows.Controls;

namespace HardwarePaintShop.Desktop.Views.Users;

public partial class UsersView : UserControl
{
    private UsersViewModel? _viewModel;
    private bool _isSynchronizingPassword;

    public UsersView()
    {
        InitializeComponent();
        DataContextChanged += (_, args) => AttachViewModel(args.NewValue as UsersViewModel);
        PwdBox.PasswordChanged += (_, _) => CopyPasswordToViewModel();
    }

    private void AttachViewModel(UsersViewModel? viewModel)
    {
        if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = viewModel;
        if (_viewModel is not null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        SynchronizePasswordBox();
    }

    private void CopyPasswordToViewModel()
    {
        if (_isSynchronizingPassword || _viewModel is null) return;
        _viewModel.FormPassword = PwdBox.Password;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(UsersViewModel.FormPassword)) SynchronizePasswordBox();
    }

    private void SynchronizePasswordBox()
    {
        var password = _viewModel?.FormPassword ?? string.Empty;
        if (PwdBox.Password == password) return;
        _isSynchronizingPassword = true;
        try { PwdBox.Password = password; }
        finally { _isSynchronizingPassword = false; }
    }
}
