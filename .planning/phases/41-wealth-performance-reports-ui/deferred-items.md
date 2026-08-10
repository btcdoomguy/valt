# Deferred Items: 41-wealth-performance-reports-ui

## Out-of-Scope Discoveries

### Pre-existing live-API integration test failures

Observed during `dotnet test` verification of Plan 41-01. These failures are environmental (network / third-party API availability) and unrelated to the WLT-04 changes.

| Test | Error | Notes |
|------|-------|-------|
| `Valt.Tests.HistoricPriceCrawlers.FrankfurterFiatHistoricalProviderTests.Should_Get_Prices` | Expected 257 prices, got 0 | Frankfurter API returned no data |
| `Valt.Tests.Infrastructure.Indicators.BitcoinDominanceProviderTests.GetAsync_ReturnsValidData` | 403 Forbidden | External indicator API blocked request |
| `Valt.Tests.LivePriceCrawlers.CoinGeckoProviderTests.Should_Get_Prices_With_Usd_And_Up_To_Date` | 403 Forbidden | CoinGecko API blocked request |
| `Valt.Tests.LivePriceCrawlers.FrankfurterFiatProviderTests.Should_Get_Prices` | HttpClient timeout | Frankfurter API unreachable |

**Disposition:** Out of scope for Phase 41. These tests were not modified by Plan 41-01 and fail against live services in the current environment.
