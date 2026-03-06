using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;

namespace Valt.UI.Views.Main.Tabs.Transactions.Models;

public partial class TransactionViewModel : ObservableObject
{
    public string Id { get; }
    public DateOnly Date { get; }
    public string Name { get; }
    public string CategoryId { get; }
    public string CategoryName { get; }
    public Icon CategoryIcon { get; }
    public string FromAccountId { get; }
    public string FromAccountName { get; }
    public Icon FromAccountIcon { get; }
    public string? ToAccountId { get; }
    public string? ToAccountName { get; }
    public Icon ToAccountIcon { get; }
    public string? FormattedFromAmount { get; }
    public long? FromAmountSats { get; }
    public decimal? FromAmountFiat { get; }
    public string? FormattedToAmount { get; }
    public long? ToAmountSats { get; }
    public decimal? ToAmountFiat { get; }
    public string? FromCurrency { get; }
    public string? ToCurrency { get; }
    public TransactionTransferTypes TransferType { get; }
    public TransactionTypes TransactionType { get; }

    public string? FiatCurrencyCode => TransferType switch
    {
        TransactionTransferTypes.FiatToBitcoin => FromCurrency,  // From is fiat
        TransactionTransferTypes.BitcoinToFiat => ToCurrency,    // To is fiat
        _ => null
    };

    private decimal _currentUsdFiatRate;

    private decimal _currentBtcRate;
    private readonly bool _futureTransaction;
    private bool _isProvisionalSats;

    [NotifyPropertyChangedFor(nameof(AutoSatAmountCurrentPrice))] [ObservableProperty]
    private long? _autoSatAmount;

    [ObservableProperty] private long? _displaySatAmount;

    [ObservableProperty] private string _autoSatAmountCurrentPrice = string.Empty;

    public string? FixedExpenseRecordId { get; set; }
    public string? FixedExpenseId { get; set; }
    public string? FixedExpenseName { get; set; }
    public DateOnly? FixedExpenseReferenceDate { get; set; }
    public string? Notes { get; set; }

    public SolidColorBrush AutoSatLineColor { get; private set; } = TransactionGridResources.RegularLine;
    public SolidColorBrush SatAmountLineColor { get; private set; } = TransactionGridResources.RegularLine;

    public TransactionViewModel(string id, DateOnly date, string name, string categoryId, string categoryName,
        Icon categoryIcon, string fromAccountId, string fromAccountName, Icon fromAccountIcon, string? toAccountId,
        string? toAccountName, Icon? toAccountIcon,
        string? formattedFromAmount, long? fromAmountSats, decimal? fromAmountFiat,
        string? formattedToAmount, long? toAmountSats, decimal? toAmountFiat,
        string? fromCurrency, string? toCurrency,
        TransactionTransferTypes transferType,
        TransactionTypes transactionType,
        long? autoSatAmount, string? fixedExpenseRecordId, string? fixedExpenseId, string? fixedExpenseName,
        DateOnly? fixedExpenseReferenceDate, string? notes, bool futureTransaction)
    {
        Id = id;
        Date = date;
        Name = name;
        CategoryId = categoryId;
        CategoryName = categoryName;
        CategoryIcon = categoryIcon;
        FromAccountId = fromAccountId;
        FromAccountName = fromAccountName;
        FromAccountIcon = fromAccountIcon;
        ToAccountId = toAccountId;
        ToAccountName = toAccountName;
        ToAccountIcon = toAccountIcon ?? Icon.Empty;
        FormattedFromAmount = formattedFromAmount;
        FromAmountSats = fromAmountSats;
        FromAmountFiat = fromAmountFiat;
        FormattedToAmount = formattedToAmount;
        ToAmountSats = toAmountSats;
        ToAmountFiat = toAmountFiat;
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        TransferType = transferType;
        TransactionType = transactionType;
        FixedExpenseRecordId = fixedExpenseRecordId;
        FixedExpenseId = fixedExpenseId;
        FixedExpenseName = fixedExpenseName;
        FixedExpenseReferenceDate = fixedExpenseReferenceDate;
        Notes = notes;
        _autoSatAmount = autoSatAmount;
        _futureTransaction = futureTransaction;
    }

    public string FormattedSummarizedAmount
    {
        get
        {
            if (TransactionType != TransactionTypes.Transfer)
                return $"{FormattedFromAmount}";

            if (FromCurrency == ToCurrency)
                return $"-> {FormattedToAmount}";

            return $"{FormattedFromAmount} -> {FormattedToAmount}";
        }
    }

    public void SetupAutoSatAmount(BtcValue transactionMessageAutoSatAmount)
    {
        AutoSatAmount = transactionMessageAutoSatAmount.Sats;
    }

