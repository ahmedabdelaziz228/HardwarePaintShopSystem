using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Users;

public partial class RolesViewModel : BaseViewModel
{
    private readonly IUserService _userService;

    [ObservableProperty] private ObservableCollection<Role>             _roles       = new();
    [ObservableProperty] private ObservableCollection<PermissionItem>   _permissions = new();
    [ObservableProperty] private Role? _selectedRole;

    [ObservableProperty] private string _formName        = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private bool   _isEditing;
    [ObservableProperty] private Guid?  _editingId;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isSuccess;

    public RolesViewModel(IUserService userService)
    {
        _userService = userService;
        _ = LoadAsync();
    }

    partial void OnSelectedRoleChanged(Role? value) => _ = LoadPermissionsForRoleAsync(value);

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Roles = new ObservableCollection<Role>(await _userService.GetRolesAsync());
            var allPerms = await _userService.GetAllPermissionsAsync();
            foreach (var p in allPerms)
                if (!Permissions.Any(x => x.Id == p.Id))
                    Permissions.Add(new PermissionItem(p.Id, p.Code, p.Name));
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private async Task LoadPermissionsForRoleAsync(Role? role)
    {
        if (role is null) return;
        IsBusy = true;
        try
        {
            var allPerms   = await _userService.GetAllPermissionsAsync();
            var grantedIds = await _userService.GetRolePermissionIdsAsync(role.Id);
            Permissions.Clear();
            foreach (var p in allPerms)
                Permissions.Add(new PermissionItem(p.Id, p.Code, p.Name) { IsGranted = grantedIds.Contains(p.Id) });
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void NewRole()
    {
        IsEditing = false; EditingId = null; SelectedRole = null;
        FormName = string.Empty; FormDescription = string.Empty; StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void EditRole(Role role)
    {
        IsEditing = true; EditingId = role.Id; SelectedRole = role;
        FormName = role.Name; FormDescription = role.Description ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveRoleAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName)) { ShowError("الاسم مطلوب."); return; }
        IsBusy = true;
        try
        {
            if (IsEditing && EditingId.HasValue)
                await _userService.UpdateRoleAsync(EditingId.Value, FormName,
                    string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription);
            else
                await _userService.AddRoleAsync(FormName,
                    string.IsNullOrWhiteSpace(FormDescription) ? null : FormDescription);
            ShowSuccess("تم حفظ الصلاحية ✓"); NewRoleCommand.Execute(null); await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SavePermissionsAsync()
    {
        if (SelectedRole is null) { ShowError("اختر صلاحية أولاً."); return; }
        IsBusy = true;
        try
        {
            var grantedIds = Permissions.Where(p => p.IsGranted).Select(p => p.Id);
            await _userService.SetRolePermissionsAsync(SelectedRole.Id, grantedIds);
            ShowSuccess("تم حفظ الأذونات ✓");
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void ShowError(string msg)   { StatusMessage = msg; IsSuccess = false; }
    private void ShowSuccess(string msg) { StatusMessage = msg; IsSuccess = true; }
}

public partial class PermissionItem : ObservableObject
{
    public Guid   Id        { get; }
    public string Code      { get; }
    public string Name      { get; }
    [ObservableProperty] private bool _isGranted;

    public PermissionItem(Guid id, string code, string name)
    { Id = id; Code = code; Name = name; }
}
