using Valt.Core.Common;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;

namespace Valt.Tests.Builders;

public class GoalBuilder
{
    private GoalId _id = new();
    private DateOnly _refDate = new DateOnly(2024, 1, 15);
    private GoalPeriods _period = GoalPeriods.Monthly;
    private IGoalType _goalType = new StackBitcoinGoalType(BtcValue.ParseSats(1_000_000));
    private decimal _progress = 0m;
    private bool _isUpToDate = false;
    private DateTime _lastUpdatedAt = DateTime.MinValue;
    private GoalStates _state = GoalStates.Open;
    private int _version = 1;

    public GoalBuilder WithId(GoalId id)
    {
        _id = id;
        return this;
    }

    public GoalBuilder WithRefDate(DateOnly refDate)
    {
        _refDate = refDate;
        return this;
    }

    public GoalBuilder WithPeriod(GoalPeriods period)
    {
        _period = period;
        return this;
    }

    public GoalBuilder WithGoalType(IGoalType goalType)
    {
        _goalType = goalType;
        return this;
    }

    public GoalBuilder WithProgress(decimal progress)
    {
        _progress = progress;
        return this;
    }

    public GoalBuilder WithIsUpToDate(bool isUpToDate)
    {
        _isUpToDate = isUpToDate;
        return this;
    }

    public GoalBuilder WithLastUpdatedAt(DateTime lastUpdatedAt)
    {
        _lastUpdatedAt = lastUpdatedAt;
        return this;
    }

    public GoalBuilder WithState(GoalStates state)
    {
        _state = state;
        return this;
    }

    public GoalBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public Goal Build()
    {
        return Goal.Create(_id, _refDate, _period, _goalType, _progress, _isUpToDate, _lastUpdatedAt, _state, _version);
    }

    public static GoalBuilder AGoal() => new();

    public static GoalBuilder AStackBitcoinGoal(long targetSats = 1_000_000) =>
        new GoalBuilder()
            .WithGoalType(new StackBitcoinGoalType(BtcValue.ParseSats(targetSats)));

    public static GoalBuilder AMonthlyGoal() =>
        new GoalBuilder()
            .WithPeriod(GoalPeriods.Monthly);

    public static GoalBuilder AYearlyGoal() =>
        new GoalBuilder()
            .WithPeriod(GoalPeriods.Yearly);
}
