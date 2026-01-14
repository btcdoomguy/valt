# Technology Stack

**Analysis Date:** 2026-01-14

## Languages

**Primary:**
- C# 14 - All application code (LangVersion 14 in .csproj files)

**Secondary:**
- XAML - UI definitions (Avalonia AXAML files)
- JSON - Configuration and data files

## Runtime

**Environment:**
- .NET 10.0 (TargetFramework in all .csproj files)
- Desktop runtime (Windows/Linux)

**Package Manager:**
- NuGet
- Centralized package versions in `Directory.Packages.props`

## Frameworks

**Core:**
- Avalonia 11.3 - Cross-platform desktop UI framework
- CommunityToolkit.Mvvm - MVVM pattern implementation with source generators

**Testing:**
- NUnit 4.4.0 - Unit testing framework
- NSubstitute 5.3.0 - Mocking library
- NetArchTest.Rules 1.3.2 - Architecture validation tests
- coverlet.collector 3.2.0 - Code coverage collection

**Build/Dev:**
- MSBuild (dotnet build)
- Microsoft.NET.Test.Sdk 18.0.0 - Test infrastructure

## Key Dependencies

**Critical:**
- LiteDB - Embedded NoSQL database for local and price data (`src/Valt.Infra/DataAccess/`)
- CsvHelper - CSV import/export functionality (`src/Valt.Infra/Services/CsvImport/`)
- LiveChartsCore.SkiaSharpView.Avalonia - Charting library for reports

**Infrastructure:**
- Microsoft.Extensions.DependencyInjection - Dependency injection
- Microsoft.Extensions.Logging - Logging framework
- Scrutor - Assembly scanning for auto-registration of services
- StringMath - Mathematical expression evaluation

**UI:**
- SkiaSharp - Graphics rendering engine
- MessageBox.Avalonia - Dialog box implementation
- Avalonia.Controls.DataGrid - Data table component
- Avalonia.Svg.Skia - SVG rendering

## Configuration

**Environment:**
- No external config files (appsettings.json not used)
- Settings stored in LiteDB database (`src/Valt.Infra/Settings/`)
- Local storage via `LocalStorageService` for UI preferences

**Build:**
- `Valt.sln` - Solution file
- `Directory.Packages.props` - Centralized package versions
- `.editorconfig` - Editor formatting configuration

## Platform Requirements

**Development:**
- Any platform with .NET 10 SDK
- No external dependencies (LiteDB is embedded)
- JetBrains Rider or Visual Studio recommended

**Production:**
- Windows or Linux desktop
- Self-contained executable
- No external database required (all local LiteDB files)

---

*Stack analysis: 2026-01-14*
*Update after major dependency changes*
