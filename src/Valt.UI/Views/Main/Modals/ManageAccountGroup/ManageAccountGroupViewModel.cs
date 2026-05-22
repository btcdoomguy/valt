using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageAccountGroup;

public partial class ManageAccountGroupViewModel : ValtModalValidatorViewModel
{
    private readonly IAccountGroupRepository? _accountGroupRepository;
    private readonly IConfigurationManager? _configurationManager;

    private AccountGroupId? _accountGroupId;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Inform a valid group name.")]
    [MaxLength(50, ErrorMessage = "Group name must be 50 characters or less.")]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _selectedTotalCurrency = "DEFAULT";

    public List<ComboBoxValue> AvailableTotalCurrencies
    {
        get
        {
            var currencies = new List<ComboBoxValue>
            {
                new(language.ManageAccountGroup_DefaultFiatCurrency, "DEFAULT"),
                new("Bitcoin", "BTC")
            };

            var availableFiat = _configurationManager?.GetAvailableFiatCurrencies()
                ?? FiatCurrency.GetAll().Select(x => x.Code).ToList();

            foreach (var code in availableFiat.OrderBy(c => c))
            {
                var currency = FiatCurrency.GetFromCode(code);
                currencies.Add(new($"{currency.Code} ({currency.Symbol})", currency.Code));
            }

            return currencies;
        }
    }

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageAccountGroupViewModel()
    {
    }

    public ManageAccountGroupViewModel(IAccountGroupRepository accountGroupRepository, IConfigurationManager configurationManager)
    {
        _accountGroupRepository = accountGroupRepository;
        _configurationManager = configurationManager;
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
            SelectedTotalCurrency = group.TotalCurrency.ToStorageString();

            // Validate that the selected currency is still available; fallback to default if not
            var availableCurrencies = _configurationManager?.GetAvailableFiatCurrencies();
            var validatedCurrency = group.TotalCurrency.FallbackToDefaultIfUnavailable(availableCurrencies);
            if (validatedCurrency != group.TotalCurrency)
            {
                SelectedTotalCurrency = validatedCurrency.ToStorageString();
            }
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            AccountGroup group;
            var totalCurrency = AccountGroupTotalCurrency.FromStorageString(SelectedTotalCurrency);

            // Validate currency is available; fallback if not
            var availableCurrencies = _configurationManager?.GetAvailableFiatCurrencies();
            totalCurrency = totalCurrency.FallbackToDefaultIfUnavailable(availableCurrencies);

            if (_accountGroupId is null)
            {
                group = AccountGroup.New(AccountGroupName.New(Name));
                group.ChangeTotalCurrency(totalCurrency);
            }
            else
            {
                group = (await _accountGroupRepository!.GetByIdAsync(_accountGroupId))!;
                group.Rename(AccountGroupName.New(Name));
                group.ChangeTotalCurrency(totalCurrency);
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
