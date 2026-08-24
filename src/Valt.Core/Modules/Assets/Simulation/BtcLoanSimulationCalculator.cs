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
                (interest, schedule) = CalculateSimple(input, principal, fees, days);
                break;
            case BtcLoanInterestMode.Compound:
                (interest, schedule) = CalculateCompound(input, principal, fees, days);
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

    private static (decimal Interest, List<LoanScheduleEntry> Schedule) CalculateSimple(
        BtcLoanSimulationInput input,
        decimal principal,
        decimal fees,
        int days)
    {
        var schedule = new List<LoanScheduleEntry>();
        var startDate = input.StartDate;
        var endDate = input.EndDate;

        // Emit a row on the start date when it is itself a month-end anchor.
        if (startDate == GetMonthEnd(startDate))
        {
            schedule.Add(new LoanScheduleEntry(startDate, 0m, principal + fees));
        }

        var totalInterest = Math.Round(principal * input.Apr / 365m * days, 2);

        for (var i = 1; i <= days; i++)
        {
            var currentDate = startDate.AddDays(i);
            if (currentDate == endDate || currentDate == GetMonthEnd(currentDate))
            {
                var elapsed = currentDate.DayNumber - startDate.DayNumber;
                var accrued = Math.Round(principal * input.Apr / 365m * elapsed, 2);
                schedule.Add(new LoanScheduleEntry(currentDate, accrued, principal + accrued + fees));
            }
        }

        // Ensure the closing row uses the headline rounded interest and total.
        if (schedule.Count == 0 || schedule[^1].Date != endDate)
        {
            schedule.Add(new LoanScheduleEntry(endDate, totalInterest, principal + totalInterest + fees));
        }
        else
        {
            var last = schedule[^1];
            schedule[^1] = new LoanScheduleEntry(last.Date, totalInterest, principal + totalInterest + fees);
        }

        return (totalInterest, schedule);
    }

    private static (decimal Interest, List<LoanScheduleEntry> Schedule) CalculateCompound(
        BtcLoanSimulationInput input,
        decimal principal,
        decimal fees,
        int days)
    {
        var schedule = new List<LoanScheduleEntry>();
        var startDate = input.StartDate;
        var endDate = input.EndDate;
        var runningPrincipal = principal;
        var accruedInterest = 0m;

        // Emit a row on the start date when it is itself a month-end anchor.
        if (startDate == GetMonthEnd(startDate))
        {
            schedule.Add(new LoanScheduleEntry(startDate, 0m, principal + fees));
        }

        for (var i = 1; i <= days; i++)
        {
            var dailyInterest = runningPrincipal * input.Apr / 365m;
            accruedInterest += dailyInterest;
            runningPrincipal += dailyInterest;

            var currentDate = startDate.AddDays(i);
            if (currentDate == endDate || currentDate == GetMonthEnd(currentDate))
            {
                var roundedAccrued = Math.Round(accruedInterest, 2);
                schedule.Add(new LoanScheduleEntry(currentDate, roundedAccrued, principal + roundedAccrued + fees));
            }
        }

        var totalInterest = Math.Round(accruedInterest, 2);

        // Ensure the closing row uses the headline rounded interest and total.
        if (schedule.Count == 0 || schedule[^1].Date != endDate)
        {
            schedule.Add(new LoanScheduleEntry(endDate, totalInterest, principal + totalInterest + fees));
        }
        else
        {
            var last = schedule[^1];
            schedule[^1] = new LoanScheduleEntry(last.Date, totalInterest, principal + totalInterest + fees);
        }

        return (totalInterest, schedule);
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
