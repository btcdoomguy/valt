using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.Commands.CreateAccountGroup;
using Valt.App.Modules.Budget.Accounts.Commands.EditAccountGroup;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccountGroups;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.ManageAccountGroup;

public partial class ManageAccountGroupViewModel : ValtModalValidatorViewModel
{
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly IConfigurationManager? _configurationManager;

    private string? _accountGroupId;

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

    public ManageAccountGroupViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IConfigurationManager configurationManager)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _configurationManager = configurationManager;
    }

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is not null && Parameter is string groupId)
        {
            var groups = await _queryDispatcher!.DispatchAsync(new GetAccountGroupsQuery());
            var group = groups.FirstOrDefault(g => g.Id == groupId);

            if (group is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_AccountGroupNotFound, GetWindow!());
                return;
            }

            _accountGroupId = group.Id;
            Name = group.Name;
            var totalCurrency = AccountGroupTotalCurrency.FromStorageString(group.TotalCurrency);
            SelectedTotalCurrency = totalCurrency.ToStorageString();

            // Validate that the selected currency is still available; fallback to default if not
            var availableCurrencies = _configurationManager?.GetAvailableFiatCurrencies();
            var validatedCurrency = totalCurrency.FallbackToDefaultIfUnavailable(availableCurrencies);
            if (validatedCurrency != totalCurrency)
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
            var totalCurrency = AccountGroupTotalCurrency.FromStorageString(SelectedTotalCurrency);

            // Validate currency is available; fallback if not
            var availableCurrencies = _configurationManager?.GetAvailableFiatCurrencies();
            totalCurrency = totalCurrency.FallbackToDefaultIfUnavailable(availableCurrencies);

            if (_accountGroupId is null)
            {
                var result = await _commandDispatcher!.DispatchAsync(new CreateAccountGroupCommand
                {
                    Name = Name,
                    TotalCurrency = totalCurrency.ToStorageString()
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, result.Error!.Message, GetWindow!());
                    return;
                }
            }
            else
            {
                var result = await _commandDispatcher!.DispatchAsync(new EditAccountGroupCommand
                {
                    GroupId = _accountGroupId,
                    Name = Name,
                    TotalCurrency = totalCurrency.ToStorageString()
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, result.Error!.Message, GetWindow!());
                    return;
                }
            }

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
