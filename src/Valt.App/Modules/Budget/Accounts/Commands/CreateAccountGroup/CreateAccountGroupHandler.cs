using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Accounts.Contracts;

namespace Valt.App.Modules.Budget.Accounts.Commands.CreateAccountGroup;

internal sealed class CreateAccountGroupHandler : ICommandHandler<CreateAccountGroupCommand, CreateAccountGroupResult>
{
    private readonly IAccountGroupRepository _accountGroupRepository;

    public CreateAccountGroupHandler(IAccountGroupRepository accountGroupRepository)
    {
        _accountGroupRepository = accountGroupRepository;
    }

    public async Task<Result<CreateAccountGroupResult>> HandleAsync(
        CreateAccountGroupCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<CreateAccountGroupResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", new Dictionary<string, string[]>
                {
                    { nameof(command.Name), ["Name is required"] }
                }));

        var group = AccountGroup.New(AccountGroupName.New(command.Name));
        await _accountGroupRepository.SaveAsync(group);

        return Result<CreateAccountGroupResult>.Success(new CreateAccountGroupResult(group.Id.Value));
    }
}
