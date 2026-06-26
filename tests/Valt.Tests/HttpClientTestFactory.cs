using Microsoft.Extensions.DependencyInjection;
using Valt.Infra;

namespace Valt.Tests;

internal static class HttpClientTestFactory
{
    public static IHttpClientFactory Create()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddHttpClient(HttpClientNames.GitHubApi, c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient(HttpClientNames.CoinGecko, c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient(HttpClientNames.Indicator, c => c.Timeout = TimeSpan.FromSeconds(5));
        services.AddHttpClient(HttpClientNames.PriceProvider, c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient(HttpClientNames.UpdateDownload, c => c.Timeout = TimeSpan.FromMinutes(10));
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }
}
