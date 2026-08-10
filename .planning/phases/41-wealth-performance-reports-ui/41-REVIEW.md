---
phase: 41-wealth-performance-reports-ui
reviewed: 2026-08-10T18:30:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs
  - src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighData.cs
  - src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs
  - src/Valt.UI/Lang/language.resx
  - src/Valt.UI/Lang/language.Designer.cs
  - tests/Valt.Tests/Reports/AllTimeHighReportTests.cs
findings:
  critical: 0
  warning: 4
  info: 2
  total: 6
status: issues_found
---

# Phase 41: Code Review Report

**Reviewed:** 2026-08-10T18:30:00Z
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

The Phase 41 tracer correctly wires active net-worth assets into `AllTimeHighReport`, exposes `DaysUnderWater` on `AllTimeHighData`, and renders the new metric in the existing ATH dashboard panel. DI registration for `IAssetQueries` is already present, so the new constructor dependency resolves without registration changes. The supplied tests pass and cover the main inclusion/exclusion and days-under-water scenarios.

However, the implementation has quality and correctness gaps: localization is incomplete (only English), the MCP layer does not surface the new `DaysUnderWater` field, SATS-denominated assets may be converted incorrectly, and there is an unhandled edge case where non-positive wealth leaves the ATH date at `DateOnly.MinValue` and risks a division-by-zero or a nonsensical days-under-water value.

## Critical Issues

No critical issues found.

## Warnings

### WR-01: Incomplete localization — only English resource updated

**File:** `src/Valt.UI/Lang/language.resx:833-835` and `src/Valt.UI/Lang/language.Designer.cs:1668-1672`
**Issue:** The new `Reports.AllTimeHigh.DaysUnderWater` string was added only to the neutral `language.resx` and the generated `language.Designer.cs`. The satellite resources `language.pt-BR.resx` and `language.es.resx` were not updated. This violates the project convention in `AGENTS.md` ("Update ALL THREE language files when adding strings"). Users running the app in Portuguese or Spanish will see the English fallback text for the new row label.
**Fix:** Add the following entry to both `language.pt-BR.resx` and `language.es.resx` now, or keep the existing D-14 deferral only if Phase 43 is guaranteed to backfill these strings and a tracking item is recorded:
```xml
<data name="Reports.AllTimeHigh.DaysUnderWater" xml:space="preserve">
  <value>Days under water</value>
</data>
```
(Provide the translated values in Phase 43.)

### WR-02: MCP AllTimeHigh DTO not updated with new DaysUnderWater metric

**File:** `src/Valt.Infra/Mcp/Tools/ReportTools.cs:345-354` and `src/Valt.Infra/Mcp/Tools/ReportTools.cs:195-204`
**Issue:** The `AllTimeHighResultDto` returned by the MCP `GetAllTimeHigh` tool does not expose the new `DaysUnderWater` value, even though the underlying report computes it and the UI panel renders it. The `AGENTS.md` MCP impact checklist requires that modifying queries/commands used by MCP tools be reflected in the corresponding tools/DTOs.
**Fix:** Add the new property to the DTO and map it in the tool:
```csharp
public class AllTimeHighResultDto
{
    // ... existing properties ...
    public required int DaysUnderWater { get; init; }
}
```
```csharp
return new AllTimeHighResultDto
{
    // ... existing mappings ...
    DaysUnderWater = data.DaysUnderWater
};
```

### WR-03: SATS-denominated assets may be converted incorrectly in ATH wealth

**File:** `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs:245-249`
**Issue:** `IsBtcCurrency` returns `true` for both "BTC" and "SATS", but the conversion path treats them identically: `asset.CurrentValue * GetUsdBitcoinPriceAt(...)`. For a SATS asset, `CurrentValue` is likely expressed in satoshis (the natural unit of the "SATS" currency code), so the result would be 100,000,000× too large unless the value is already normalized to BTC. This would inflate the reported ATH, shift the peak date, and produce a wildly wrong `DaysUnderWater` for users with SATS-denominated assets included in net worth.
**Fix:** Either divide by `SatoshisPerBitcoin` for SATS inside `CalculateActiveAssetsValue`, or explicitly reject SATS as an unsupported asset currency code and throw a clear exception. Add a regression test covering a SATS asset to document the expected behavior:
```csharp
var valueOnUsd = currencyCode.Equals("SATS", OrdinalIgnoreCase)
    ? assetValue / SatoshisPerBitcoin * _provider.GetUsdBitcoinPriceAt(currentScanDate)
    : assetValue * _provider.GetUsdBitcoinPriceAt(currentScanDate);
```

### WR-04: Edge case where ATH date remains DateOnly.MinValue produces nonsensical DaysUnderWater and division-by-zero risk

**File:** `src/Valt.Infra/Modules/Reports/AllTimeHigh/AllTimeHighReport.cs:62, 183-213`
**Issue:** `allTimeHighCurrentDate` is initialized to `DateOnly.MinValue` and only updates when `dateTotal > allTimeHighCurrentFiatValue`. If all daily wealth totals are ≤ 0 (for example, all accounts empty/negative and all assets excluded or valued at zero), the ATH date never advances from `DateOnly.MinValue`. The new `DaysUnderWater` calculation then returns the difference between the report end date and `DateOnly.MinValue` (~738,000 days). Additionally, `declineFromAth` divides `lastDayFiatValue` by `allTimeHighCurrentFiatValue` (still 0), which throws `DivideByZeroException`. This latent issue existed before but is now surfaced by the new metric.
**Fix:** Guard against a non-positive ATH before computing derived values. One option is to initialize the ATH date to `_startDate` and treat `allTimeHighCurrentFiatValue <= 0` as a special "no positive ATH" state, or make `DaysUnderWater` nullable so the UI can render "—" instead of a huge integer:
```csharp
int? daysUnderWater = null;
if (allTimeHighCurrentFiatValue > 0)
{
    daysUnderWater = _endDate.DayNumber - allTimeHighCurrentDate.DayNumber;
}
```
Correspondingly, guard `declineFromAth` and the max drawdown math to avoid division by zero, and render the row conditionally in `ReportsViewModel.FetchAllTimeHighDataAsync`.

## Info

### IN-01: UX inconsistency — existing "doesn't include Assets" disclaimer may contradict the new asset-aware ATH

**File:** `src/Valt.UI/Lang/language.resx:1025-1027`
**Issue:** The `Reports.DoesntIncludeAssets` string still states "* doesn't include Assets". If this string is shown anywhere near the All Time High panel or in general wealth disclaimers, it now conflicts with the fact that the ATH panel explicitly includes active net-worth assets. The string is not referenced in the changed ATH files, so it may only affect other panels, but it is worth verifying.
**Fix:** Audit all usages of `Reports.DoesntIncludeAssets` and either remove it (if no longer true anywhere) or scope it only to panels that still exclude assets.

### IN-02: Test coverage gap for SATS and non-USD asset currency conversions

**File:** `tests/Valt.Tests/Reports/AllTimeHighReportTests.cs:289-380`
**Issue:** The new tests cover a USD-denominated net-worth asset, non-net-worth/sold exclusion, and zero vs. positive `DaysUnderWater`. They do not cover a SATS-denominated asset, nor an asset with a currency other than USD converted to a non-USD report currency (e.g., EUR asset → BRL report). This leaves WR-03 and the cross-rate conversion path unverified.
**Fix:** Add tests for a SATS asset and a EUR-denominated asset included in a BRL report to lock in the conversion math.

---

_Reviewed: 2026-08-10T18:30:00Z_
_Reviewer: gsd-code-reviewer_
_Depth: standard_
