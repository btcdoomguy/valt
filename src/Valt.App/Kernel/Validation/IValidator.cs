namespace Valt.App.Kernel.Validation;

/// <summary>
/// Validates instances of type T.
/// </summary>
public interface IValidator<in T>
{
    ValidationResult Validate(T instance);
}
