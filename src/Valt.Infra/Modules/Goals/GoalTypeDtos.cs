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

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = null!;

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
