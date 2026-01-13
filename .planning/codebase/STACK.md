# Technology Stack

**Analysis Date:** 2026-01-13

## Languages

**Primary:**
- C# 14 - All application code (`src/Valt.Core/Valt.Core.csproj`, `src/Valt.Infra/Valt.Infra.csproj`, `src/Valt.UI/Valt.UI.csproj`)

**Secondary:**
- XAML (Avalonia) - UI markup files (`.axaml` in `src/Valt.UI/Views/`)
- RESX - Localization files (`src/Valt.UI/Lang/language.resx`, `src/Valt.UI/Lang/language.pt-BR.resx`)

## Runtime

**Environment:**
- .NET 10 (net10.0 target framework)
- C# Language Version 14
- Nullable reference types enabled across all projects
- Implicit usings enabled

**Package Manager:**
- NuGet with Central Package Management
- Version management: `Directory.Packages.props` (33+ packages)
- No lock file (standard NuGet behavior)

## Frameworks

**Core:**
- Avalonia UI 11.3.7 - Cross-platform desktop framework
  - `Avalonia.Desktop` - Desktop implementation
  - `Avalonia.Themes.Fluent` - Fluent design theme
  - `Avalonia.Controls.DataGrid` - Table control
  - `Avalonia.Svg.Skia` - SVG rendering
  - Compiled bindings enabled

**Testing:**
- NUnit 4.4.0 - Unit testing framework
- NSubstitute 5.3.0 - Mocking library
- NetArchTest.Rules 1.3.2 - Architecture constraint testing
- coverlet.collector 3.2.0 - Code coverage

**Build/Dev:**
- MSBuild via .NET SDK
- No separate build tool (direct `dotnet build`)

## Key Dependencies

**Critical:**
- LiteDB 5.0.21 - Embedded NoSQL database with password protection (`src/Valt.Infra/DataAccess/`)
- CommunityToolkit.Mvvm 8.4.0 - MVVM framework (`src/Valt.UI/Base/ValtViewModel.cs`)
- LiveChartsCore.SkiaSharpView.Avalonia 2.0.0-rc6.1 - Charts for reports (`src/Valt.UI/Views/Main/Tabs/Reports/`)

**Infrastructure:**
- Microsoft.Extensions.DependencyInjection 9.0.10 - DI container
- Microsoft.Extensions.Logging 9.0.10 - Logging framework
- Scrutor 6.1.0 - Assembly scanning for DI auto-registration (`src/Valt.Infra/Extensions.cs`)

**Utilities:**
- StringMath 4.1.3 - Mathematical expression parsing (`src/Valt.UI/Views/Main/Modals/MathExpression/`)
- CsvHelper 33.1.0 - CSV reading for price data import

## Configuration

**Environment:**
- No environment variables required
- All configuration stored in LiteDB databases
- Settings classes: `CurrencySettings`, `DisplaySettings`, `UISettings` (`src/Valt.Infra/Settings/`)

**Build:**
- `Directory.Packages.props` - Central NuGet version management
- Individual `.csproj` files per project
- `Valt.sln` - Solution file

## Platform Requirements

**Development:**
- Any platform with .NET 10 SDK
- JetBrains Rider or Visual Studio recommended
- No external database required (LiteDB embedded)

**Production:**
- Desktop application (Windows, macOS, Linux via Avalonia)
- Distributed as standalone executable
- Local database files in user's data directory

---

*Stack analysis: 2026-01-13*
*Update after major dependency changes*
