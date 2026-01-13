# External Integrations

**Analysis Date:** 2026-01-13

## APIs & External Services

**Live Bitcoin Prices:**
- Coinbase API - Current BTC exchange rates
  - Client: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Bitcoin/Providers/CoinbaseProvider.cs`
  - Endpoint: `https://api.coinbase.com/v2/exchange-rates?currency=BTC`
  - Auth: None required (public API)
  - Update frequency: 30 seconds via `LivePricesUpdaterJob`
  - Returns: Rates for all 34 supported fiat currencies

**Live Fiat Rates (Primary):**
- Frankfurter API - EUR-based exchange rates
  - Client: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Fiat/Providers/FrankfurterFiatRateProvider.cs`
  - Endpoint: `https://api.frankfurter.dev/v1/latest?base=USD&symbols={currencies}`
  - Auth: None required (free API)
  - Supports: 31 currencies

**Live Fiat Rates (Fallback):**
- CurrencyApi - USD-based exchange rates
  - Client: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Fiat/Providers/CurrencyApiFiatRateProvider.cs`
  - Endpoint: `https://latest.currency-api.pages.dev/v1/currencies/usd.json`
  - Auth: None required (free API)
  - Supports: 34 currencies (includes PYG, UYU)
  - Provider selection: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Fiat/Providers/FiatPriceProviderSelector.cs`

**Historical Bitcoin Prices:**
- Kraken API - Historical OHLC data
  - Client: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Bitcoin/Providers/KrakenBitcoinHistoricalDataProvider.cs`
  - Endpoint: `https://api.kraken.com/0/public/OHLC?pair=XBTUSD&interval=1440&since={timestamp}`
  - Auth: None required (public API)
  - Limitation: Maximum 2 years of historical data
  - Update frequency: 120 seconds via `BitcoinHistoryUpdaterJob`

**Historical Fiat Rates:**
- Frankfurter API - Historical rates (primary)
  - Client: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/FrankfurterFiatHistoricalDataProvider.cs`
  - Endpoint: `https://api.frankfurter.dev/v1/{startDate}..{endDate}?base=USD&symbols={currencies}`

- CurrencyApi - Historical rates (fallback)
  - Client: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/CurrencyApiFiatHistoricalDataProvider.cs`

- Static CSV - Built-in baseline data
  - Client: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/StaticCsvFiatHistoricalDataProvider.cs`
  - No network calls required

## Data Storage

**Databases:**
- LiteDB - Embedded NoSQL database
  - Local Database: `src/Valt.Infra/DataAccess/LocalDatabase.cs`
    - Stores: Accounts, Transactions, Categories, Fixed Expenses, AvgPrice Profiles, Settings
    - Security: Password-protected with encrypted connection
    - Connection type: Direct file access
  - Price Database: `src/Valt.Infra/DataAccess/PriceDatabase.cs`
    - Stores: Historical Bitcoin and fiat prices
    - Shared across application instances
    - Optimized for rapid rate lookups

**Migrations:**
- Manager: `src/Valt.Infra/DataAccess/Migrations/MigrationManager.cs`
- Scripts: `src/Valt.Infra/DataAccess/Migrations/Scripts/`

**File Storage:**
- Not applicable (desktop application with local storage only)

**Caching:**
- Account cache: `src/Valt.Infra/Modules/Budget/Accounts/Services/AccountCacheService.cs`
- Price cache: In-memory via `IPriceDatabase` queries

## Version Management

**GitHub Releases API:**
- Purpose: Check for application updates
- Client: `src/Valt.Infra/Services/Updates/GitHubUpdateChecker.cs`
- Endpoint: `https://api.github.com/repos/btcdoomguy/valt/releases/latest`
- Auth: None required (public repository)
- User-Agent: `Valt-Desktop-App`
- Features: Version comparison, release notes, asset download links

## Background Jobs

| Job | Interval | Purpose | External API |
|-----|----------|---------|--------------|
| `LivePricesUpdaterJob` | 30s | Fetch current BTC/fiat rates | Coinbase, Frankfurter/CurrencyApi |
| `BitcoinHistoryUpdaterJob` | 120s | Update historical BTC data | Kraken |
| `FiatHistoryUpdaterJob` | 120s | Update historical fiat rates | Frankfurter/CurrencyApi |
| `AutoSatAmountJob` | 120s | Calculate satoshi equivalents | Local (uses cached prices) |
| `AccountTotalsJob` | 5s | Refresh wealth calculations | Local only |

## Network Configuration

**HTTP Timeouts:**
- Live price providers: 5 seconds
- Historical price providers: 10 seconds
- GitHub API: 10 seconds

**Error Handling:**
- Graceful degradation with logging
- Fallback providers for critical data
- Silent failure for optional data

**Offline Support:**
- All price data cached in LiteDB
- Application functions without network
- Cached rates used for calculations

## Integration Registration

**DI Registration:** `src/Valt.Infra/Extensions.cs`

```
Price Providers:
- IBitcoinPriceProvider → CoinbaseProvider
- IFiatPriceProvider → FrankfurterFiatRateProvider, CurrencyApiFiatRateProvider
- IFiatPriceProviderSelector → FiatPriceProviderSelector

Historical Providers:
- IBitcoinHistoricalDataProvider → KrakenBitcoinHistoricalDataProvider
- IFiatHistoricalDataProvider → FrankfurterFiatHistoricalDataProvider, CurrencyApiFiatHistoricalDataProvider, StaticCsvFiatHistoricalDataProvider

Update Checker:
- IUpdateChecker → GitHubUpdateChecker
```

## API Key Requirements

**All integrated services are free tier, no API keys required:**
- Coinbase public endpoints
- Kraken public endpoints
- Frankfurter free API
- CurrencyApi free tier
- GitHub public API (60 requests/hour unauthenticated)

---

*Integration audit: 2026-01-13*
*Update when adding/removing external services*
