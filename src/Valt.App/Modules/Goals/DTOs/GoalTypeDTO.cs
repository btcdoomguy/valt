namespace Valt.App.Modules.Goals.DTOs;

/// <summary>
/// Base class for goal type input DTOs.
/// Each goal type has different parameters.
/// </summary>
public abstract record GoalTypeInputDTO
{
    /// <summary>
    /// Goal type: 0=StackBitcoin, 1=SpendingLimit, 2=Dca, 3=IncomeFiat, 4=IncomeBtc, 5=ReduceExpenseCategory, 6=BitcoinHodl
    /// </summary>
    public abstract int TypeId { get; }
}

/// <summary>
/// Stack a target amount of bitcoin (in sats).
/// Progress: 0% to 100% (success)
/// </summary>
public record StackBitcoinGoalTypeDTO : GoalTypeInputDTO
{
    public override int TypeId => 0;
    public required long TargetSats { get; init; }
}

/// <summary>
/// Stay under a spending limit in fiat (displayed in user's default currency).
/// Progress: 0% (no spending) to 100% (limit reached) - fails at 100%
/// </summary>
public record SpendingLimitGoalTypeDTO : GoalTypeInputDTO
{
    public override int TypeId => 1;
    public required decimal TargetAmount { get; init; }
}

/// <summary>
/// Dollar-cost average by making a target number of bitcoin purchases.
/// Progress: 0% to 100% (success)
/// </summary>
public record DcaGoalTypeDTO : GoalTypeInputDTO
{
    public override int TypeId => 2;
    public required int TargetPurchaseCount { get; init; }
}

/// <summary>
/// Earn a target amount of fiat income.
/// Progress: 0% to 100% (success)
/// </summary>
public record IncomeFiatGoalTypeDTO : GoalTypeInputDTO
{
    public override int TypeId => 3;
    public required decimal TargetAmount { get; init; }
}

/// <summary>
/// Earn a target amount of bitcoin income (in sats).
/// Progress: 0% to 100% (success)
/// </summary>
public record IncomeBtcGoalTypeDTO : GoalTypeInputDTO
{
    public override int TypeId => 4;
    public required long TargetSats { get; init; }
}

/// <summary>
/// Reduce spending in a specific category below a target.
/// Progress: 0% (no spending) to 100% (limit reached) - fails at 100%
/// </summary>
public record ReduceExpenseCategoryGoalTypeDTO : GoalTypeInputDTO
{
    public override int TypeId => 5;
    public required decimal TargetAmount { get; init; }
    public required string CategoryId { get; init; }
}

/// <summary>
/// HODL goal - don't sell more than a maximum amount of bitcoin.
/// MaxSellableSats = 0 means no sales allowed (full HODL).
/// Progress: 0% (no sales) to 100% (limit reached) - fails at 100%
/// </summary>
public record BitcoinHodlGoalTypeDTO : GoalTypeInputDTO
{
    public override int TypeId => 6;
    public required long MaxSellableSats { get; init; }
}
