# Technology Stack

**Analysis Date:** 2026-05-27

## Languages

**Primary:**
- C# 14 - All application code across all four projects

**Secondary:**
- XAML - Avalonia UI views and controls (`src/Valt.UI/Views/**/*.axaml`)
- RESX - Localization strings (`src/Valt.UI/Lang/language.resx`, `language.pt-BR.resx`, `language.es.resx`)
- JSON - Icon font mappings, configuration, API responses

## Runtime

**Environment:**
- .NET 10.0 (`net10.0`)
- Desktop application targeting Windows, Linux, macOS

**Package Manager:**
- NuGet with Central Package Management (`Directory.Packages.props`)
- Lockfile: Not used (CPM manages versions centrally)

## Frameworks

**Core:**
- Avalonia 12.0.3 - Cross-platform desktop UI framework
- Avalonia.Desktop 12.0.3 - Desktop platform support
- Avalonia.Themes.Fluent 12.0.3 - Fluent design theme
- Avalonia.Fonts.Inter 12.0.3 - Inter font family
- Avalonia.Controls.ColorPicker 12.0.3 - Color picker control
- Avalonia.Controls.DataGrid 12.0.0 - Data grid control

**MVVM:**
- CommunityToolkit.Mvvm 8.4.2 - MVVM toolkit (source generators for `[ObservableProperty]`, `[RelayCommand]`)

**Web (Embedded MCP Server):**
- Microsoft.AspNetCore.App (FrameworkReference) - Kestrel web server for embedded MCP

**Charting:**
- LiveChartsCore.SkiaSharpView.Avalonia 2.1.0-dev-365 - Chart rendering
- SkiaSharp.NativeAssets.Linux 3.119.4-preview.1.1 - Skia native libraries for Linux

**Build/Dev:**
- Microsoft.NET.Sdk - Standard .NET SDK project format
- Self-contained publish with single-file deployment (`PublishSingleFile=true`)

## Key Dependencies

**Critical:**
- LiteDB 5.0.21 - Embedded NoSQL database for local data and price history storage
- ModelContextProtocol.AspNetCore 1.3.0 - MCP server implementation for AI assistant integration
- CommunityToolkit.Mvvm 8.4.2 - MVVM pattern implementation with source generators
- Microsoft.Extensions.DependencyInjection 9.0.10 - DI container

**Infrastructure:**
- CsvHelper 33.1.0 - CSV import/export functionality
- Scrutor 6.1.0 - Assembly scanning for DI registration
- StringMath 4.1.3 - Mathematical expression evaluation
- YahooFinanceApi 2.3.3 - Asset price fetching from Yahoo Finance
- Microsoft.Extensions.Logging 9.0.10 - Structured logging

**Testing:**
- NUnit 4.4.0 - Test framework
- NSubstitute 5.3.0 - Mocking framework
- NetArchTest.Rules 1.3.2 - Architecture validation tests
- coverlet.collector 3.2.0 - Code coverage

## Configuration

**Environment:**
- No `.env` file or environment-variable-based configuration detected
- Configuration stored in LiteDB (`system_config` and `system_settings` collections)
- Database file path and password configured at runtime through UI

**Build:**
- `Directory.Packages.props` - Central package version management
- `Valt.sln` - Solution file
- `.editorconfig` - Editor configuration
- `Valt.sln.DotSettings.user` - ReSharper/Rider user settings

**Avalonia:**
- `AvaloniaUseCompiledBindingsByDefault=true` - Compiled bindings enabled
- `BuiltInComInteropSupport=true` - COM interop support
- `app.manifest` - Windows application manifest

## Platform Requirements

**Development:**
- .NET 10 SDK (10.0.100 or later)
- Compatible with JetBrains Rider, Visual Studio, VS Code

**Production:**
- Self-contained deployment for:
  - `win-x64` - Windows x64
  - `linux-x64` - Linux x64
  - `osx-x64` - macOS x64 (experimental)
- No runtime installation required on target machines

---

*Stack analysis: 2026-05-27*
