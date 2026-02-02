using System.Text.Json;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;

namespace Valt.Infra.Modules.Goals;

/// <summary>
/// Centralized serialization for goal type DTOs to avoid code duplication.
/// </summary>
internal static class GoalTypeSerializer
{
    /// <summary>
    /// Deserializes a goal type DTO from JSON.
    /// </summary>
    public static TDto Deserialize<TDto>(string json, string typeName) where TDto : class
    {
        return JsonSerializer.Deserialize<TDto>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeName}");
    }

    /// <summary>
    /// Deserializes and converts to a domain goal type.
    /// </summary>
    public static IGoalType DeserializeGoalType(GoalTypeNames typeName, string json)
    {
        return typeName switch
        {
            GoalTypeNames.StackBitcoin => DeserializeStackBitcoin(json),
            GoalTypeNames.SpendingLimit => DeserializeSpendingLimit(json),
            GoalTypeNames.Dca => DeserializeDca(json),
            GoalTypeNames.IncomeFiat => DeserializeIncomeFiat(json),
            GoalTypeNames.IncomeBtc => DeserializeIncomeBtc(json),
            GoalTypeNames.ReduceExpenseCategory => DeserializeReduceExpenseCategory(json),
            GoalTypeNames.BitcoinHodl => DeserializeBitcoinHodl(json),
            _ => throw new NotSupportedException($"Goal type {typeName} is not supported")
        };
    }

    public static StackBitcoinGoalType DeserializeStackBitcoin(string json)
    {
        var dto = Deserialize<StackBitcoinGoalTypeDto>(json, nameof(StackBitcoinGoalType));
        return new StackBitcoinGoalType(dto.TargetSats, dto.CalculatedSats);
    }

    public static SpendingLimitGoalType DeserializeSpendingLimit(string json)
    {
        var dto = Deserialize<SpendingLimitGoalTypeDto>(json, nameof(SpendingLimitGoalType));
        return new SpendingLimitGoalType(dto.TargetAmount, dto.CalculatedSpending);
    }

    public static DcaGoalType DeserializeDca(string json)
    {
        var dto = Deserialize<DcaGoalTypeDto>(json, nameof(DcaGoalType));
        return new DcaGoalType(dto.TargetPurchaseCount, dto.CalculatedPurchaseCount);
    }

    public static IncomeFiatGoalType DeserializeIncomeFiat(string json)
    {
        var dto = Deserialize<IncomeFiatGoalTypeDto>(json, nameof(IncomeFiatGoalType));
        return new IncomeFiatGoalType(dto.TargetAmount, dto.CalculatedIncome);
    }

    public static IncomeBtcGoalType DeserializeIncomeBtc(string json)
    {
        var dto = Deserialize<IncomeBtcGoalTypeDto>(json, nameof(IncomeBtcGoalType));
        return new IncomeBtcGoalType(dto.TargetSats, dto.CalculatedSats);
    }

    public static ReduceExpenseCategoryGoalType DeserializeReduceExpenseCategory(string json)
    {
        var dto = Deserialize<ReduceExpenseCategoryGoalTypeDto>(json, nameof(ReduceExpenseCategoryGoalType));
        return new ReduceExpenseCategoryGoalType(dto.TargetAmount, dto.CategoryId, dto.CategoryName, dto.CalculatedSpending);
    }

    public static BitcoinHodlGoalType DeserializeBitcoinHodl(string json)
    {
        var dto = Deserialize<BitcoinHodlGoalTypeDto>(json, nameof(BitcoinHodlGoalType));
        return new BitcoinHodlGoalType(dto.MaxSellableSats, dto.CalculatedSoldSats);
    }

    /// <summary>
    /// Serializes a goal type to JSON.
    /// </summary>
    public static string Serialize(IGoalType goalType)
    {
        return goalType.TypeName switch
        {
            GoalTypeNames.StackBitcoin => SerializeStackBitcoin((StackBitcoinGoalType)goalType),
            GoalTypeNames.SpendingLimit => SerializeSpendingLimit((SpendingLimitGoalType)goalType),
            GoalTypeNames.Dca => SerializeDca((DcaGoalType)goalType),
            GoalTypeNames.IncomeFiat => SerializeIncomeFiat((IncomeFiatGoalType)goalType),
            GoalTypeNames.IncomeBtc => SerializeIncomeBtc((IncomeBtcGoalType)goalType),
            GoalTypeNames.ReduceExpenseCategory => SerializeReduceExpenseCategory((ReduceExpenseCategoryGoalType)goalType),
            GoalTypeNames.BitcoinHodl => SerializeBitcoinHodl((BitcoinHodlGoalType)goalType),
            _ => throw new NotSupportedException($"Goal type {goalType.TypeName} is not supported")
        };
    }

    private static string SerializeStackBitcoin(StackBitcoinGoalType goalType)
    {
        var dto = new StackBitcoinGoalTypeDto
        {
            TargetSats = goalType.TargetSats,
            CalculatedSats = goalType.CalculatedSats
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeSpendingLimit(SpendingLimitGoalType goalType)
    {
        var dto = new SpendingLimitGoalTypeDto
        {
            TargetAmount = goalType.TargetAmount,
            CalculatedSpending = goalType.CalculatedSpending
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeDca(DcaGoalType goalType)
    {
        var dto = new DcaGoalTypeDto
        {
            TargetPurchaseCount = goalType.TargetPurchaseCount,
            CalculatedPurchaseCount = goalType.CalculatedPurchaseCount
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeIncomeFiat(IncomeFiatGoalType goalType)
    {
        var dto = new IncomeFiatGoalTypeDto
        {
            TargetAmount = goalType.TargetAmount,
            CalculatedIncome = goalType.CalculatedIncome
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeIncomeBtc(IncomeBtcGoalType goalType)
    {
        var dto = new IncomeBtcGoalTypeDto
        {
            TargetSats = goalType.TargetSats,
            CalculatedSats = goalType.CalculatedSats
        };
        return JsonSerializer.Serialize(dto);
    }

    private static string SerializeReduceExpenseCategory(ReduceExpenseCategoryGoalType goalType)
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

    private static string SerializeBitcoinHodl(BitcoinHodlGoalType goalType)
    {
        var dto = new BitcoinHodlGoalTypeDto
        {
            MaxSellableSats = goalType.MaxSellableSats,
            CalculatedSoldSats = goalType.CalculatedSoldSats
        };
        return JsonSerializer.Serialize(dto);
    }
}
