using LiteDB;
using Valt.Core.Modules.Goals;
using Valt.Infra.Kernel;

namespace Valt.Infra.Modules.Goals;

internal static class Extensions
{
    public static GoalEntity AsEntity(this Goal goal)
    {
        return new GoalEntity
        {
            Id = new ObjectId(goal.Id.ToString()),
            RefDate = goal.RefDate.ToValtDateTime(),
            PeriodId = (int)goal.Period,
            GoalTypeNameId = (int)goal.GoalType.TypeName,
            GoalTypeJson = GoalTypeSerializer.Serialize(goal.GoalType),
            Progress = goal.Progress,
            IsUpToDate = goal.IsUpToDate,
            LastUpdatedAt = goal.LastUpdatedAt,
            StateId = (int)goal.State,
            Version = goal.Version
        };
    }

    public static Goal AsDomainObject(this GoalEntity entity)
    {
        var goalType = GoalTypeSerializer.DeserializeGoalType((GoalTypeNames)entity.GoalTypeNameId, entity.GoalTypeJson);

        return Goal.Create(
            new GoalId(entity.Id.ToString()),
            DateOnly.FromDateTime(entity.RefDate),
            (GoalPeriods)entity.PeriodId,
            goalType,
            entity.Progress,
            entity.IsUpToDate,
            entity.LastUpdatedAt,
            (GoalStates)entity.StateId,
            entity.Version);
    }
}
