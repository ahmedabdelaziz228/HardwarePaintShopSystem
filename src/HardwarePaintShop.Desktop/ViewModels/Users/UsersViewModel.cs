using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwarePaintShop.Application.Interfaces;
using HardwarePaintShop.Domain.Entities;
using System.Collections.ObjectModel;

namespace HardwarePaintShop.Desktop.ViewModels.Users;

public partial class UsersViewModel : BaseViewModel
{
    private readonly IUserService _userService;

    [ObservableProperty] private ObservableCollection<User>   _users = new();
    [ObservableProperty] private ObservableCollection<Role>   _roles = new();
    [ObservableProperty] private User? _selectedUser;

    // Form fields
    [ObservableProperty] private string _formFullName = string.Empty;
    [ObservableProperty] private string _formUsername = string.Empty;
    [ObservableProperty] private string _formPassword = string.Empty;
    [ObservableProperty] private Guid?  _formRoleId;
    [ObservableProperty] private bool   _formIsActive = true;
    [ObservableProperty] private bool   _isEditing;
    [ObservableProperty] private Guid?  _editingId;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _isSuccess;
    [ObservableProperty] private string _searchText = string.Empty;

    private List<User> _allUsers = new();

    public UsersViewModel(IUserService userService)
    {
        _userService = userService;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => FilterUsers();

    private void FilterUsers()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allUsers
            : _allUsers.Where(u =>
                u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Users = new ObservableCollection<User>(filtered);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _allUsers = await _userService.GetUsersAsync();
            Roles = new ObservableCollection<Role>(await _userService.GetRolesAsync());
            FilterUsers();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void NewUser()
    {
        IsEditing = false; EditingId = null; SelectedUser = null;
        FormFullName = string.Empty; FormUsername = string.Empty;
        FormPassword = string.Empty; FormRoleId = null; FormIsActive = true;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void EditUser(User user)
    {
        IsEditing = true; EditingId = user.Id; SelectedUser = user;
        FormFullName = user.FullName; FormUsername = user.Username;
        FormPassword = string.Empty; FormRoleId = user.RoleId; FormIsActive = user.IsActive;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FormFullName)) { ShowError("الاسم الكامل مطلوب."); return; }
        if (string.IsNullOrWhiteSpace(FormUsername))  { ShowError("اسم المستخدم مطلوب."); return; }
        if (!IsEditing && string.IsNullOrWhiteSpace(FormPassword)) { ShowError("كلمة المرور مطلوبة."); return; }
        if (FormRoleId is null) { ShowError("يجب اختيار صلاحية."); return; }

        IsBusy = true;
        try
        {
            if (IsEditing && EditingId.HasValue)
            {
                await _userService.UpdateUserAsync(EditingId.Value, FormFullName, FormUsername, FormRoleId.Value, FormIsActive);
                if (!string.IsNullOrWhiteSpace(FormPassword))
                    await _userService.ChangePasswordAsync(EditingId.Value, FormPassword);
            }
            else
            {
                await _userService.AddUserAsync(FormFullName, FormUsername, FormPassword, FormRoleId.Value);
            }
            ShowSuccess("تم الحفظ ✓"); NewUserCommand.Execute(null); await LoadAsync();
        }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeactivateAsync(User user)
    {
        IsBusy = true;
        try { await _userService.DeactivateUserAsync(user.Id); ShowSuccess("تم تعطيل الحساب ✓"); await LoadAsync(); }
        catch (Exception ex) { ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    private void ShowError(string msg)   { StatusMessage = msg; IsSuccess = false; }
    private void ShowSuccess(string msg) { StatusMessage = msg; IsSuccess = true; }
}
