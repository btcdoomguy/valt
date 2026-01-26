using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Goals;
using Valt.Infra.DataAccess;

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
                    entity.GoalTypeNameId,
                    entity.GoalTypeJson,
                    from,
                    to);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<StaleGoalDTO>>(staleGoals);
    }

    public Task<IReadOnlyList<GoalDTO>> GetGoalsAsync(DateOnly? filterDate)
    {
        var allGoals = _localDatabase.GetGoals().FindAll().ToList();

        IEnumerable<GoalEntity> goals = allGoals;

        if (filterDate.HasValue)
        {
            var date = filterDate.Value;
            goals = allGoals.Where(g =>
            {
                var refDate = DateOnly.FromDateTime(g.RefDate);
                var period = (GoalPeriods)g.PeriodId;
                var range = GetPeriodRange(refDate, period);
                return date >= range.From && date <= range.To;
            });
        }

        var result = goals
            .OrderBy(g => GetGoalSortOrder(g))
            .ThenBy(g => g.GoalTypeNameId)
            .ThenBy(g => g.RefDate)
            .Select(MapToDto)
            .ToList();

        return Task.FromResult<IReadOnlyList<GoalDTO>>(result);
    }

    public Task<GoalDTO?> GetGoalAsync(string goalId)
    {
        var entity = _localDatabase.GetGoals().FindById(new ObjectId(goalId));

        if (entity is null)
            return Task.FromResult<GoalDTO?>(null);

        return Task.FromResult<GoalDTO?>(MapToDto(entity));
    }

    private static int GetGoalSortOrder(GoalEntity goal)
    {
        var state = (GoalStates)goal.StateId;
        var period = (GoalPeriods)goal.PeriodId;

        return state switch
        {
            GoalStates.Open => period == GoalPeriods.Monthly ? 0 : 1,
            GoalStates.Completed => 2,
            GoalStates.Failed => 3,
            _ => 4
        };
    }

    private static GoalDTO MapToDto(GoalEntity entity)
    {
        return new GoalDTO
        {
            Id = entity.Id.ToString(),
            RefDate = DateOnly.FromDateTime(entity.RefDate),
            Period = entity.PeriodId,
            Progress = entity.Progress,
            IsUpToDate = entity.IsUpToDate,
            LastUpdatedAt = entity.LastUpdatedAt,
            State = entity.StateId,
            GoalType = MapGoalTypeToDto((GoalTypeNames)entity.GoalTypeNameId, entity.GoalTypeJson)
        };
    }

    private static GoalTypeOutputDTO MapGoalTypeToDto(GoalTypeNames typeName, string json)
    {
        return typeName switch
        {
            GoalTypeNames.StackBitcoin => MapStackBitcoinGoalType(json),
            GoalTypeNames.SpendingLimit => MapSpendingLimitGoalType(json),
            GoalTypeNames.Dca => MapDcaGoalType(json),
            GoalTypeNames.IncomeFiat => MapIncomeFiatGoalType(json),
            GoalTypeNames.IncomeBtc => MapIncomeBtcGoalType(json),
            GoalTypeNames.ReduceExpenseCategory => MapReduceExpenseCategoryGoalType(json),
            GoalTypeNames.BitcoinHodl => MapBitcoinHodlGoalType(json),
            _ => throw new NotSupportedException($"Goal type {typeName} is not supported")
        };
    }

    private static StackBitcoinGoalTypeOutputDTO MapStackBitcoinGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<StackBitcoinGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize StackBitcoinGoalType");
        return new StackBitcoinGoalTypeOutputDTO
        {
            TargetSats = dto.TargetSats,
            CalculatedSats = dto.CalculatedSats
        };
    }

    private static SpendingLimitGoalTypeOutputDTO MapSpendingLimitGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<SpendingLimitGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize SpendingLimitGoalType");
        return new SpendingLimitGoalTypeOutputDTO
        {
            TargetAmount = dto.TargetAmount,
            CalculatedSpending = dto.CalculatedSpending
        };
    }

    private static DcaGoalTypeOutputDTO MapDcaGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<DcaGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize DcaGoalType");
        return new DcaGoalTypeOutputDTO
        {
            TargetPurchaseCount = dto.TargetPurchaseCount,
            CalculatedPurchaseCount = dto.CalculatedPurchaseCount
        };
    }

    private static IncomeFiatGoalTypeOutputDTO MapIncomeFiatGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<IncomeFiatGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize IncomeFiatGoalType");
        return new IncomeFiatGoalTypeOutputDTO
        {
            TargetAmount = dto.TargetAmount,
            CalculatedIncome = dto.CalculatedIncome
        };
    }

    private static IncomeBtcGoalTypeOutputDTO MapIncomeBtcGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<IncomeBtcGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize IncomeBtcGoalType");
        return new IncomeBtcGoalTypeOutputDTO
        {
            TargetSats = dto.TargetSats,
            CalculatedSats = dto.CalculatedSats
        };
    }

    private static ReduceExpenseCategoryGoalTypeOutputDTO MapReduceExpenseCategoryGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<ReduceExpenseCategoryGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize ReduceExpenseCategoryGoalType");
        return new ReduceExpenseCategoryGoalTypeOutputDTO
        {
            TargetAmount = dto.TargetAmount,
            CategoryId = dto.CategoryId,
            CategoryName = dto.CategoryName,
            CalculatedSpending = dto.CalculatedSpending
        };
    }

    private static BitcoinHodlGoalTypeOutputDTO MapBitcoinHodlGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<BitcoinHodlGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize BitcoinHodlGoalType");
        return new BitcoinHodlGoalTypeOutputDTO
        {
            MaxSellableSats = dto.MaxSellableSats,
            CalculatedSoldSats = dto.CalculatedSoldSats
        };
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
