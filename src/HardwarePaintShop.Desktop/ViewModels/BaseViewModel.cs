using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace HardwarePaintShop.Desktop.ViewModels;

/// <summary>
/// Base class for all ViewModels.
/// Provides IsBusy, ErrorMessage, and HasError.
/// Uses CommunityToolkit.Mvvm source generators.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    protected void ClearError() => ErrorMessage = string.Empty;

    protected void SetError(string message) => ErrorMessage = message;
}
