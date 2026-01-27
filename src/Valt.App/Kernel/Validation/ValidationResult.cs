namespace Valt.App.Kernel.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private ValidationResult(bool isValid, Dictionary<string, string[]> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static ValidationResult Success() => new(true, new Dictionary<string, string[]>());

    public static ValidationResult Failure(Dictionary<string, string[]> errors) => new(false, errors);

    public static ValidationResult Failure(string propertyName, string errorMessage) =>
        new(false, new Dictionary<string, string[]> { { propertyName, [errorMessage] } });

    public static ValidationResult Failure(string propertyName, params string[] errorMessages) =>
        new(false, new Dictionary<string, string[]> { { propertyName, errorMessages } });
}
