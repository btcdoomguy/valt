# Valt 🚀

**Personal budget management for bitcoiners** – tailored for Windows and Linux users.

*No cloud, no API, no ads, no tracking.* Sovereign and free. 💪

Valt is a *simple and easy-to-use* budget management app designed specifically for bitcoiners, as traditional apps and SaaS solutions aren't built for us. With Valt, you can view the most important details about your wealth, all priced in **bitcoin**. 📈

## Features ✨

- 💼 **Infinite Accounts**: Create unlimited accounts to represent your bitcoin wallets and fiat wallets.
- 📝 **Transaction Tracking**: Register spending, income, and bitcoin transactions with ease.
- 🔄 **Automatic Exchange Calculation**: When transferring between fiat and bitcoin accounts, the app auto-calculates the exchange rate and bitcoin equivalent.
- 📊 **Price History**: Track a full history of bitcoin and fiat prices in USD terms.
- 📉 **Reports Module**: Generate graphical reports of your transactions, check all your monthly and yearly evolution, track your cashflow.
- 📈 **Average Price Calculation**: Calculate the average price of your bitcoin transactions/other assets and create profiles for different scenarios.
- 🧮 **Bitcoin-Term Calculations**: Every transaction is calculated in bitcoin terms using the BTC closing price of that day.
- 🔍 **Cost Analysis**: See how much your transaction cost in bitcoin on the date it occurred – and its value based on the current bitcoin price.
- 📅 **Fixed Expenses**: Add and track all your recurring expenses to avoid missing payments!
- 🌐 **Real-Time Prices**: Displays the current bitcoin price and your favorite fiat currency in real time.
- 📊 **Cost of Living Evolution**: Visualize how your cost of living changes in fiat vs. bitcoin terms.
- 🔒 **Safe Mode**: Hide sensitive values during app usage for privacy.
- 📤 **Export/Import Data**: Easily export and import your data.
- 🌍 **Language Support**: Available in en-US and pt-BR.
- 💱 **Fiat Currencies Support**: Supports a wide range of fiat ~~currencies~~.

With Valt, you can easily keep track of your finances and stay in control of your spending. You can also use it to track historical data about each bitcoin purchase or spending, getting the price of bitcoin on each historical date and checking the evolution of your wealth.

## Roadmap 🛤️

- 💲 **More Fiat Currencies**: Expand support for additional fiat currencies.
- 🎨 **Interface & UX Improvements**: Enhance the overall look, feel, and user experience.
- 🔕 **Latest Changes Window**: A dedicated view for recent updates and changes.
- 🌎 **More Languages**: Add support for additional languages.

## macOS Packaging 🍎

Valt can be packaged as a native macOS application for Apple Silicon (arm64). The repository includes scripts to build and package the app:

### Prerequisites
- .NET 10 SDK
- Avalonia UI
- `create-dmg` (install via `brew install create-dmg`)

### Build and Package
```bash
# Build the app and create the .app bundle
./scripts/package-macos.sh

# Create a distributable DMG image
./scripts/create-dmg.sh
```

The generated `Valt.app` can be dragged to `/Applications`. The DMG image (`Valt-macos-arm64.dmg`) provides a simple installer with a default Finder background.

**Note:** The app is signed with an ad‑hoc signature for local use. For distribution outside your machine, consider obtaining an Apple Developer ID and performing proper notarization.

## Disclaimer ⚠️

This is a *personal project* and not affiliated with any company or organization.  
The app is not intended for commercial purposes.  
It's in **beta**, so expect bugs and missing features. 🐛

Follow on X: [@btcchicofatal](https://x.com/btcchicofatal)