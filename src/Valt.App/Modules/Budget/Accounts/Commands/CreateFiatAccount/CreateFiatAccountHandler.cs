using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.CreateFiatAccount;

internal sealed class CreateFiatAccountHandler : ICommandHandler<CreateFiatAccountCommand, CreateFiatAccountResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountGroupRepository _accountGroupRepository;
    private readonly IValidator<CreateFiatAccountCommand> _validator;

    public CreateFiatAccountHandler(
        IAccountRepository accountRepository,
        IAccountGroupRepository accountGroupRepository,
        IValidator<CreateFiatAccountCommand> validator)
    {
        _accountRepository = accountRepository;
        _accountGroupRepository = accountGroupRepository;
        _validator = validator;
    }

    public async Task<Result<CreateFiatAccountResult>> HandleAsync(CreateFiatAccountCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<CreateFiatAccountResult>.ValidationFailure(
                new Dictionary<string, string[]>(validation.Errors));
        }

        // Validate group exists if specified
        AccountGroupId? groupId = null;
        if (!string.IsNullOrEmpty(command.GroupId))
        {
            groupId = new AccountGroupId(command.GroupId);
            var group = await _accountGroupRepository.GetByIdAsync(groupId);
            if (group is null)
            {
                return Result<CreateFiatAccountResult>.NotFound("AccountGroup", command.GroupId);
            }
        }

        // Parse currency
        FiatCurrency fiatCurrency;
        try
        {
            fiatCurrency = FiatCurrency.GetFromCode(command.Currency);
        }
        catch (InvalidCurrencyCodeException)
        {
            return Result<CreateFiatAccountResult>.Failure(
                "INVALID_CURRENCY",
                $"Invalid currency: {command.Currency}");
        }

        var name = AccountName.New(command.Name);
        var nickname = string.IsNullOrEmpty(command.CurrencyNickname)
            ? AccountCurrencyNickname.Empty
            : AccountCurrencyNickname.New(command.CurrencyNickname);
        var icon = Icon.RestoreFromId(command.IconId);
        var initialAmount = FiatValue.New(command.InitialAmount);

        var account = FiatAccount.New(name, nickname, command.Visible, icon, fiatCurrency, initialAmount, groupId);

        await _accountRepository.SaveAccountAsync(account);

        return Result<CreateFiatAccountResult>.Success(new CreateFiatAccountResult(account.Id.Value));
    }
}
