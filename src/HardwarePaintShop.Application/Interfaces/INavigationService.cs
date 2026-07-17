namespace HardwarePaintShop.Application.Interfaces;

public interface INavigationService
{
    void NavigateTo(string pageKey, object? parameter = null);
}
