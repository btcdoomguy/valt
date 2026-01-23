using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.Transactions.Queries.DTOs;

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

    [NotifyPropertyChangedFor(nameof(AutoSatAmountCurrentPrice))] [ObservableProperty]
    private long? _autoSatAmount;

    [ObservableProperty] private string _autoSatAmountCurrentPrice;

    public string? FixedExpenseRecordId { get; set; }
    public string? FixedExpenseId { get; set; }
    public string? FixedExpenseName { get; set; }
    public DateOnly? FixedExpenseReferenceDate { get; set; }
    public string? Notes { get; set; }

    public SolidColorBrush AutoSatLineColor { get; private set; }

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
        ToAccountIcon = toAccountIcon;
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

    public void RefreshCurrentAutoSatValue(decimal currentUsdFiatRate, decimal currentBtcRate, string mainFiatCurrency)
    {
        if (AutoSatAmount is null && FromAmountFiat is null && ToAmountFiat is null)
        {
            AutoSatAmountCurrentPrice = string.Empty;
            return;
        }

        var satTotalToUse = (AutoSatAmount ?? FromAmountSats ?? ToAmountSats).GetValueOrDefault();

        if (satTotalToUse == 0)
        {
            AutoSatAmountCurrentPrice = string.Empty;
            return;
        }

        if (satTotalToUse < 0)
            satTotalToUse *= -1;

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
        
        var fiatTotalToUse = FromAmountFiat ?? ToAmountFiat ?? 0;
        if (fiatTotalToUse < 0)
            fiatTotalToUse *= -1;

        if (FromAmountFiat is null)
            AutoSatLineColor = TransactionGridResources.RegularLine;
        else
            AutoSatLineColor = newTotal > fiatTotalToUse
                ? TransactionGridResources.Credit
                : TransactionGridResources.Debt;
        AutoSatAmountCurrentPrice = CurrencyDisplay.FormatFiat(newTotal, mainFiatCurrency);
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
            Icon.RestoreFromId(dto.FromAccountIcon),
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