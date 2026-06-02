# External Integrations

**Analysis Date:** 2026-05-27

## APIs & External Services

**Bitcoin Price Providers (Live):**
- CoinGecko API - Live BTC prices in 32+ fiat currencies
  - Endpoint: `https://api.coingecko.com/api/v3/simple/price`
  - Rate-limited via `CoinGeckoRateLimiter`
  - Files: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Bitcoin/Providers/CoinGeckoProvider.cs`
- Coinbase API - Live BTC exchange rates (fallback)
  - Endpoint: `https://api.coinbase.com/v2/exchange-rates?currency=BTC`
  - Files: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Bitcoin/Providers/CoinbaseProvider.cs`

**Bitcoin Price Providers (Historical):**
- Kraken API - Historical BTC OHLC data (max 2 years back)
  - Endpoint: `https://api.kraken.com/0/public/OHLC`
  - Files: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Bitcoin/Providers/KrakenBitcoinHistoricalDataProvider.cs`
- Static CSV seed data - Initial historical BTC prices
  - Source: `https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/initial-seed-price.csv`
  - Files: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Bitcoin/BitcoinInitialSeedPriceProvider.cs`

**Fiat Exchange Rate Providers (Live):**
- Frankfurter API - Live fiat rates (30 currencies)
  - Endpoint: `https://api.frankfurter.dev/v1/latest`
  - Files: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Fiat/Providers/FrankfurterFiatRateProvider.cs`
- Currency API - Live fiat rates (all 34 currencies, fallback)
  - Endpoint: `https://latest.currency-api.pages.dev/v1/currencies/usd.json`
  - Files: `src/Valt.Infra/Crawlers/LivePriceCrawlers/Fiat/Providers/CurrencyApiFiatRateProvider.cs`

**Fiat Exchange Rate Providers (Historical):**
- Frankfurter API - Historical fiat rates
  - Endpoint: `https://api.frankfurter.dev/v1/{start}..{end}`
  - Files: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/FrankfurterFiatHistoricalDataProvider.cs`
- Currency API - Historical fiat rates (date-by-date, fallback)
  - Endpoint: `https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@{date}/v1/currencies/usd.json`
  - Files: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/CurrencyApiFiatHistoricalDataProvider.cs`
- Static CSV data - Initial historical fiat rates
  - Source: `https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/{currency}.csv`
  - Files: `src/Valt.Infra/Crawlers/HistoricPriceCrawlers/Fiat/Providers/StaticCsvFiatHistoricalDataProvider.cs`

**Market Indicators:**
- Alternative.me Fear & Greed Index
  - Endpoint: `https://api.alternative.me/fng/?limit=1&format=json`
  - Files: `src/Valt.Infra/Crawlers/Indicators/FearAndGreedProvider.cs`
- Bitcoin.com Charts API - Mayer Multiple, Rainbow Chart
  - Endpoint: `https://charts.bitcoin.com/api/v1/charts/{mayer|rainbow}`
  - Files: `src/Valt.Infra/Crawlers/Indicators/BitcoinComIndicatorsProvider.cs`
- CoinGecko Global API - Bitcoin dominance
  - Endpoint: `https://api.coingecko.com/api/v3/global`
  - Files: `src/Valt.Infra/Crawlers/Indicators/BitcoinDominanceProvider.cs`

**Asset Prices:**
- Yahoo Finance API - External investment prices (stocks, ETFs, crypto)
  - Client: `YahooFinanceApi` NuGet package
  - Files: `src/Valt.Infra/Modules/Assets/PriceProviders/YahooFinancePriceProvider.cs`

**Application Updates:**
- GitHub API - Check for new releases
  - Endpoint: `https://api.github.com/repos/btcdoomguy/valt/releases`
  - Files: `src/Valt.Infra/Services/Updates/GitHubUpdateChecker.cs`

**Documentation Links (UI only):**
- `https://btcdoomguy.github.io/valt-docs/` - User documentation
- `https://charts.bitcoin.com/mayer.html` - Mayer Multiple external chart
- `https://charts.bitcoin.com/rainbow.html` - Rainbow Chart external view
- `https://alternative.me/crypto/fear-and-greed-index/` - Fear & Greed external view
- `https://www.coingecko.com/en/global-charts` - Market dominance external view

## Data Storage

**Databases:**
- LiteDB 5.0.21 - Embedded NoSQL database
  - **Local database** (`accounts.db` or user-specified): Stores accounts, transactions, categories, fixed expenses, goals, avg price profiles, configuration, settings, assets
  - **Price database** (`prices.db` in app data): Stores historical BTC prices, historical fiat rates, market indicator snapshots
  - Password protection supported for local database
  - Files: `src/Valt.Infra/DataAccess/LocalDatabase.cs`, `src/Valt.Infra/DataAccess/PriceDatabase.cs`

**File Storage:**
- Local filesystem only - User chooses database file location
- App data directory for price database: `ValtEnvironment.AppDataPath`
- CSV import/export via file dialogs

**Caching:**
- In-memory indicator cache (`IIndicatorCache` / `IndicatorCache`)
- In-memory account totals cache refreshed every 5 seconds
- No distributed cache

## Authentication & Identity

**Auth Provider:**
- None - Desktop application with local-only access
- Database-level password protection via LiteDB
- No user accounts, OAuth, or external identity providers

## Monitoring & Observability

**Error Tracking:**
- None - Errors logged to console/file via Microsoft.Extensions.Logging

**Logs:**
- Microsoft.Extensions.Logging with Console provider (MCP server only)
- Application-level logging via injected `ILogger<T>` instances
- No external log aggregation (Splunk, Datadog, etc.)

## CI/CD & Deployment

**Hosting:**
- Desktop application - distributed via GitHub Releases

**CI Pipeline:**
- GitHub Actions (`https://github.com/btcdoomguy/valt`)
- Workflow: `.github/workflows/main.yml`
- Triggers on version tags (`v*.*.*`)
- Builds for Windows (win-x64), Linux (linux-x64), macOS (osx-x64)
- Self-contained single-file publish
- Artifact attestation via `actions/attest-build-provenance@v2`
- Release creation via `softprops/action-gh-release@v2`

## Environment Configuration

**Required env vars:**
- None - Application is fully self-contained

**Secrets location:**
- Database password: User-provided at runtime, stored only in memory
- No API keys required for any external service (all use public endpoints)

## Webhooks & Callbacks

**Incoming:**
- MCP HTTP endpoint: `http://localhost:{port}/mcp` (default port 5200, fallbacks 5201-5204)
- Stateless HTTP transport for Model Context Protocol
- Files: `src/Valt.Infra/Mcp/Server/McpServerService.cs`

**Outgoing:**
- None - Application does not call external webhooks

---

*Integration audit: 2026-05-27*
