---
status: complete
quick_id: 260702-ed9
date: 2026-07-02
---

# Quick Task 260702-ed9 Summary

## What was done

Fixed a regression introduced in `beda261` where fiat-collateral leveraged positions with a BTC symbol (e.g. BTC-PERP) were loaded as BTC-collateral positions in the Manage Asset modal. This caused the fiat-collateral input panel (Collateral, Leverage, Position Size) to be hidden, making the leverage panel appear missing.

## Changes

- `src/Valt.UI/Services/AssetFormBuilder.cs`
  - `LoadFromDto` now sets `IsBitcoinUnderlyingAsset` based only on `CollateralAssetTypeId == Btc`, removing the incorrect symbol/heuristic fallback.

- `tests/Valt.Tests/UI/Services/AssetFormBuilderTests.cs`
  - Updated `LoadFromDto_LeveragedPosition_RoundTripsValues` to expect `IsBitcoinUnderlyingAsset = false` for fiat-collateral BTC positions.
  - Added `LoadFromDto_LeveragedPosition_BtcCollateral_SetsBitcoinUnderlyingAsset` to verify BTC-collateral positions still set the flag correctly.

## Verification

- `dotnet test --filter "FullyQualifiedName~AssetFormBuilderTests"` passed (26 tests).
- `dotnet build Valt.sln` succeeded.
- Full test suite: 1607 passed, 3 failed (unrelated CoinGecko API 403 errors).

## Commit

- Code/test changes committed separately.
