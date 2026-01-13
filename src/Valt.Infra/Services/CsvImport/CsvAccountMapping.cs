namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Represents a mapping between a CSV account name and its resolved account ID.
/// Used by the import executor to determine which account to use for each transaction.
/// </summary>
/// <param name="CsvAccountName">The account name as it appears in the CSV file</param>
/// <param name="AccountId">The resolved account ID (existing or to be created)</param>
/// <param name="IsNew">Whether this account needs to be created</param>
/// <param name="IsBtcAccount">Whether this is a Bitcoin account</param>
/// <param name="Currency">The currency code for fiat accounts (e.g., "USD", "BRL")</param>
public record CsvAccountMapping(
    string CsvAccountName,
    string? AccountId,
    bool IsNew,
    bool IsBtcAccount,
    string? Currency);
