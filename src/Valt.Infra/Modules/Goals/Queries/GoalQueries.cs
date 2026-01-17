using Valt.Core.Modules.Goals;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Queries;

internal class GoalQueries : IGoalQueries
{
    private readonly ILocalDatabase _localDatabase;

    public GoalQueries(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public Task<IReadOnlyList<StaleGoalDTO>> GetStaleGoalsAsync()
    {
        var staleGoals = _localDatabase.GetGoals()
            .Find(x => !x.IsUpToDate && x.StateId == (int)GoalStates.Open)
            .Select(entity =>
            {
                var period = (GoalPeriods)entity.PeriodId;
                var refDate = DateOnly.FromDateTime(entity.RefDate);
                var (from, to) = GetPeriodRange(refDate, period);

                return new StaleGoalDTO(
                    entity.Id.ToString(),
                    (GoalTypeNames)entity.GoalTypeNameId,
                    entity.GoalTypeJson,
                    from,
                    to);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<StaleGoalDTO>>(staleGoals);
    }

    private static (DateOnly From, DateOnly To) GetPeriodRange(DateOnly refDate, GoalPeriods period)
    {
        return period switch
        {
            GoalPeriods.Monthly => (
                new DateOnly(refDate.Year, refDate.Month, 1),
                new DateOnly(refDate.Year, refDate.Month, DateTime.DaysInMonth(refDate.Year, refDate.Month))),
            GoalPeriods.Yearly => (
                new DateOnly(refDate.Year, 1, 1),
                new DateOnly(refDate.Year, 12, 31)),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }
}
