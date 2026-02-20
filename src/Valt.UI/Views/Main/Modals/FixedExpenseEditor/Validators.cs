using System.ComponentModel.DataAnnotations;
using Valt.Core.Common;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Modals.FixedExpenseEditor;

internal sealed class RequiredIfAttachedToDefaultAccountAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is FixedExpenseEditorViewModel vm && 
            vm.IsAttachedToDefaultAccount && value is null)
        {
            return new ValidationResult(language.FixedExpenseEditor_Validation_DefaultAccountRequired);
        }
        return ValidationResult.Success;
    }
}

internal sealed class RequiredIfAttachedToCurrencyAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is FixedExpenseEditorViewModel vm && 
            vm.IsAttachedToCurrency && value is null)
        {
            return new ValidationResult(language.FixedExpenseEditor_Validation_CurrencyRequired);
        }
        return ValidationResult.Success;
    }
}

internal sealed class RequiredIfFixedAmountAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is FixedExpenseEditorViewModel vm && 
            vm.IsFixedAmount && (value is null || ((FiatValue)value).Value == 0))
        {
            return new ValidationResult(language.FixedExpenseEditor_Validation_FixedAmountRequired);
        }
        return ValidationResult.Success;
    }
}

internal sealed class RequiredIfVariableAmountAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is FixedExpenseEditorViewModel vm && 
            vm.IsVariableAmount && (value is null || ((FiatValue)value).Value == 0))
        {
            return new ValidationResult(language.FixedExpenseEditor_Validation_RangedAmountMinRequired);
        }
        return ValidationResult.Success;
    }
}

internal sealed class RangedAmountMinLessThanMaxAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is FixedExpenseEditorViewModel vm && vm.IsVariableAmount)
        {
            if (value is decimal min && vm.RangedAmountMax != null && min > vm.RangedAmountMax.Value)
            {
                return new ValidationResult(language.FixedExpenseEditor_Validation_InvalidRangedAmountMinMax);
            }
        }
        return ValidationResult.Success;
    }
}

