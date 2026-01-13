using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Infra.Modules.Budget.Accounts.Queries.DTOs;

namespace Valt.UI.Views.Main.Modals.ImportWizard.Models;

/// <summary>
/// Represents a mapping between a CSV account name and an existing Valt account.
/// </summary>
public partial class AccountMappingItem : ObservableObject
{
    /// <summary>
    /// The account name as it appears in the CSV file.
    /// </summary>
    [ObservableProperty]
    private string _csvAccountName = string.Empty;

    /// <summary>
    /// The matching existing account, if found.
    /// </summary>
    [ObservableProperty]
    private AccountDTO? _existingAccount;

    /// <summary>
    /// Whether this account will be created as new.
    /// </summary>
    [ObservableProperty]
    private bool _isNew;

    /// <summary>
    /// Whether this is a Bitcoin account (inferred from [btc] suffix).
    /// </summary>
    [ObservableProperty]
    private bool _isBtcAccount;

    /// <summary>
    /// The currency code for fiat accounts (inferred from bracket suffix like [USD]).
    /// </summary>
    [ObservableProperty]
    private string? _currency;

    /// <summary>
    /// Creates an AccountMappingItem from a CSV account name.
    /// </summary>
    public static AccountMappingItem Create(string csvAccountName, AccountDTO? existingAccount)
    {
        var isBtc = csvAccountName.Contains("[btc]", StringComparison.OrdinalIgnoreCase);
        string? currency = null;

        // Extract currency from bracket suffix like "[USD]" or "[BRL]"
        if (!isBtc)
        {
            var bracketStart = csvAccountName.LastIndexOf('[');
            var bracketEnd = csvAccountName.LastIndexOf(']');
            if (bracketStart >= 0 && bracketEnd > bracketStart)
            {
                currency = csvAccountName.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).ToUpperInvariant();
                if (currency.Equals("BTC", StringComparison.OrdinalIgnoreCase))
                {
                    isBtc = true;
                    currency = null;
                }
            }
        }

        return new AccountMappingItem
        {
            CsvAccountName = csvAccountName,
            ExistingAccount = existingAccount,
            IsNew = existingAccount is null,
            IsBtcAccount = isBtc,
            Currency = currency
        };
    }

    /// <summary>
    /// Gets a display string for the account type.
    /// </summary>
    public string AccountTypeDisplay => IsBtcAccount ? "BTC" : Currency ?? "Fiat";

    /// <summary>
    /// Gets a display string for the status.
    /// </summary>
    public string StatusDisplay => IsNew ? "New" : "Existing";
}
