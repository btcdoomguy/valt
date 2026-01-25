namespace Valt.App.Modules.Budget.Transactions.DTOs;

public abstract record TransactionDetailsDto
{
    public required string FromAccountId { get; init; }
}

public record FiatTransactionDto : TransactionDetailsDto
{
    public required decimal Amount { get; init; }
    public required bool IsCredit { get; init; }
}

public record BitcoinTransactionDto : TransactionDetailsDto
{
    public required long AmountSats { get; init; }
    public required bool IsCredit { get; init; }
}

public record FiatToFiatTransferDto : TransactionDetailsDto
{
    public required string ToAccountId { get; init; }
    public required decimal FromAmount { get; init; }
    public required decimal ToAmount { get; init; }
}

public record BitcoinToBitcoinTransferDto : TransactionDetailsDto
{
    public required string ToAccountId { get; init; }
    public required long AmountSats { get; init; }
}

public record FiatToBitcoinTransferDto : TransactionDetailsDto
{
    public required string ToAccountId { get; init; }
    public required decimal FromFiatAmount { get; init; }
    public required long ToSatsAmount { get; init; }
}

public record BitcoinToFiatTransferDto : TransactionDetailsDto
{
    public required string ToAccountId { get; init; }
    public required long FromSatsAmount { get; init; }
    public required decimal ToFiatAmount { get; init; }
}
