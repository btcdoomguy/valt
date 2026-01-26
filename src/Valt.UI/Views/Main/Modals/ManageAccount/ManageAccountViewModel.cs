using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.Commands.CreateBtcAccount;
using Valt.App.Modules.Budget.Accounts.Commands.CreateFiatAccount;
using Valt.App.Modules.Budget.Accounts.Commands.EditAccount;
using Valt.App.Modules.Budget.Accounts.Queries;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccount;
using Valt.App.Modules.Budget.Transactions.Queries.HasTransactionsForAccount;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Views.Main.Modals.IconSelector;

namespace Valt.UI.Views.Main.Modals.ManageAccount;

public partial class ManageAccountViewModel : ValtModalValidatorViewModel
{
    private readonly ICommandDispatcher? _commandDispatcher;
    private readonly IQueryDispatcher? _queryDispatcher;
    private readonly IModalFactory? _modalFactory;
    private readonly IConfigurationManager? _configurationManager;

    #region Form Data

    private AccountId? _accountId;
    private AccountGroupId? _existingGroupId;

    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Inform a valid account name.")]
    private string _name = string.Empty;

    [ObservableProperty] [NotifyDataErrorInfo] [MaxLength(15, ErrorMessage = "Currency nickname must be 15 characters or less.")]
    private string _currencyNickname = string.Empty;

