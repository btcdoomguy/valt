namespace Valt.Infra.Services.CsvImport;

/// <summary>
/// Generates sample CSV templates for transaction import.
/// The template demonstrates all 6 transaction types with example data.
/// </summary>
public interface ICsvTemplateGenerator
{
    /// <summary>
    /// Generates a sample CSV template string with all transaction types.
    /// </summary>
    /// <returns>CSV content as a string</returns>
    string GenerateTemplate();

    /// <summary>
    /// Saves the sample CSV template to a file.
    /// </summary>
    /// <param name="filePath">Path where to save the template file</param>
    void SaveTemplate(string filePath);
}
