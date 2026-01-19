using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Settings;
using FontScale = Valt.Infra.Settings.FontScale;
using Valt.Infra.TransactionTerms;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.LocalStorage;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.Services.FontScaling;
using Valt.UI.Services.Theming;
using Valt.UI.Views.Main.Modals.ChangePassword;

namespace Valt.UI.Views.Main.Modals.Settings;

public partial class SettingsViewModel : ValtModalViewModel
{
    private readonly CurrencySettings _currencySettings;
    private readonly DisplaySettings _displaySettings;
    private readonly ILocalDatabase _localDatabase;
    private readonly ITransactionTermService _transactionTermService;
    private readonly IModalFactory _modalFactory;
    private readonly ILocalStorageService _localStorageService;
    private readonly IConfigurationManager? _configurationManager;
    private readonly IThemeService? _themeService;
    private readonly IFontScaleService? _fontScaleService;

    [ObservableProperty] private string _mainFiatCurrency;
    [ObservableProperty] private bool _showHiddenAccounts;
    [ObservableProperty] private string _currentCulture;
    [ObservableProperty] private ThemeDefinition? _selectedTheme;
    [ObservableProperty] private FontScaleItem? _selectedFontScale;

    private List<string> _initialSelectedCurrencies = new();
    private HashSet<string> _currenciesInUse = new();

    /// <summary>
    /// All available fiat currencies (excluding USD which is mandatory)
    /// </summary>
    public AvaloniaList<FiatCurrencyItem> AllFiatCurrencies { get; } = new(FiatCurrencyItem.GetAllExceptUsd());

    /// <summary>
    /// Selected fiat currencies (excluding USD which is mandatory)
    /// </summary>
    public AvaloniaList<FiatCurrencyItem> SelectedFiatCurrencies { get; } = new();

    public List<ComboBoxValue> AvailableFiatCurrencies
    {
        get
        {
            var currencies = _configurationManager?.GetAvailableFiatCurrencies()
                ?? FiatCurrency.GetAll().Select(x => x.Code).ToList();
            return currencies.Select(code =>
            {
                var currency = FiatCurrency.GetFromCode(code);
                return new ComboBoxValue($"{currency.Code} ({currency.Symbol})", currency.Code);
            }).ToList();
        }
    }

    public IReadOnlyList<ThemeDefinition> AvailableThemes => _themeService?.AvailableThemes ?? Array.Empty<ThemeDefinition>();

    public static IReadOnlyList<FontScaleItem> AvailableFontScales => FontScaleItem.All;

    public static List<ComboBoxValue> Cultures
    {
        get
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(c => !string.IsNullOrEmpty(c.Name)) //exclude invariant culture
                .OrderBy(c => c.DisplayName)
                .Select(c => new ComboBoxValue(c.DisplayName, c.Name)).ToList();

            //prioritize pt-br, en-US and es (Spanish)
            var ptBr = cultures.SingleOrDefault(x => x.Value == "pt-BR")!;
            var enUs = cultures.SingleOrDefault(x => x.Value == "en-US")!;
            var es = cultures.SingleOrDefault(x => x.Value == "es")!;

            cultures.Remove(ptBr);
            cultures.Insert(0, ptBr);

            cultures.Remove(es);
            cultures.Insert(0, es);

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
        CurrentCulture = "en-US";
        SelectedFontScale = FontScaleItem.All.First(x => x.Scale == FontScale.Medium);

        SelectedFiatCurrencies.CollectionChanged += OnSelectedFiatCurrenciesChanged;
    }

