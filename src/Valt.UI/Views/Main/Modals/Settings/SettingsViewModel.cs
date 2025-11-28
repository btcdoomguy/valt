using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Settings;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Views.Main.Modals.ChangePassword;

namespace Valt.UI.Views.Main.Modals.Settings;

public partial class SettingsViewModel : ValtModalViewModel
{
    private readonly CurrencySettings _currencySettings;
    private readonly DisplaySettings _displaySettings;
    private readonly ILocalDatabase _localDatabase;
    private readonly ITransactionTermService _transactionTermService;
    private readonly IModalFactory _modalFactory;

    [ObservableProperty] private string _mainFiatCurrency;
    [ObservableProperty] private bool _showHiddenAccounts;
    [ObservableProperty] private string _currentCulture;

    public static List<ComboBoxValue> AvailableFiatCurrencies
    {
        get
        {
            return FiatCurrency.GetAll().Select(x => new ComboBoxValue($"{x.Code} ({x.Symbol})", x.Code)).ToList();
        }
    }

    public static List<ComboBoxValue> Cultures
    {
        get
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(c => !string.IsNullOrEmpty(c.Name)) //exclude invariant culture
                .OrderBy(c => c.DisplayName)
                .Select(c => new ComboBoxValue(c.DisplayName, c.Name)).ToList();
            
            //prioritize pt-br and en-US
            var ptBr = cultures.SingleOrDefault(x => x.Value == "pt-BR")!;
            var enUs = cultures.SingleOrDefault(x => x.Value == "en-US")!;
            
            cultures.Remove(ptBr);
            cultures.Insert(0, ptBr);
            
            cultures.Remove(enUs);
            cultures.Insert(0, enUs);
            
            return cultures;
        }
    } 

    public SettingsViewModel()
    {
        //Design-time constructor
        MainFiatCurrency = "BRL";
        ShowHiddenAccounts = false;
    }
    
    public SettingsViewModel(CurrencySettings currencySettings,
        DisplaySettings displaySettings,
        ILocalDatabase localDatabase,
        ITransactionTermService transactionTermService,
        IModalFactory modalFactory)
    {
        _currencySettings = currencySettings;
        _displaySettings = displaySettings;
        _localDatabase = localDatabase;
        _transactionTermService = transactionTermService;
        _modalFactory = modalFactory;

        MainFiatCurrency = _currencySettings.MainFiatCurrency;
        ShowHiddenAccounts = _displaySettings.ShowHiddenAccounts;
        CurrentCulture = LocalStorageHelper.LoadCulture();
    }
    
    [RelayCommand]
    private Task ProcessTransactionTermCache()
    {
        _localDatabase!.GetAccountCaches().DeleteAll();

        var transactions = _localDatabase.GetTransactions().FindAll();

        foreach (var transaction in transactions)
        {
            _transactionTermService!.AddEntry(transaction.Name, transaction.CategoryId.ToString(),
                transaction.FromSatAmount, transaction.FromFiatAmount);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ClearAccountCache()
    {
        _localDatabase!.ClearAccountCache();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ChangeDatabasePassword()
    {
        var modal =
            (ChangePasswordView)await _modalFactory.CreateAsync(ApplicationModalNames.ChangePassword, OwnerWindow)!;

        await modal.ShowDialog(OwnerWindow!);
    }
    
    [RelayCommand]
    private async Task Ok()
    {
        _currencySettings.MainFiatCurrency = MainFiatCurrency;
        _currencySettings.Save();

        _displaySettings.ShowHiddenAccounts = ShowHiddenAccounts;
        _displaySettings.Save();

        await LocalStorageHelper.ChangeCulture(CurrentCulture);
        
        CloseDialog?.Invoke(new Response(true));
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