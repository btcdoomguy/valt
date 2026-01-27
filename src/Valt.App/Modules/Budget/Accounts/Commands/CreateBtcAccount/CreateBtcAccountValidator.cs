using Valt.App.Kernel.Validation;

namespace Valt.App.Modules.Budget.Accounts.Commands.CreateBtcAccount;

internal sealed class CreateBtcAccountValidator : IValidator<CreateBtcAccountCommand>
{
    private const int MaxNameLength = 30;
    private const int MaxNicknameLength = 15;

    public ValidationResult Validate(CreateBtcAccountCommand command)
    {
        var builder = new ValidationResultBuilder();

        builder.AddErrorIfNullOrWhiteSpace(command.Name, nameof(command.Name), "Account name is required.");

        if (!string.IsNullOrWhiteSpace(command.Name) && command.Name.Length > MaxNameLength)
        {
            builder.AddError(nameof(command.Name), $"Account name cannot exceed {MaxNameLength} characters.");
        }

        if (!string.IsNullOrEmpty(command.CurrencyNickname) && command.CurrencyNickname.Length > MaxNicknameLength)
        {
            builder.AddError(nameof(command.CurrencyNickname), $"Currency nickname cannot exceed {MaxNicknameLength} characters.");
        }

        builder.AddErrorIfNullOrWhiteSpace(command.IconId, nameof(command.IconId), "Icon is required.");

        return builder.Build();
    }
}
