namespace Valt.Infra.Settings;

public record UISettings
{
    public int CrawlerStartupWaitTime => 3000;
    public int CrawlerIntervalTime => 60000;
}