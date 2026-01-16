using LiteDB;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Kernel;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
            GoalTypeJson = SerializeGoalType(goal.GoalType),
            Progress = goal.Progress,
            IsUpToDate = goal.IsUpToDate,
            LastUpdatedAt = goal.LastUpdatedAt,
            StateId = (int)goal.State,
            Version = goal.Version
        };
    }

    public static Goal AsDomainObject(this GoalEntity entity)
    {
        var goalType = DeserializeGoalType((GoalTypeNames)entity.GoalTypeNameId, entity.GoalTypeJson);

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

    private static string SerializeGoalType(IGoalType goalType)
    {
        return goalType.TypeName switch
        {
            GoalTypeNames.StackBitcoin => JsonSerializer.Serialize((StackBitcoinGoalType)goalType),
            _ => throw new NotSupportedException($"Goal type {goalType.TypeName} is not supported")
        };
    }

    private static IGoalType DeserializeGoalType(GoalTypeNames typeName, string json)
    {
        return typeName switch
        {
            GoalTypeNames.StackBitcoin => JsonSerializer.Deserialize<StackBitcoinGoalType>(json)
                ?? throw new InvalidOperationException("Failed to deserialize StackBitcoinGoalType"),
            _ => throw new NotSupportedException($"Goal type {typeName} is not supported")
        };
    }
}
