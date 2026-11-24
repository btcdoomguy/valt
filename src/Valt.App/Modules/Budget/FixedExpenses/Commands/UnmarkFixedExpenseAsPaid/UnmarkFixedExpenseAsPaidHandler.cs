using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.UnmarkFixedExpenseAsPaid;

internal sealed class UnmarkFixedExpenseAsPaidHandler(IFixedExpenseRecordService fixedExpenseRecordService)
    : ICommandHandler<UnmarkFixedExpenseAsPaidCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(UnmarkFixedExpenseAsPaidCommand command, CancellationToken ct = default)
    {
        await fixedExpenseRecordService.UnmarkFixedExpenseAsPaidAsync(
            new TransactionFixedExpenseReference(command.FixedExpenseId, command.ReferenceDate));
        return Result.Success();
    }
}
