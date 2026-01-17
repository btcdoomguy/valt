using System.Text.Json.Serialization;

namespace Valt.Infra.Modules.Goals;

internal class StackBitcoinGoalTypeDto
{
    [JsonPropertyName("targetSats")]
    public long TargetSats { get; set; }

    [JsonPropertyName("calculatedSats")]
    public long CalculatedSats { get; set; }
}

internal class SpendingLimitGoalTypeDto
{
    [JsonPropertyName("targetAmount")]
    public decimal TargetAmount { get; set; }

    [JsonPropertyName("calculatedSpending")]
    public decimal CalculatedSpending { get; set; }
}

internal class DcaGoalTypeDto
{
    [JsonPropertyName("targetPurchaseCount")]
    public int TargetPurchaseCount { get; set; }

    [JsonPropertyName("calculatedPurchaseCount")]
    public int CalculatedPurchaseCount { get; set; }
}

internal class IncomeFiatGoalTypeDto
{
    [JsonPropertyName("targetAmount")]
    public decimal TargetAmount { get; set; }

    [JsonPropertyName("calculatedIncome")]
    public decimal CalculatedIncome { get; set; }
}

internal class IncomeBtcGoalTypeDto
{
    [JsonPropertyName("targetSats")]
    public long TargetSats { get; set; }

    [JsonPropertyName("calculatedSats")]
    public long CalculatedSats { get; set; }
}

internal class ReduceExpenseCategoryGoalTypeDto
{
    [JsonPropertyName("targetAmount")]
    public decimal TargetAmount { get; set; }

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = null!;

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = null!;

    [JsonPropertyName("calculatedSpending")]
    public decimal CalculatedSpending { get; set; }
}

internal class BitcoinHodlGoalTypeDto
{
    [JsonPropertyName("maxSellableSats")]
    public long MaxSellableSats { get; set; }

    [JsonPropertyName("calculatedSoldSats")]
    public long CalculatedSoldSats { get; set; }
}
