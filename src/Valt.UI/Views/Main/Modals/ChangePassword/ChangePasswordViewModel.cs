using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.DataAccess;
using Valt.UI.Base;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ChangePassword;

public partial class ChangePasswordViewModel : ValtModalValidatorViewModel
{
    private readonly ILocalDatabase _localDatabase;

    #region Form Data
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Old password is required.")]
    [MinLength(6, ErrorMessage = "Old password must be at least 6 characters long.")]
    private string _oldPassword = string.Empty;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    private string _password = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password is required.")]
    [CustomValidation(typeof(ChangePasswordViewModel), "ValidatePassword")]
    private string _confirmPassword = string.Empty;

    #endregion

    public ChangePasswordViewModel(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        try
        {
            _localDatabase.ChangeDatabasePassword(OldPassword, Password);
            
            CloseWindow?.Invoke();
        }
        catch (Exception ex)
        {
            await MessageBoxHelper.ShowErrorAsync("Error", ex.Message, OwnerWindow!);
        }
    }
    
    [RelayCommand]
    private Task CloseEmpty()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }
    
    public record Response();
    
    public static ValidationResult ValidatePassword(string password, ValidationContext context)
    {
        var instance = (ChangePasswordViewModel)context.ObjectInstance;
        var isValid = instance.Password == instance.ConfirmPassword;

        return (isValid ? ValidationResult.Success : new ValidationResult("Passwords do not match."))!;
    }
}