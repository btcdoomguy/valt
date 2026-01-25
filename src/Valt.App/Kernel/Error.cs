namespace Valt.App.Kernel;

/// <summary>
/// Represents an error with a code, message, and optional validation errors.
/// </summary>
public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public Error(string code, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        Code = code;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public bool HasValidationErrors => ValidationErrors is { Count: > 0 };

    public static Error NotFound(string entityType, string id) =>
        new($"{entityType.ToUpperInvariant()}_NOT_FOUND", $"{entityType} with ID '{id}' was not found.");

    public static Error Validation(string message, Dictionary<string, string[]> errors) =>
        new("VALIDATION_FAILED", message, errors);

    public static Error Conflict(string message) =>
        new("CONFLICT", message);

    public static Error Internal(string message) =>
        new("INTERNAL_ERROR", message);

    public override string ToString() =>
        HasValidationErrors
            ? $"[{Code}] {Message} - Validation Errors: {string.Join(", ", ValidationErrors!.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}")))}"
            : $"[{Code}] {Message}";
}
