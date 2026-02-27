namespace Valt.Infra.Modules.Configuration;

public enum SimulatedPriceType
{
    Percentage = 0,
    Fixed = 1
}

public record SimulatedPriceLineConfig(SimulatedPriceType Type, decimal Value);
