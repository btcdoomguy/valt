namespace Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

public record FiatUsdPrice
{
    public string CurrencyCode { get; }
    public decimal Price { get; }
    public FiatUsdPrice(string currencyCode, decimal price)
    {
        CurrencyCode = currencyCode;
        Price = price;
    }
}