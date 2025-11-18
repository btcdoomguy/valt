using LiteDB;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Budget.Transactions;

namespace Valt.Infra.TransactionTerms;

internal class TransactionTermService : ITransactionTermService
{
    private readonly ILocalDatabase _localDatabase;

    public TransactionTermService(ILocalDatabase localDatabase)
    {
        _localDatabase = localDatabase;
    }

    public void AddEntry(string name, string categoryId, long? satAmount, decimal? fiatAmount)
    {
        var entry = _localDatabase.GetTransactionTerms()
            .FindOne(x => x.Name == name && x.CategoryId == new ObjectId(categoryId));

        if (entry is null)
        {
            _localDatabase.GetTransactionTerms().Insert(new TransactionTermEntity()
            {
                CategoryId = new ObjectId(categoryId),
                Count = 1,
                Name = name,
                SatAmount = satAmount,
                FiatAmount = fiatAmount
            });
        }
        else
        {
            entry.Count++;
            entry.SatAmount = satAmount;
            entry.FiatAmount = fiatAmount;

            _localDatabase.GetTransactionTerms().Update(entry);
        }
    }

    public void RemoveEntry(string name, string categoryId)
    {
        var entry = _localDatabase.GetTransactionTerms()
            .FindOne(x => x.Name == name && x.CategoryId == new ObjectId(categoryId));

        if (entry is not null)
        {
            entry.Count--;

            if (entry.Count == 0)
                _localDatabase.GetTransactionTerms().Delete(entry.Id);
        }
    }

    public IEnumerable<TransactionTermResult> Search(string term, int limit)
    {
        var categories = _localDatabase.GetCategories().FindAll().ToList();

        return _localDatabase.GetTransactionTerms()
            .Find(x => x.Name.StartsWith(term, StringComparison.InvariantCultureIgnoreCase))
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .Select(x =>
            {
                var category = categories?.SingleOrDefault(y => y.Id == x.CategoryId)?.Name ?? string.Empty;
                return new TransactionTermResult(x.Name, x.CategoryId.ToString(), category, x.SatAmount, x.FiatAmount);
            });
    }
}