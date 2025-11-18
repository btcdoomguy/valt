using LiteDB;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Kernel.Exceptions;

namespace Valt.Infra.Modules.Budget.FixedExpenses;

public static class Extensions
{
    public static FixedExpense AsDomainObject(this FixedExpenseEntity entity, FixedExpenseRecordEntity? lastFixedExpenseRecord)
    {
        try
        {
            return FixedExpense.Create(entity.Id.ToString(),
                entity.Name,
                entity.DefaultAccountId is not null ? new AccountId(entity.DefaultAccountId.ToString()!) : null,
                new CategoryId(entity.CategoryId.ToString()),
                entity.Currency is not null ? FiatCurrency.GetFromCode(entity.Currency!) : null,
                entity.Ranges.Select(rangeEntity =>
                {
                    if (rangeEntity.FixedAmount is not null)
                        return FixedExpenseRange.CreateFixedAmount(rangeEntity.FixedAmount, rangeEntity.Period,
                            DateOnly.FromDateTime(rangeEntity.PeriodStart), rangeEntity.Day);

                    return FixedExpenseRange.CreateRangedAmount(
                        new RangedFiatValue(rangeEntity.RangedAmountMin!, rangeEntity.RangedAmountMax!),
                        rangeEntity.Period, DateOnly.FromDateTime(rangeEntity.PeriodStart), rangeEntity.Day);
                }).ToList(),
                lastFixedExpenseRecord is not null ? DateOnly.FromDateTime(lastFixedExpenseRecord.ReferenceDate) : null,
                entity.Enabled,
                entity.Version);
        }
        catch (Exception ex)
        {
            throw new BrokenConversionFromDbException(nameof(FixedExpenseEntity), entity.Id.ToString(), ex);
        }
    }

    public static FixedExpenseEntity AsEntity(this FixedExpense fixedExpense)
    {
        return new FixedExpenseEntity()
        {
            Id = new ObjectId(fixedExpense.Id),
            Name = fixedExpense.Name,
            CategoryId = new ObjectId(fixedExpense.CategoryId),
            Currency = fixedExpense.Currency?.Code,
            DefaultAccountId = fixedExpense.DefaultAccountId != null
                ? new ObjectId(fixedExpense.DefaultAccountId)
                : null,
            Enabled = fixedExpense.Enabled,
            Ranges = fixedExpense.Ranges.Select(x => new FixedExpenseRangeEntity()
            {
                Day = x.Day,
                FixedAmount = x.FixedAmount?.Value,
                RangedAmountMin = x.RangedAmount?.Min.Value,
                RangedAmountMax = x.RangedAmount?.Max.Value,
                Period = x.Period,
                PeriodStart = x.PeriodStart.ToValtDateTime(),
            }).ToList(),
            Version = fixedExpense.Version
        };
    }
}