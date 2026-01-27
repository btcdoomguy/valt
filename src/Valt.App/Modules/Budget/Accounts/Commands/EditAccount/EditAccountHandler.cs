using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.EditAccount;

internal sealed class EditAccountHandler : ICommandHandler<EditAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountGroupRepository _accountGroupRepository;
    private readonly IValidator<EditAccountCommand> _validator;

    public EditAccountHandler(
        IAccountRepository accountRepository,
        IAccountGroupRepository accountGroupRepository,
        IValidator<EditAccountCommand> validator)
    {
        _accountRepository = accountRepository;
        _accountGroupRepository = accountGroupRepository;
        _validator = validator;
    }

    public async Task<Result<Unit>> HandleAsync(EditAccountCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<Unit>.ValidationFailure(
                new Dictionary<string, string[]>(validation.Errors));
        }

        var accountId = new AccountId(command.AccountId);
        var account = await _accountRepository.GetAccountByIdAsync(accountId);

        if (account is null)
        {
            return Result<Unit>.NotFound("Account", command.AccountId);
        }

        // Validate group exists if specified
        AccountGroupId? groupId = null;
        if (!string.IsNullOrEmpty(command.GroupId))
        {
            groupId = new AccountGroupId(command.GroupId);
            var group = await _accountGroupRepository.GetByIdAsync(groupId);
            if (group is null)
            {
                return Result<Unit>.NotFound("AccountGroup", command.GroupId);
            }
        }

        // Update common properties
        var name = AccountName.New(command.Name);
        var nickname = string.IsNullOrEmpty(command.CurrencyNickname)
            ? AccountCurrencyNickname.Empty
            : AccountCurrencyNickname.New(command.CurrencyNickname);
        var icon = Icon.RestoreFromId(command.IconId);

        account.Rename(name);
        account.ChangeCurrencyNickname(nickname);
        account.ChangeVisibility(command.Visible);
        account.ChangeIcon(icon);
        account.AssignToGroup(groupId);

        // Update type-specific properties
        if (account is FiatAccount fiatAccount)
        {
            if (!string.IsNullOrEmpty(command.Currency))
            {
                FiatCurrency fiatCurrency;
                try
                {
                    fiatCurrency = FiatCurrency.GetFromCode(command.Currency);
                }
                catch (InvalidCurrencyCodeException)
                {
                    return Result<Unit>.Failure(
                        "INVALID_CURRENCY",
                        $"Invalid currency: {command.Currency}");
                }
                fiatAccount.ChangeCurrency(fiatCurrency);
            }

            if (command.InitialAmountFiat.HasValue)
            {
                fiatAccount.ChangeInitialAmount(FiatValue.New(command.InitialAmountFiat.Value));
            }
        }
        else if (account is BtcAccount btcAccount)
        {
            if (command.InitialAmountSats.HasValue)
            {
                BtcValue initialAmount = command.InitialAmountSats.Value;
                btcAccount.ChangeInitialAmount(initialAmount);
            }
        }

        await _accountRepository.SaveAccountAsync(account);

        return Result.Success();
    }
}
