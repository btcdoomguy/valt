using System.Text.Json.Serialization;

namespace Valt.Infra.Modules.Goals;

internal class StackBitcoinGoalTypeDto
{
    [JsonPropertyName("targetSats")]
    public long TargetSats { get; set; }

    [JsonPropertyName("calculatedSats")]
    public long CalculatedSats { get; set; }
}
