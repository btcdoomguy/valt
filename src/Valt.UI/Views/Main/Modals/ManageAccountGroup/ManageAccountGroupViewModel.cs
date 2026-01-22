using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageAccountGroup;

public partial class ManageAccountGroupViewModel : ValtModalValidatorViewModel
{
    private readonly IAccountGroupRepository? _accountGroupRepository;

    private AccountGroupId? _accountGroupId;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Inform a valid group name.")]
    [MaxLength(50, ErrorMessage = "Group name must be 50 characters or less.")]
    private string _name = string.Empty;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageAccountGroupViewModel()
    {
    }

    public ManageAccountGroupViewModel(IAccountGroupRepository accountGroupRepository)
    {
        _accountGroupRepository = accountGroupRepository;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not null && Parameter is string groupId)
        {
            var group = await _accountGroupRepository!.GetByIdAsync(new AccountGroupId(groupId));

            if (group is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_AccountGroupNotFound, GetWindow!());
                return;
            }

            _accountGroupId = group.Id;
            Name = group.Name;
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            AccountGroup group;

            if (_accountGroupId is null)
            {
                group = AccountGroup.New(AccountGroupName.New(Name));
            }
            else
            {
                group = (await _accountGroupRepository!.GetByIdAsync(_accountGroupId))!;
                group.Rename(AccountGroupName.New(Name));
            }

            await _accountGroupRepository!.SaveAsync(group);

            CloseDialog?.Invoke(new Response(true));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record Response(bool Ok);
}
