namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Localized messages for CSV import operations.
/// Format strings use {0}, {1}, etc. for parameter placeholders.
/// </summary>
public record CsvImportMessages(
    /// <summary>Message shown while creating accounts. No parameters.</summary>
    string CreatingAccounts,

    /// <summary>Message shown after creating an account. {0} = account name.</summary>
    string CreatedAccount,

    /// <summary>Error when account creation fails. {0} = account name, {1} = error message.</summary>
    string FailedToCreateAccount,

    /// <summary>Message shown while creating categories. No parameters.</summary>
    string CreatingCategories,

    /// <summary>Message shown after creating a category. {0} = category name.</summary>
    string CreatedCategory,

    /// <summary>Error when category creation fails. {0} = category name, {1} = error message.</summary>
    string FailedToCreateCategory,

    /// <summary>Message shown while importing transactions. {0} = current, {1} = total.</summary>
    string ImportingTransaction,

    /// <summary>Error when source account not found. {0} = line number, {1} = account name.</summary>
    string AccountNotFound,

    /// <summary>Error when destination account not found. {0} = line number, {1} = account name.</summary>
    string ToAccountNotFound,

    /// <summary>Error when category not found. {0} = line number, {1} = category name.</summary>
    string CategoryNotFound,

    /// <summary>Generic line error. {0} = line number, {1} = error message.</summary>
    string LineError,

    /// <summary>Error when transaction type cannot be determined. {0} = line number.</summary>
    string UnableToDetermineType,

    /// <summary>Message shown after setting initial value for an account. {0} = account name.</summary>
    string SetInitialValue);
