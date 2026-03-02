namespace Valt.Infra.Crawlers.Indicators;

public interface IIndicatorCache
{
    IndicatorSnapshot? GetLatest();
    void Save(IndicatorSnapshot snapshot);
}
