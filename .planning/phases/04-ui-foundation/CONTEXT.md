# Phase 4: UI Foundation - Context

**Gathered:** 2026-06-01
**Status:** Ready for planning
**Source:** Manual research (gsd-sdk unavailable)

## Phase Boundary

This phase delivers the UI components for Bitcoin Price Simulation:
1. Fixed price bar in Resumo panel showing current BTC price
2. "Simular" button that opens a modal
3. Modal for inputting custom BTC price

**Out of phase scope:** Service refactoring, calculation logic, visual state changes (Phase 5)

## Decisions (Locked)

- Custom BTC price does NOT persist across sessions
- Custom price is always in main fiat currency
- Only affects Reports tab calculations
- Fixed price bar is a persistent bar at the top of Resumo section (not a dashboard card)

## Existing Patterns

### Modal Registration Pattern
1. Add enum value to `ApplicationModalNames.cs`
2. Add case to modal factory switch in `Extensions.cs`
3. Create View (.axaml) + ViewModel (.cs) in `Views/Main/Modals/{Name}/`
4. View inherits from `Window` with `SystemDecorations="None"`, `CustomTitleBar`
5. ViewModel inherits from `ValtModalViewModel`

### Localization Pattern
- Add strings to `language.resx`, `language.pt-BR.resx`, `language.es.resx`
- Add static property to `language.Designer.cs`
- Reference in XAML: `{x:Static lang:language.KeyName}`

### Existing Similar Feature
- `SimulatedPricesConfig` modal already exists for configuring multiple simulated price lines
- This new feature is a simpler, single price override
- Both can coexist; the new feature is a quick override while the existing one shows multiple scenarios

## Code References

- Reports tab view: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`
- Reports tab ViewModel: `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`
- Modal enum: `src/Valt.UI/Views/ApplicationModalNames.cs`
- Modal factory: `src/Valt.UI/Services/ModalFactory.cs` or `Extensions.cs`
- Localization: `src/Valt.UI/Lang/language.resx` (and .pt-BR.resx, .es.resx)
- Existing modal example: `src/Valt.UI/Views/Main/Modals/SimulatedPricesConfig/`
- Custom title bar: `src/Valt.UI/UserControls/CustomTitleBar.axaml`
- Fiat input control: `src/Valt.UI/UserControls/FiatInput.axaml`

## Risks

- The existing `SimulatedPricesConfig` modal name is similar to what we might want to call the new modal. Need distinct naming.
- The fixed price bar placement needs to be prominent but not intrusive in the Resumo section.

## Agent Discretion

- Exact visual design of the fixed price bar (colors, layout)
- Whether to reuse existing `FiatInput` control or create simpler input
- Exact modal dimensions (follow existing patterns: ~500x400)
