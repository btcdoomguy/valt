using Valt.Core.Common;

namespace Valt.Infra.TransactionTerms;

public record TransactionTermResult(
    string Name,
    string CategoryId,
    string CategoryName,
    long? SatAmount,
    decimal? FiatAmount)
{
    public override string ToString() => Name;
}