using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using Valt.Core.Common;
using Valt.Core.Kernel.Exceptions;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;
using Valt.Core.Modules.Budget.Accounts.Exceptions;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Infra.Modules.Budget.Accounts;
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
    private readonly IAccountRepository? _accountRepository;
    private readonly IAccountGroupRepository? _accountGroupRepository;
    private readonly ITransactionRepository? _transactionRepository;
    private readonly IModalFactory? _modalFactory;
    private readonly IConfigurationManager? _configurationManager;
    private readonly AccountDisplayOrderManager? _accountDisplayOrderManager;

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

    public ManageAccountViewModel(IAccountRepository accountRepository,
        IAccountGroupRepository accountGroupRepository,
        ITransactionRepository transactionRepository,
        IModalFactory modalFactory,
        IConfigurationManager configurationManager,
        AccountDisplayOrderManager accountDisplayOrderManager)
    {
        _accountRepository = accountRepository;
        _accountGroupRepository = accountGroupRepository;
        _transactionRepository = transactionRepository;
        _modalFactory = modalFactory;
        _configurationManager = configurationManager;
        _accountDisplayOrderManager = accountDisplayOrderManager;

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
            var account = await _accountRepository!.GetAccountByIdAsync(new AccountId(accountId));

            if (account is null)
            {
                await MessageBoxHelper.ShowAlertAsync(language.Error_ValidationError, language.Error_AccountNotFound, GetWindow!());
                return;
            }

            _accountId = account.Id;
            _existingGroupId = account.GroupId;
            Name = account.Name;
            CurrencyNickname = account.CurrencyNickname.Value;
            Visible = account.Visible;
            Icon = account.Icon;

            // Set selected group
            if (account.GroupId is not null)
            {
                SelectedGroup = AvailableGroups.FirstOrDefault(g => g.Id == account.GroupId.Value);
            }
            else
            {
                SelectedGroup = AvailableGroups.FirstOrDefault();
            }

            switch (account)
            {
                case BtcAccount btcAccount:
                    InitialBtcAmount = btcAccount.InitialAmount;
                    AccountType = nameof(AccountTypes.Bitcoin);
                    break;
                case FiatAccount fiatAccount:
                    InitialFiatAmount = fiatAccount.InitialAmount;
                    Currency = fiatAccount.FiatCurrency.Code;
                    AccountType = nameof(AccountTypes.Fiat);
                    break;
            }

            if (await _transactionRepository!.HasAnyTransactionAsync(account.Id))
                CanEditAccountStructure = false;
        }
        else
        {
            SelectedGroup = AvailableGroups.FirstOrDefault();
        }
    }

    private async Task LoadAvailableGroupsAsync()
    {
        var groups = await _accountGroupRepository!.GetAllAsync();
        var groupOptions = new List<GroupOption> { new(null, "(None)") };
        groupOptions.AddRange(groups.Select(g => new GroupOption(g.Id.Value, g.Name.Value)));
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
            Account? account;
            if (AccountType == nameof(AccountTypes.Fiat))
            {
                if (_accountId is null)
                {
                    account = FiatAccount.New(AccountName.New(Name), CurrencyNickname, Visible, Icon,
                        FiatCurrency.GetFromCode(Currency), InitialFiatAmount);
                }
                else
                {
                    account = await _accountRepository!.GetAccountByIdAsync(_accountId);

                    if (account is null)
                        throw new EntityNotFoundException(nameof(Account), _accountId);

                    if (account is not FiatAccount fiatAccount)
                        throw new WrongAccountTypeException(account.Id);

                    fiatAccount.Rename(Name);
                    fiatAccount.ChangeCurrencyNickname(CurrencyNickname);
                    fiatAccount.ChangeVisibility(Visible);
                    fiatAccount.ChangeIcon(Icon);
                    fiatAccount.ChangeCurrency(FiatCurrency.GetFromCode(Currency));
                    fiatAccount.ChangeInitialAmount(InitialFiatAmount);
                }
            }
            else
            {
                if (_accountId is null)
                {
                    account = BtcAccount.New(AccountName.New(Name), CurrencyNickname, Visible, Icon, InitialBtcAmount);
                }
                else
                {
                    account = await _accountRepository!.GetAccountByIdAsync(_accountId);

                    if (account is null)
                        throw new EntityNotFoundException(nameof(Account), _accountId);

                    if (account is not BtcAccount btcAccount)
                        throw new WrongAccountTypeException(account.Id);

                    btcAccount.Rename(Name);
                    btcAccount.ChangeCurrencyNickname(CurrencyNickname);
                    btcAccount.ChangeVisibility(Visible);
                    btcAccount.ChangeIcon(Icon);
                    btcAccount.ChangeInitialAmount(InitialBtcAmount);
                }
            }

            // Apply group selection and reset display order if group changed
            var selectedGroupId = SelectedGroup?.Id is not null ? new AccountGroupId(SelectedGroup.Id) : null;
            var groupChanged = _existingGroupId != selectedGroupId;
            var isNewAccount = _accountId is null;

            if (groupChanged || isNewAccount)
            {
                // Get the next display order for the target group
                var newDisplayOrder = _accountDisplayOrderManager!.GetNextDisplayOrderForGroup(
                    selectedGroupId is not null ? new ObjectId(selectedGroupId.Value) : null);
                account.ChangeDisplayOrder(newDisplayOrder);
            }

            account.AssignToGroup(selectedGroupId);

            await _accountRepository!.SaveAccountAsync(account);

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