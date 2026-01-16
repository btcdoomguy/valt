using Valt.Core.Common;
using Valt.Core.Kernel;
using Valt.Core.Modules.Goals.Events;

namespace Valt.Core.Modules.Goals;

public sealed class Goal : AggregateRoot<GoalId>
{
    public DateOnly RefDate { get; private set; }
    public GoalPeriods Period { get; private set; }
    public IGoalType GoalType { get; private set; }
    public decimal Progress { get; private set; }
    public bool IsUpToDate { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public GoalStates State { get; private set; }

    private Goal(GoalId id, DateOnly refDate, GoalPeriods period, IGoalType goalType, decimal progress,
        bool isUpToDate, DateTime lastUpdatedAt, GoalStates state, int version)
    {
        Id = id;
        RefDate = refDate;
        Period = period;
        GoalType = goalType;
        Progress = progress;
        IsUpToDate = isUpToDate;
        LastUpdatedAt = lastUpdatedAt;
        State = state;
        Version = version;
    }

    public static Goal Create(GoalId id, DateOnly refDate, GoalPeriods period, IGoalType goalType,
        decimal progress, bool isUpToDate, DateTime lastUpdatedAt, GoalStates state, int version)
    {
        return new Goal(id, refDate, period, goalType, progress, isUpToDate, lastUpdatedAt, state, version);
    }

    public static Goal New(DateOnly refDate, GoalPeriods period, IGoalType goalType)
    {
        var goal = new Goal(
            new GoalId(),
            refDate,
            period,
            goalType,
            0m,
            false,
            DateTime.MinValue,
            GoalStates.Open,
            0);

        goal.AddEvent(new GoalCreatedEvent(goal));
        return goal;
    }

    public void MarkAsStale()
    {
        if (!IsUpToDate)
            return;

        IsUpToDate = false;
        AddEvent(new GoalUpdatedEvent(this));
    }

    public void UpdateProgress(decimal progress, IGoalType updatedGoalType, DateTime updatedAt)
    {
        Progress = progress;
        GoalType = updatedGoalType;
        IsUpToDate = true;
        LastUpdatedAt = updatedAt;
        AddEvent(new GoalUpdatedEvent(this));
    }

    public void MarkAsCompleted()
    {
        if (State != GoalStates.Open)
            return;

        State = GoalStates.MarkedAsCompleted;
        AddEvent(new GoalUpdatedEvent(this));
    }

    public void Close()
    {
        if (State != GoalStates.Open)
            return;

        State = GoalStates.Closed;
        AddEvent(new GoalUpdatedEvent(this));
    }

    public void Conclude()
    {
        if (State != GoalStates.Completed && State != GoalStates.Open)
            return;

        State = GoalStates.MarkedAsCompleted;
        AddEvent(new GoalUpdatedEvent(this));
    }

    public void Reopen()
    {
        if (State != GoalStates.MarkedAsCompleted && State != GoalStates.Closed)
            return;

        State = GoalStates.Open;
        IsUpToDate = false;
        AddEvent(new GoalUpdatedEvent(this));
    }

    public DateOnlyRange GetPeriodRange() => Period switch
    {
        GoalPeriods.Monthly => new DateOnlyRange(
            new DateOnly(RefDate.Year, RefDate.Month, 1),
            new DateOnly(RefDate.Year, RefDate.Month, DateTime.DaysInMonth(RefDate.Year, RefDate.Month))),
        GoalPeriods.Yearly => new DateOnlyRange(
            new DateOnly(RefDate.Year, 1, 1),
            new DateOnly(RefDate.Year, 12, 31)),
        _ => throw new ArgumentOutOfRangeException()
    };
}
