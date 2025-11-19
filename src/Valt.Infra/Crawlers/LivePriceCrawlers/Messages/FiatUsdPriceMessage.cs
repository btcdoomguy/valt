namespace Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

public record FiatUsdPrice
{
    public DateTime Utc { get; }
    public bool UpToDate { get; }
    public IReadOnlySet<Item> Items { get; }

    public FiatUsdPrice(DateTime utc, bool upToDate, IEnumerable<Item> items)
    {
        Utc = utc;
        UpToDate = upToDate;
        Items = new HashSet<Item>(items);
    }

    public record Item(string CurrencyCode, decimal Price);
}