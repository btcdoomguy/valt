namespace Valt.Infra.Crawlers.Indicators;

public interface IFearAndGreedProvider
{
    Task<FearAndGreedData> GetAsync();
}
