namespace Valt.Infra.TransactionTerms;

public interface ITransactionTermService
{
    void AddEntry(string name, string categoryId, long? satAmount, decimal? fiatAmount);
    void RemoveEntry(string name, string categoryId);
    IEnumerable<TransactionTermResult> Search(string term, int limit);
}