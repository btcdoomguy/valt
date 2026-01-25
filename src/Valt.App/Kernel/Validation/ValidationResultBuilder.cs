namespace Valt.App.Kernel.Validation;

/// <summary>
/// Builder for constructing ValidationResult with multiple errors.
/// </summary>
public sealed class ValidationResultBuilder
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public ValidationResultBuilder AddError(string propertyName, string errorMessage)
    {
        if (!_errors.TryGetValue(propertyName, out var messages))
        {
            messages = [];
            _errors[propertyName] = messages;
        }

        messages.Add(errorMessage);
        return this;
    }

    public ValidationResultBuilder AddErrorIf(bool condition, string propertyName, string errorMessage)
    {
        if (condition)
        {
            AddError(propertyName, errorMessage);
        }

        return this;
    }

    public ValidationResultBuilder AddErrorIfNull<T>(T? value, string propertyName, string errorMessage) where T : class
    {
        if (value is null)
        {
            AddError(propertyName, errorMessage);
        }

        return this;
    }

    public ValidationResultBuilder AddErrorIfNullOrEmpty(string? value, string propertyName, string errorMessage)
    {
        if (string.IsNullOrEmpty(value))
        {
            AddError(propertyName, errorMessage);
        }

        return this;
    }

    public ValidationResultBuilder AddErrorIfNullOrWhiteSpace(string? value, string propertyName, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(propertyName, errorMessage);
        }

        return this;
    }

    public bool HasErrors => _errors.Count > 0;

    public ValidationResult Build()
    {
        if (_errors.Count == 0)
        {
            return ValidationResult.Success();
        }

        var errors = _errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        return ValidationResult.Failure(errors);
    }
}
