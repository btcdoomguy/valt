namespace Valt.Core.Common;

public record RangedFiatValue
{
    public FiatValue Min { get; private set; }
    public FiatValue Max { get; private set; }
    
    public RangedFiatValue(FiatValue min, FiatValue max)
    {
        if (min.Value >= max.Value)
            throw new ArgumentException("Min value must be less than max value");
        
        Min = min;
        Max = max;
    }
}