    public void RefreshCurrentAutoSatValue(decimal currentUsdFiatRate, decimal currentBtcRate, string mainFiatCurrency,
        IReadOnlyDictionary<string, decimal>? fiatRates = null)
    {
        if (AutoSatAmount is null && FromAmountFiat is null && ToAmountFiat is null)
        {
            AutoSatAmountCurrentPrice = string.Empty;
            DisplaySatAmount = null;
            return;
        }

        var satTotalToUse = (AutoSatAmount ?? FromAmountSats ?? ToAmountSats).GetValueOrDefault();

        // If no actual sats, try provisional calculation
        if (satTotalToUse == 0 && AutoSatAmount is null)
        {
            var provisionalSats = CalculateProvisionalSats(currentBtcRate, fiatRates);
            if (provisionalSats is not null)
            {
                _isProvisionalSats = true;
                DisplaySatAmount = provisionalSats.Value;
                SatAmountLineColor = TransactionGridResources.FutureLine;
                satTotalToUse = Math.Abs(provisionalSats.Value);
            }
            else
            {
                _isProvisionalSats = false;
                DisplaySatAmount = null;
                AutoSatAmountCurrentPrice = string.Empty;
                return;
            }
        }
        else
        {
            _isProvisionalSats = false;
            DisplaySatAmount = AutoSatAmount ?? FromAmountSats ?? ToAmountSats;
            SatAmountLineColor = TransactionGridResources.RegularLine;
        }

        if (satTotalToUse < 0)
            satTotalToUse *= -1;

        if (satTotalToUse == 0)
        {
            AutoSatAmountCurrentPrice = string.Empty;
            return;
        }

        _currentUsdFiatRate = currentUsdFiatRate;
        _currentBtcRate = currentBtcRate;

        if (_currentBtcRate <= 0 || _currentUsdFiatRate <= 0)
        {
            AutoSatAmountCurrentPrice = string.Empty;
            return;
        }

        var btcPrice = _currentBtcRate * _currentUsdFiatRate;

        var autoSatAmountParsed = BtcValue.New(satTotalToUse);

        var newTotal = FiatValue.New(btcPrice * autoSatAmountParsed.Btc);

        if (_isProvisionalSats)
        {
            AutoSatLineColor = TransactionGridResources.FutureLine;
        }
        else
        {
            var fiatTotalToUse = FromAmountFiat ?? ToAmountFiat ?? 0;
            if (fiatTotalToUse < 0)
                fiatTotalToUse *= -1;

            if (FromAmountFiat is null)
                AutoSatLineColor = TransactionGridResources.RegularLine;
            else
                AutoSatLineColor = newTotal > fiatTotalToUse
                    ? TransactionGridResources.Credit
                    : TransactionGridResources.Debt;
        }

        AutoSatAmountCurrentPrice = CurrencyDisplay.FormatFiat(newTotal, mainFiatCurrency);
    }

    private long? CalculateProvisionalSats(decimal currentBtcRate, IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        if (TransferType is not (TransactionTransferTypes.Fiat or TransactionTransferTypes.FiatToFiat))
            return null;

        if (currentBtcRate <= 0 || fiatRates is null || FromCurrency is null)
            return null;

        if (!fiatRates.TryGetValue(FromCurrency, out var currencyToUsdRate) || currencyToUsdRate <= 0)
            return null;

        var fiatAmount = FromAmountFiat ?? 0;
        if (fiatAmount == 0)
            return null;

        fiatAmount = Math.Abs(fiatAmount);

        var btcAmount = fiatAmount / (currentBtcRate * currencyToUsdRate);
        var btcValue = BtcValue.ParseBitcoin(btcAmount);

        return btcValue.Sats;
    }

    public SolidColorBrush AmountColor
    {
        get
        {
            if (TransferType is TransactionTransferTypes.Bitcoin or TransactionTransferTypes.Fiat)
            {
                return TransactionType == TransactionTypes.Credit
                    ? TransactionGridResources.Credit
                    : TransactionGridResources.Debt;
            }

            return TransactionGridResources.Transfer;
        }
    }

    public SolidColorBrush LineColor =>
        _futureTransaction ? TransactionGridResources.FutureLine : TransactionGridResources.RegularLine;

    public static IEnumerable<TransactionViewModel> Parse(List<TransactionDTO> dtoList, DateOnly today)
    {
        return dtoList.Select(dto => new TransactionViewModel(dto.Id, dto.Date, dto.Name, dto.CategoryId,
            dto.CategoryName,
            dto.CategoryIcon is not null ? Icon.RestoreFromId(dto.CategoryIcon) : Icon.Empty,
            dto.FromAccountId,
            dto.FromAccountName,
            dto.FromAccountIcon is not null ? Icon.RestoreFromId(dto.FromAccountIcon) : Icon.Empty,
            dto.ToAccountId,
            dto.ToAccountName,
            dto.ToAccountIcon is not null ? Icon.RestoreFromId(dto.ToAccountIcon) : Icon.Empty,
            dto.FormattedFromAmount,
            dto.FromAmountSats,
            dto.FromAmountFiat,
            dto.FormattedToAmount,
            dto.ToAmountSats,
            dto.ToAmountFiat,
            dto.FromAccountCurrency, dto.ToAccountCurrency,
            Enum.Parse<TransactionTransferTypes>(dto.TransferType),
            Enum.Parse<TransactionTypes>(dto.TransactionType),
            dto.AutoSatAmount,
            dto.FixedExpenseRecordId,
            dto.FixedExpenseId,
            dto.FixedExpenseName,
            dto.FixedExpenseReferenceDate,
            dto.Notes,
            dto.Date > today));
    }
}