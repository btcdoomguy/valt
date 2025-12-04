using Valt.Core.Kernel.Abstractions.Time;

namespace Valt.Tests.Builders;

public class FakeClock : IClock
{
    private readonly DateTime _dateTimeUtc;

    public FakeClock(DateTime dateTimeUtc)
    {
        _dateTimeUtc = dateTimeUtc;
    }
    public DateTime GetCurrentDateTimeUtc()
    {
        return _dateTimeUtc;
    }

    public DateOnly GetCurrentLocalDate()
    {
        return DateOnly.FromDateTime(_dateTimeUtc.Date);
    }
}