namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Represents a mapping between a CSV category name and its resolved category ID.
/// Used by the import executor to determine which category to assign to each transaction.
/// </summary>
/// <param name="CsvCategoryName">The category name as it appears in the CSV file</param>
/// <param name="CategoryId">The resolved category ID (existing or to be created)</param>
/// <param name="IsNew">Whether this category needs to be created</param>
public record CsvCategoryMapping(
    string CsvCategoryName,
    string? CategoryId,
    bool IsNew);
