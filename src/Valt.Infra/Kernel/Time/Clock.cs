using Valt.Core.Kernel.Abstractions.Time;

namespace Valt.Infra.Kernel.Time;

public class Clock : IClock
{
    public DateTime GetCurrentDateTimeUtc()
    {
        return DateTime.UtcNow;
    }

    public DateOnly GetCurrentLocalDate()
    {
        return DateOnly.FromDateTime(DateTime.Today);
    }
}