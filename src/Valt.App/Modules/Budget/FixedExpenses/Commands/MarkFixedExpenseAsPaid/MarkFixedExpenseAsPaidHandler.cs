using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;

namespace Valt.App.Modules.Budget.FixedExpenses.Commands.MarkFixedExpenseAsPaid;

internal sealed class MarkFixedExpenseAsPaidHandler(IFixedExpenseRecordService fixedExpenseRecordService)
    : ICommandHandler<MarkFixedExpenseAsPaidCommand, Unit>
{
    public async Task<Result<Unit>> HandleAsync(MarkFixedExpenseAsPaidCommand command, CancellationToken ct = default)
    {
        await fixedExpenseRecordService.MarkFixedExpenseAsPaidAsync(
            new TransactionFixedExpenseReference(command.FixedExpenseId, command.ReferenceDate));
        return Result.Success();
    }
}
