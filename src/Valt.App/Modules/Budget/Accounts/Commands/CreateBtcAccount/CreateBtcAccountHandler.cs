using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.CreateBtcAccount;

internal sealed class CreateBtcAccountHandler : ICommandHandler<CreateBtcAccountCommand, CreateBtcAccountResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountGroupRepository _accountGroupRepository;
    private readonly IValidator<CreateBtcAccountCommand> _validator;

    public CreateBtcAccountHandler(
        IAccountRepository accountRepository,
        IAccountGroupRepository accountGroupRepository,
        IValidator<CreateBtcAccountCommand> validator)
    {
        _accountRepository = accountRepository;
        _accountGroupRepository = accountGroupRepository;
        _validator = validator;
    }

    public async Task<Result<CreateBtcAccountResult>> HandleAsync(CreateBtcAccountCommand command, CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
        {
            return Result<CreateBtcAccountResult>.ValidationFailure(
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
                return Result<CreateBtcAccountResult>.NotFound("AccountGroup", command.GroupId);
            }
        }

        var name = AccountName.New(command.Name);
        var nickname = string.IsNullOrEmpty(command.CurrencyNickname)
            ? AccountCurrencyNickname.Empty
            : AccountCurrencyNickname.New(command.CurrencyNickname);
        var icon = Icon.RestoreFromId(command.IconId);
        BtcValue initialAmount = command.InitialAmountSats;

        var account = BtcAccount.New(name, nickname, command.Visible, icon, initialAmount, groupId);

        await _accountRepository.SaveAccountAsync(account);

        return Result<CreateBtcAccountResult>.Success(new CreateBtcAccountResult(account.Id.Value));
    }
}
