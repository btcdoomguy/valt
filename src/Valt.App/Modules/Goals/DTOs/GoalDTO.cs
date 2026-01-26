namespace Valt.App.Modules.Goals.DTOs;

/// <summary>
/// DTO representing a Goal for display purposes.
/// </summary>
public record GoalDTO
{
    public required string Id { get; init; }
    public required DateOnly RefDate { get; init; }
    public required int Period { get; init; }
    public required decimal Progress { get; init; }
    public required bool IsUpToDate { get; init; }
    public required DateTime LastUpdatedAt { get; init; }
    public required int State { get; init; }
    public required GoalTypeOutputDTO GoalType { get; init; }
}

/// <summary>
/// Base class for goal type output DTOs.
/// Contains common properties for all goal types.
/// </summary>
public abstract record GoalTypeOutputDTO
{
    /// <summary>
    /// Goal type: 0=StackBitcoin, 1=SpendingLimit, 2=Dca, 3=IncomeFiat, 4=IncomeBtc, 5=ReduceExpenseCategory, 6=BitcoinHodl
    /// </summary>
    public abstract int TypeId { get; }

    /// <summary>
    /// Whether this goal type requires price data for calculation.
    /// </summary>
    public abstract bool RequiresPriceData { get; }

    /// <summary>
    /// Progression mode: 0=ZeroToSuccess, 1=DecreasingSuccess
    /// </summary>
    public abstract int ProgressionMode { get; }
}

/// <summary>
/// Stack a target amount of bitcoin (in sats).
/// </summary>
public record StackBitcoinGoalTypeOutputDTO : GoalTypeOutputDTO
{
    public override int TypeId => 0;
    public override bool RequiresPriceData => false;
    public override int ProgressionMode => 0; // ZeroToSuccess

    public required long TargetSats { get; init; }
    public required long CalculatedSats { get; init; }
}

/// <summary>
/// Stay under a spending limit in fiat.
/// </summary>
public record SpendingLimitGoalTypeOutputDTO : GoalTypeOutputDTO
{
    public override int TypeId => 1;
    public override bool RequiresPriceData => true;
    public override int ProgressionMode => 1; // DecreasingSuccess

    public required decimal TargetAmount { get; init; }
    public required decimal CalculatedSpending { get; init; }
}

/// <summary>
/// Dollar-cost average by making a target number of bitcoin purchases.
/// </summary>
public record DcaGoalTypeOutputDTO : GoalTypeOutputDTO
{
    public override int TypeId => 2;
    public override bool RequiresPriceData => false;
    public override int ProgressionMode => 0; // ZeroToSuccess

    public required int TargetPurchaseCount { get; init; }
    public required int CalculatedPurchaseCount { get; init; }
}

/// <summary>
/// Earn a target amount of fiat income.
/// </summary>
public record IncomeFiatGoalTypeOutputDTO : GoalTypeOutputDTO
{
    public override int TypeId => 3;
    public override bool RequiresPriceData => true;
    public override int ProgressionMode => 0; // ZeroToSuccess

    public required decimal TargetAmount { get; init; }
    public required decimal CalculatedIncome { get; init; }
}

/// <summary>
/// Earn a target amount of bitcoin income (in sats).
/// </summary>
public record IncomeBtcGoalTypeOutputDTO : GoalTypeOutputDTO
{
    public override int TypeId => 4;
    public override bool RequiresPriceData => false;
    public override int ProgressionMode => 0; // ZeroToSuccess

    public required long TargetSats { get; init; }
    public required long CalculatedSats { get; init; }
}

/// <summary>
/// Reduce spending in a specific category below a target.
/// </summary>
public record ReduceExpenseCategoryGoalTypeOutputDTO : GoalTypeOutputDTO
{
    public override int TypeId => 5;
    public override bool RequiresPriceData => true;
    public override int ProgressionMode => 1; // DecreasingSuccess

    public required decimal TargetAmount { get; init; }
    public required string CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal CalculatedSpending { get; init; }
}

/// <summary>
/// HODL goal - don't sell more than a maximum amount of bitcoin.
/// </summary>
public record BitcoinHodlGoalTypeOutputDTO : GoalTypeOutputDTO
{
    public override int TypeId => 6;
    public override bool RequiresPriceData => false;
    public override int ProgressionMode => 1; // DecreasingSuccess

    public required long MaxSellableSats { get; init; }
    public required long CalculatedSoldSats { get; init; }
}