    [ObservableProperty] private bool _visible;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IconUiWrapper))]
    private Icon _icon = Icon.Empty;

    public IconUIWrapper IconUiWrapper => new(Icon);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCurrencySelector))]
    [NotifyPropertyChangedFor(nameof(ShowBtcFields))]
    private string? _accountType;

    [ObservableProperty] private BtcValue _initialBtcAmount = BtcValue.Empty;

    [ObservableProperty] private FiatValue _initialFiatAmount = FiatValue.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InitialFiatAmount))]
    [NotifyPropertyChangedFor(nameof(SymbolOnRight))]
    [NotifyPropertyChangedFor(nameof(Symbol))]
    private string _currency;

    public bool SymbolOnRight => FiatCurrency.GetFromCode(Currency).SymbolOnRight;
    public string Symbol => FiatCurrency.GetFromCode(Currency).Symbol;

    public bool ShowCurrencySelector => AccountType == nameof(AccountTypes.Fiat);
    public bool ShowBtcFields => AccountType == nameof(AccountTypes.Bitcoin);

    public static List<string> AvailableAccountTypes =>
    [
        nameof(AccountTypes.Fiat),
        nameof(AccountTypes.Bitcoin),
    ];

    public List<string> AvailableCurrencies => _configurationManager?.GetAvailableFiatCurrencies()
        ?? FiatCurrency.GetAll().Select(x => x.Code).ToList();

    [ObservableProperty] private bool _canEditAccountStructure = true;

    [ObservableProperty] private List<GroupOption> _availableGroups = [];

    [ObservableProperty] private GroupOption? _selectedGroup;

    #endregion

    public record GroupOption(string? Id, string Name);

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageAccountViewModel()
    {
        AccountType = AvailableAccountTypes[0];
        Currency = FiatCurrency.Usd.Code;
        Visible = true;

        InitialBtcAmount = BtcValue.New(100000);
        InitialFiatAmount = FiatValue.New(123m);
    }

    public ManageAccountViewModel(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IModalFactory modalFactory,
        IConfigurationManager configurationManager)
    {
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _modalFactory = modalFactory;
        _configurationManager = configurationManager;

        AccountType = AvailableAccountTypes[0];
        Currency = AvailableCurrencies.FirstOrDefault() ?? FiatCurrency.Usd.Code;
        Visible = true;
    }

    public override async Task OnBindParameterAsync()
    {
        // Load available groups
        await LoadAvailableGroupsAsync();

        if (Parameter is not null && Parameter is string accountId)
        {
            var account = await _queryDispatcher!.DispatchAsync(new GetAccountQuery { AccountId = accountId });

            if (account is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_AccountNotFound, GetWindow!());
                return;
            }

            _accountId = new AccountId(account.Id);
            _existingGroupId = account.GroupId is not null ? new AccountGroupId(account.GroupId) : null;
            Name = account.Name;
            CurrencyNickname = account.CurrencyNickname;
            Visible = account.Visible;
            Icon = account.IconId is not null ? Icon.RestoreFromId(account.IconId) : Icon.Empty;

            // Set selected group
            if (account.GroupId is not null)
            {
                SelectedGroup = AvailableGroups.FirstOrDefault(g => g.Id == account.GroupId);
            }
            else
            {
                SelectedGroup = AvailableGroups.FirstOrDefault();
            }

            if (account.IsBtcAccount)
            {
                InitialBtcAmount = account.InitialAmountSats ?? 0;
                AccountType = nameof(AccountTypes.Bitcoin);
            }
            else
            {
                InitialFiatAmount = FiatValue.New(account.InitialAmountFiat ?? 0);
                Currency = account.Currency ?? FiatCurrency.Usd.Code;
                AccountType = nameof(AccountTypes.Fiat);
            }

            var hasTransactions = await _queryDispatcher!.DispatchAsync(
                new HasTransactionsForAccountQuery { AccountId = accountId });
            if (hasTransactions)
                CanEditAccountStructure = false;
        }
        else
        {
            SelectedGroup = AvailableGroups.FirstOrDefault();
        }
    }

    private async Task LoadAvailableGroupsAsync()
    {
        var groups = await _queryDispatcher!.DispatchAsync(new GetAccountGroupsQuery());
        var groupOptions = new List<GroupOption> { new(null, "(None)") };
        groupOptions.AddRange(groups.Select(g => new GroupOption(g.Id, g.Name)));
        AvailableGroups = groupOptions;
    }

    [RelayCommand]
    private async Task IconSelectorOpen()
    {
        var modal =
            (IconSelectorView)await _modalFactory!.CreateAsync(ApplicationModalNames.IconSelector, GetWindow!(),
                Icon.ToString())!;

        var response = await modal.ShowDialog<IconSelectorViewModel.Response?>(GetWindow!());

        if (response is null)
            return;

        if (response.Icon is not null)
        {
            Icon = new Icon(response.Icon.Source, response.Icon.Name, response.Icon.Unicode,
                System.Drawing.Color.FromArgb(response.Color.A, response.Color.R, response.Color.G, response.Color.B));
        }
        else
        {
            Icon = Icon.Empty;
        }
    }

    [RelayCommand]
    private async Task Ok()
    {
        ValidateAllProperties();

        if (!HasErrors)
        {
            if (_accountId is null)
            {
                // Create new account
                if (AccountType == nameof(AccountTypes.Fiat))
                {
                    var result = await _commandDispatcher!.DispatchAsync(new CreateFiatAccountCommand
                    {
                        Name = Name,
                        CurrencyNickname = CurrencyNickname,
                        Visible = Visible,
                        IconId = Icon.ToString(),
                        Currency = Currency,
                        InitialAmount = InitialFiatAmount.Value,
                        GroupId = SelectedGroup?.Id
                    });

                    if (result.IsFailure)
                    {
                        await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                        return;
                    }
                }
                else
                {
                    var result = await _commandDispatcher!.DispatchAsync(new CreateBtcAccountCommand
                    {
                        Name = Name,
                        CurrencyNickname = CurrencyNickname,
                        Visible = Visible,
                        IconId = Icon.ToString(),
                        InitialAmountSats = InitialBtcAmount.Sats,
                        GroupId = SelectedGroup?.Id
                    });

                    if (result.IsFailure)
                    {
                        await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                        return;
                    }
                }
            }
            else
            {
                // Edit existing account
                var result = await _commandDispatcher!.DispatchAsync(new EditAccountCommand
                {
                    AccountId = _accountId.Value,
                    Name = Name,
                    CurrencyNickname = CurrencyNickname,
                    Visible = Visible,
                    IconId = Icon.ToString(),
                    GroupId = SelectedGroup?.Id,
                    Currency = AccountType == nameof(AccountTypes.Fiat) ? Currency : null,
                    InitialAmountFiat = AccountType == nameof(AccountTypes.Fiat) ? InitialFiatAmount.Value : null,
                    InitialAmountSats = AccountType == nameof(AccountTypes.Bitcoin) ? InitialBtcAmount.Sats : null
                });

                if (result.IsFailure)
                {
                    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetWindow!());
                    return;
                }
            }

            // When creating/updating a fiat account, ensure the currency is in the available currencies list
            if (AccountType == nameof(AccountTypes.Fiat))
            {
                _configurationManager?.AddFiatCurrency(Currency);
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