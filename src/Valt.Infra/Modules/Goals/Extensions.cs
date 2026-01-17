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
            GoalTypeNames.StackBitcoin => SerializeStackBitcoinGoalType((StackBitcoinGoalType)goalType),
            GoalTypeNames.SpendingLimit => SerializeSpendingLimitGoalType((SpendingLimitGoalType)goalType),
            GoalTypeNames.Dca => SerializeDcaGoalType((DcaGoalType)goalType),
            GoalTypeNames.IncomeFiat => SerializeIncomeFiatGoalType((IncomeFiatGoalType)goalType),
            GoalTypeNames.IncomeBtc => SerializeIncomeBtcGoalType((IncomeBtcGoalType)goalType),
            GoalTypeNames.ReduceExpenseCategory => SerializeReduceExpenseCategoryGoalType((ReduceExpenseCategoryGoalType)goalType),
            GoalTypeNames.BitcoinHodl => SerializeBitcoinHodlGoalType((BitcoinHodlGoalType)goalType),
            _ => throw new NotSupportedException($"Goal type {goalType.TypeName} is not supported")
        };
    }

    private static IGoalType DeserializeGoalType(GoalTypeNames typeName, string json)
    {
        return typeName switch
        {
            GoalTypeNames.StackBitcoin => DeserializeStackBitcoinGoalType(json),
            GoalTypeNames.SpendingLimit => DeserializeSpendingLimitGoalType(json),
            GoalTypeNames.Dca => DeserializeDcaGoalType(json),
            GoalTypeNames.IncomeFiat => DeserializeIncomeFiatGoalType(json),
            GoalTypeNames.IncomeBtc => DeserializeIncomeBtcGoalType(json),
            GoalTypeNames.ReduceExpenseCategory => DeserializeReduceExpenseCategoryGoalType(json),
            GoalTypeNames.BitcoinHodl => DeserializeBitcoinHodlGoalType(json),
            _ => throw new NotSupportedException($"Goal type {typeName} is not supported")
        };
    }

    private static string SerializeStackBitcoinGoalType(StackBitcoinGoalType goalType)
    {
        var dto = new StackBitcoinGoalTypeDto
        {
            TargetSats = goalType.TargetSats,
            CalculatedSats = goalType.CalculatedSats
        };
        return JsonSerializer.Serialize(dto);
    }

    private static StackBitcoinGoalType DeserializeStackBitcoinGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<StackBitcoinGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize StackBitcoinGoalType");
        return new StackBitcoinGoalType(dto.TargetSats, dto.CalculatedSats);
    }

    private static string SerializeSpendingLimitGoalType(SpendingLimitGoalType goalType)
    {
        var dto = new SpendingLimitGoalTypeDto
        {
            TargetAmount = goalType.TargetAmount,
            Currency = goalType.Currency,
            CalculatedSpending = goalType.CalculatedSpending
        };
        return JsonSerializer.Serialize(dto);
    }

    private static SpendingLimitGoalType DeserializeSpendingLimitGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<SpendingLimitGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize SpendingLimitGoalType");
        return new SpendingLimitGoalType(dto.TargetAmount, dto.Currency, dto.CalculatedSpending);
    }

    private static string SerializeDcaGoalType(DcaGoalType goalType)
    {
        var dto = new DcaGoalTypeDto
        {
            TargetPurchaseCount = goalType.TargetPurchaseCount,
            CalculatedPurchaseCount = goalType.CalculatedPurchaseCount
        };
        return JsonSerializer.Serialize(dto);
    }

    private static DcaGoalType DeserializeDcaGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<DcaGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize DcaGoalType");
        return new DcaGoalType(dto.TargetPurchaseCount, dto.CalculatedPurchaseCount);
    }

    private static string SerializeIncomeFiatGoalType(IncomeFiatGoalType goalType)
    {
        var dto = new IncomeFiatGoalTypeDto
        {
            TargetAmount = goalType.TargetAmount,
            Currency = goalType.Currency,
            CalculatedIncome = goalType.CalculatedIncome
        };
        return JsonSerializer.Serialize(dto);
    }

    private static IncomeFiatGoalType DeserializeIncomeFiatGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<IncomeFiatGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize IncomeFiatGoalType");
        return new IncomeFiatGoalType(dto.TargetAmount, dto.Currency, dto.CalculatedIncome);
    }

    private static string SerializeIncomeBtcGoalType(IncomeBtcGoalType goalType)
    {
        var dto = new IncomeBtcGoalTypeDto
        {
            TargetSats = goalType.TargetSats,
            CalculatedSats = goalType.CalculatedSats
        };
        return JsonSerializer.Serialize(dto);
    }

    private static IncomeBtcGoalType DeserializeIncomeBtcGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<IncomeBtcGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize IncomeBtcGoalType");
        return new IncomeBtcGoalType(dto.TargetSats, dto.CalculatedSats);
    }

    private static string SerializeReduceExpenseCategoryGoalType(ReduceExpenseCategoryGoalType goalType)
    {
        var dto = new ReduceExpenseCategoryGoalTypeDto
        {
            TargetAmount = goalType.TargetAmount,
            CategoryId = goalType.CategoryId,
            CategoryName = goalType.CategoryName,
            CalculatedSpending = goalType.CalculatedSpending
        };
        return JsonSerializer.Serialize(dto);
    }

    private static ReduceExpenseCategoryGoalType DeserializeReduceExpenseCategoryGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<ReduceExpenseCategoryGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize ReduceExpenseCategoryGoalType");
        return new ReduceExpenseCategoryGoalType(dto.TargetAmount, dto.CategoryId, dto.CategoryName, dto.CalculatedSpending);
    }

    private static string SerializeBitcoinHodlGoalType(BitcoinHodlGoalType goalType)
    {
        var dto = new BitcoinHodlGoalTypeDto
        {
            MaxSellableSats = goalType.MaxSellableSats,
            CalculatedSoldSats = goalType.CalculatedSoldSats
        };
        return JsonSerializer.Serialize(dto);
    }

    private static BitcoinHodlGoalType DeserializeBitcoinHodlGoalType(string json)
    {
        var dto = JsonSerializer.Deserialize<BitcoinHodlGoalTypeDto>(json)
            ?? throw new InvalidOperationException("Failed to deserialize BitcoinHodlGoalType");
        return new BitcoinHodlGoalType(dto.MaxSellableSats, dto.CalculatedSoldSats);
    }
}
