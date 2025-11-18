namespace Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

public record BtcPriceMessage
{
    public string CurrencyCode { get; }
    public decimal Price { get; }
    public decimal? PreviousPrice { get; private set; }

    public BtcPriceMessage(string currencyCode, decimal price)
    {
        CurrencyCode = currencyCode;
        Price = price;
    }

    public BtcPriceMessage(string currencyCode, decimal price, decimal previousPrice) : this(currencyCode, price)
    {
        SetPreviousPrice(previousPrice);
    }

    public void SetPreviousPrice(decimal previousPrice)
    {
        PreviousPrice = previousPrice;
    }
}