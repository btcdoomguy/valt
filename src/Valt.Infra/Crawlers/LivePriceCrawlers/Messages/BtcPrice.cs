namespace Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

public record BtcPrice
{
    public DateTime Utc { get; }
    public bool UpToDate { get; }
    public IReadOnlySet<Item> Items { get; }

    public BtcPrice(DateTime utc, bool upToDate, IEnumerable<Item> items)
    {
        Utc = utc;
        UpToDate = upToDate;
        Items = new HashSet<Item>(items);
    }

    public record Item
    {
        public Item(string currencyCode, decimal price, decimal? previousPrice = null)
        {
            CurrencyCode = currencyCode;
            Price = price;
            SetPreviousPrice(previousPrice);
        }

        public string CurrencyCode { get; }
        public decimal Price { get; }
        public decimal? PreviousPrice { get; private set; }

        public void SetPreviousPrice(decimal? previousPrice)
        {
            PreviousPrice = previousPrice;
        }
    }
}