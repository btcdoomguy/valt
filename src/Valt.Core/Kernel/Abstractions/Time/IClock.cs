namespace Valt.Core.Kernel.Abstractions.Time;

public interface IClock
{
    DateTime GetCurrentDateTimeUtc();
    DateOnly GetCurrentLocalDate();
}