    public SettingsViewModel(CurrencySettings currencySettings,
        DisplaySettings displaySettings,
        ILocalDatabase localDatabase,
        ITransactionTermService transactionTermService,
        IModalFactory modalFactory,
        ILocalStorageService localStorageService,
        IConfigurationManager configurationManager,
        IThemeService themeService,
        IFontScaleService fontScaleService)
    {
        _currencySettings = currencySettings;
        _displaySettings = displaySettings;
        _localDatabase = localDatabase;
        _transactionTermService = transactionTermService;
        _modalFactory = modalFactory;
        _localStorageService = localStorageService;
        _configurationManager = configurationManager;
        _themeService = themeService;
        _fontScaleService = fontScaleService;

        MainFiatCurrency = _currencySettings.MainFiatCurrency;
        ShowHiddenAccounts = _displaySettings.ShowHiddenAccounts;
        CurrentCulture = _localStorageService.LoadCulture();
        SelectedTheme = _themeService.AvailableThemes.FirstOrDefault(t => t.Name == _themeService.CurrentTheme)
                        ?? _themeService.AvailableThemes.First();
        SelectedFontScale = FontScaleItem.All.FirstOrDefault(x => x.Scale == _displaySettings.FontScale)
                            ?? FontScaleItem.All.First(x => x.Scale == FontScale.Medium);

        // Initialize currencies
        InitializeFiatCurrencies();
        SelectedFiatCurrencies.CollectionChanged += OnSelectedFiatCurrenciesChanged;
    }

    private void InitializeFiatCurrencies()
    {
        // Get currently configured currencies (excluding USD)
        var configuredCurrencies = _configurationManager?.GetAvailableFiatCurrencies()
            .Where(c => c != "USD")
            .ToList() ?? new List<string>();

        _initialSelectedCurrencies = new List<string>(configuredCurrencies);

        // Get currencies in use (cannot be removed)
        _currenciesInUse = _configurationManager?.GetCurrenciesInUse()
            .Where(c => c != "USD")
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

        // Select the configured currencies in the list
        foreach (var item in AllFiatCurrencies.Where(c => configuredCurrencies.Contains(c.Code, StringComparer.OrdinalIgnoreCase)))
        {
            SelectedFiatCurrencies.Add(item);
        }
    }

    private async void OnSelectedFiatCurrenciesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (FiatCurrencyItem item in e.OldItems)
            {
                // Check if currency is in use
                if (_currenciesInUse.Contains(item.Code))
                {
                    // Re-add the item to prevent removal
                    SelectedFiatCurrencies.Add(item);

                    await MessageBoxHelper.ShowAlertAsync(
                        language.Error,
                        string.Format(language.Settings_FiatCurrencies_CannotRemove, item.Code),
                        OwnerWindow!);
                }
            }
        }
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
        // Check if new currencies were added
        var currentCurrencies = SelectedFiatCurrencies.Select(c => c.Code).ToList();
        var newCurrencies = currentCurrencies.Except(_initialSelectedCurrencies, StringComparer.OrdinalIgnoreCase).ToList();

        if (newCurrencies.Any())
        {
            var confirmed = await MessageBoxHelper.ShowQuestionAsync(
                language.Settings_FiatCurrencies_ConfirmAdd_Title,
                language.Settings_FiatCurrencies_ConfirmAdd_Message,
                OwnerWindow!);

            if (!confirmed)
                return;
        }

        // Save currency settings (always include USD)
        var currenciesToSave = new List<string> { "USD" };
        currenciesToSave.AddRange(currentCurrencies);
        _configurationManager?.SetAvailableFiatCurrencies(currenciesToSave);

        _currencySettings.MainFiatCurrency = MainFiatCurrency;
        _currencySettings.Save();

        _displaySettings.ShowHiddenAccounts = ShowHiddenAccounts;

        // Apply and save font scale if changed
        if (SelectedFontScale != null && _fontScaleService != null)
        {
            if (_fontScaleService.CurrentScale != SelectedFontScale.Scale)
            {
                _fontScaleService.ApplyScale(SelectedFontScale.Scale);
                _displaySettings.FontScale = SelectedFontScale.Scale;
            }
        }

        _displaySettings.Save();

        // Apply and save theme if changed
        if (SelectedTheme != null && _themeService != null)
        {
            if (_themeService.CurrentTheme != SelectedTheme.Name)
            {
                _themeService.ApplyTheme(SelectedTheme.Name);
                _themeService.SaveTheme(SelectedTheme.Name);
            }
        }

        await _localStorageService.ChangeCultureAsync(CurrentCulture);

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