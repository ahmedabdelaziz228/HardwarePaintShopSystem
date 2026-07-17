using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Application.Models;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Inventory;

public partial class AlertsViewModel : BaseViewModel
{
    private readonly IInventoryService _inventoryService;
    [ObservableProperty] private ObservableCollection<AlertListItem> _alerts = new();
    [ObservableProperty] private AlertListItem? _selectedAlert;
    [ObservableProperty] private bool _unreadOnly = true;
    public int UnreadCount => Alerts.Count(a => !a.IsRead);
    public int CriticalCount => Alerts.Count(a => !a.IsRead && a.Severity == Domain.Enums.AlertSeverity.Critical);
    public bool CanManage { get; }

    public AlertsViewModel(IInventoryService inventoryService, IPermissionService permissionService)
    {
        _inventoryService = inventoryService; CanManage = permissionService.Can("Alert.Manage"); _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Alerts = new ObservableCollection<AlertListItem>(await _inventoryService.RefreshAlertsAsync(UnreadOnly));
            OnPropertyChanged(nameof(UnreadCount)); OnPropertyChanged(nameof(CriticalCount));
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task MarkReadAsync(AlertListItem? alert)
    {
        alert ??= SelectedAlert; if (alert is null || alert.IsRead) return;
        try { await _inventoryService.MarkAlertReadAsync(alert.Id); await LoadAsync(); }
        catch (Exception ex) { SetError(ex.Message); }
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        if (!CanManage) { SetError("ليس لديك صلاحية إدارة التنبيهات."); return; }
        try { await _inventoryService.MarkAllAlertsReadAsync(); await LoadAsync(); }
        catch (Exception ex) { SetError(ex.Message); }
    }
}
