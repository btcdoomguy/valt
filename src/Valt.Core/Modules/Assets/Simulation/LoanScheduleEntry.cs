namespace Valt.Core.Modules.Assets.Simulation;

public sealed record LoanScheduleEntry(DateOnly Date, decimal AccruedInterest, decimal CumulativeTotal);
