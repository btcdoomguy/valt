using System;
using System.Collections.Generic;

namespace Valt.Core.Modules.Assets.Simulation;

public static class BtcLoanSimulationCalculator
{
    public static BtcLoanSimulationResult Calculate(BtcLoanSimulationInput input)
    {
        Validate(input);

        var days = input.EndDate.DayNumber - input.StartDate.DayNumber;
        var principal = input.PrincipalAmount;
        var fees = input.Fees;

        if (days <= 0)
        {
            var zeroDayTotalRepay = principal + fees;
            return new BtcLoanSimulationResult
            {
                TotalRepay = zeroDayTotalRepay,
                Principal = principal,
                Interest = 0m,
                Fees = fees,
                LiquidationPrice = CalculateLiquidationPrice(input, zeroDayTotalRepay),
                EffectiveApr = 0m,
                Schedule = new[] { new LoanScheduleEntry(input.StartDate, 0m, zeroDayTotalRepay) }
            };
        }

        decimal interest;
        List<LoanScheduleEntry> schedule;

        switch (input.InterestMode)
        {
            case BtcLoanInterestMode.Simple:
                interest = CalculateSimpleInterest(principal, input.Apr, days);
                schedule = GenerateSimpleSchedule(input, principal, fees, days, interest);
                break;
            case BtcLoanInterestMode.Compound:
                // Compound mode is implemented in Task 2; for Task 1 fall back to simple math.
                interest = CalculateSimpleInterest(principal, input.Apr, days);
                schedule = GenerateSimpleSchedule(input, principal, fees, days, interest);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(input.InterestMode));
        }

        var totalRepay = principal + interest + fees;

        return new BtcLoanSimulationResult
        {
            TotalRepay = totalRepay,
            Principal = principal,
            Interest = interest,
            Fees = fees,
            LiquidationPrice = CalculateLiquidationPrice(input, totalRepay),
            EffectiveApr = CalculateEffectiveApr(principal, totalRepay, days),
            Schedule = schedule
        };
    }

    private static void Validate(BtcLoanSimulationInput input)
    {
        if (input.Apr < 0)
            throw new ArgumentException("APR cannot be negative", nameof(input.Apr));

        if (input.Fees < 0)
            throw new ArgumentException("Fees cannot be negative", nameof(input.Fees));

        if (input.CollateralSats <= 0)
            throw new ArgumentException("Collateral must be positive", nameof(input.CollateralSats));

        if (input.PrincipalAmount <= 0)
            throw new ArgumentException("Principal amount must be positive", nameof(input.PrincipalAmount));
    }

    private static decimal CalculateSimpleInterest(decimal principal, decimal apr, int days)
        => Math.Round(principal * apr / 365m * days, 2);

    private static List<LoanScheduleEntry> GenerateSimpleSchedule(
        BtcLoanSimulationInput input,
        decimal principal,
        decimal fees,
        int days,
        decimal totalInterest)
    {
        var schedule = new List<LoanScheduleEntry>();
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        var candidate = GetMonthEnd(startDate);
        while (candidate < endDate)
        {
            var elapsed = candidate.DayNumber - startDate.DayNumber;
            var accrued = CalculateSimpleInterest(principal, input.Apr, elapsed);
            schedule.Add(new LoanScheduleEntry(candidate, accrued, principal + accrued + fees));
            candidate = GetMonthEnd(candidate.AddDays(1));
        }

        schedule.Add(new LoanScheduleEntry(endDate, totalInterest, principal + totalInterest + fees));
        return schedule;
    }

    private static DateOnly GetMonthEnd(DateOnly date)
        => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

    private static decimal CalculateLiquidationPrice(BtcLoanSimulationInput input, decimal totalDebt)
    {
        var collateralBtc = input.CollateralSats / 100_000_000m;
        if (collateralBtc == 0m || input.LiquidationLtv == 0m)
            return 0m;

        return Math.Round(totalDebt / (collateralBtc * input.LiquidationLtv / 100m), 2);
    }

    private static decimal CalculateEffectiveApr(decimal principal, decimal totalRepay, int days)
    {
        if (days <= 0 || principal <= 0)
            return 0m;

        var totalCost = totalRepay - principal;
        return Math.Round(totalCost / principal * 365m / days, 2);
    }
}
