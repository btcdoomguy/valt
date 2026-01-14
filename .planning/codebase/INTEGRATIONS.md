# External Integrations

**Analysis Date:** 2026-01-14

## APIs & External Services

**Live Bitcoin Price:**
- Coinbase - Real-time BTC price in multiple fiat currencies
  - SDK/Client: Direct HTTP via `HttpClient`
  - Endpoint: `https://api.coinbase.com/v2/exchange-rates?currency=BTC`
  - Auth: None (public API)
  - File: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Bitcoin/Providers/CoinbaseProvider.cs`
  - Frequency: 30-second updates via `LivePricesUpdaterJob`

**Live Fiat Rates (Primary):**
- Frankfurter - USD to fiat exchange rates
  - SDK/Client: Direct HTTP via `HttpClient`
  - Endpoint: `https://api.frankfurter.dev/v1/latest?base=USD&symbols={currencies}`
  - Auth: None (public API)
  - File: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Fiat/Providers/FrankfurterFiatRateProvider.cs`
  - Supported: 31 fiat currencies

**Live Fiat Rates (Fallback):**
- CurrencyApi - Fallback USD to fiat rates
  - SDK/Client: Direct HTTP via `HttpClient`
  - Endpoint: `https://latest.currency-api.pages.dev/v1/currencies/usd.json`
  - Auth: None (public API)
  - File: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Fiat/Providers/CurrencyApiFiatRateProvider.cs`
  - Supported: 34 fiat currencies (includes PYG, UYU)

**Historical Bitcoin Prices:**
- Kraken - Daily BTC OHLC data
  - SDK/Client: Direct HTTP via `HttpClient`
  - Endpoint: `https://api.kraken.com/0/public/OHLC?pair=XBTUSD&interval=1440&since={timestamp}`
  - Auth: None (public API)
  - File: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Bitcoin/Providers/KrakenBitcoinHistoricalDataProvider.cs`
  - Limitation: Maximum 2 years of historical data
  - Frequency: 120-second updates via `BitcoinHistoryUpdaterJob`

**Historical Fiat Rates (Primary):**
- Frankfurter - Historical USD to fiat rates
  - Endpoint: `https://api.frankfurter.dev/v1/{startDate}..{endDate}?base=USD&symbols={currencies}`
  - Auth: None (public API)
  - File: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/FrankfurterFiatHistoricalDataProvider.cs`
  - Frequency: 120-second updates via `FiatHistoryUpdaterJob`

**Historical Fiat Rates (Fallback):**
- CurrencyApi Historical - Fallback historical fiat rates
  - File: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/CurrencyApiFiatHistoricalDataProvider.cs`
  - Supported: 34 fiat currencies

**Static Data Fallback:**
- StaticCsvFiatHistoricalDataProvider - Embedded CSV with historical rates
  - File: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/StaticCsvFiatHistoricalDataProvider.cs`
  - Purpose: Fallback when APIs unavailable

## Data Storage

**Databases:**
- LiteDB Local Database - Primary application data
  - Connection: File-based with password protection
  - File: `src/Valt.Infra/DataAccess/LocalDatabase.cs`
  - Contents: Accounts, transactions, categories, fixed expenses, settings
  - Location: `{appDataPath}/valt.db`

- LiteDB Price Database - Historical price data
  - Connection: File-based, unencrypted
  - File: `src/Valt.Infra/DataAccess/PriceDatabase.cs`
  - Contents: Historical BTC prices, fiat exchange rates
  - Location: `{appDataPath}/prices.db`

**File Storage:**
- None - All data in LiteDB, no external file storage

**Caching:**
- In-memory account cache via `AccountCacheService`
  - File: `src/Valt.Infra/Modules/Budget/Accounts/Services/AccountCacheService.cs`
  - Purpose: Performance optimization for account totals

## Authentication & Identity

**Auth Provider:**
- None - Local application, no user authentication
- Database password protection handled at LiteDB level

**OAuth Integrations:**
- None

## Monitoring & Observability

**Error Tracking:**
- CrashReportService - Local crash report files
  - File: `src/Valt.Infra/Kernel/CrashReportService.cs`
  - Location: `{appDataPath}/crash-reports/`
  - No external error tracking service

**Analytics:**
- None - No telemetry or analytics

**Logs:**
- Console logging via Microsoft.Extensions.Logging
- Background job logging via `JobLoggerProvider`
  - File: `src/Valt.Infra/Kernel/BackgroundJobs/JobLoggerProvider.cs`

## Version Management

**GitHub Update Checker:**
- Purpose: Check for application updates
- Endpoint: `https://api.github.com/repos/btcdoomguy/valt/releases/latest`
- File: `src/Valt.Infra/Services/Updates/GitHubUpdateChecker.cs`
- Auth: None (public repository)
- User-Agent: "Valt-Desktop-App"
- Timeout: 10 seconds

## Background Jobs

| Job | Type | Interval | Purpose | File |
|-----|------|----------|---------|------|
| LivePricesUpdaterJob | PriceDatabase | 30s | Fetch live BTC/fiat prices | `src/Valt.Infra/Crawlers/LivePriceCrawlers/LivePricesUpdaterJob.cs` |
| BitcoinHistoryUpdaterJob | PriceDatabase | 120s | Update historical BTC prices | `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Bitcoin/BitcoinHistoryUpdaterJob.cs` |
| FiatHistoryUpdaterJob | PriceDatabase | 120s | Update historical fiat rates | `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/FiatHistoryUpdaterJob.cs` |
| AutoSatAmountJob | ValtDatabase | 120s | Calculate sat amounts for transactions | `src/Valt.Infra/Modules/Budget/Transactions/Services/AutoSatAmountJob.cs` |
| AccountTotalsJob | ValtDatabase | 5s | Refresh account balance cache | `src/Valt.Infra/Modules/Budget/Accounts/Services/AccountTotalsJob.cs` |

## Environment Configuration

**Development:**
- No environment variables required
- All data stored locally in LiteDB files
- No external service dependencies for core functionality

**Production:**
- Same as development (local-first architecture)
- Price APIs are public and require no credentials
- Database files in user's AppData directory

## Network Resilience

**Timeout Handling:**
- 5-10 second timeouts on all API calls
- Graceful fallbacks to alternative providers

**Error Handling:**
- Silent fallbacks in price crawlers
- Multiple provider support (Frankfurter + CurrencyApi)
- Embedded CSV fallback for historical data

**Offline Mode:**
- Core functionality works offline with cached prices
- Price updates deferred until connectivity restored

## Security Notes

**API Keys:**
- None required - all public APIs

**Database Encryption:**
- Local database password-protected via LiteDB
- Price database unencrypted (non-sensitive data)

---

*Integration audit: 2026-01-14*
*Update when adding/removing external services*
