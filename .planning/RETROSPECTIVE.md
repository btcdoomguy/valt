# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v0.5 — Asset Sold History

**Shipped:** 2026-07-14
**Phases:** 3 | **Plans:** 12 | **Tasks:** 27

### What Was Built

- Asset aggregate first-class sold-state support (`IsSold`, `DateSold`, `PreviousVisibility`, `MarkAsSold`, `UndoSale`) with LiteDB persistence and test-builder support.
- CQRS command/validator/handler pipelines for `MarkAssetAsSold` and `UndoAssetSale` with `IClock`-driven date validation.
- Active/sold read-model split with `IAssetQueries` filtering so the UI, reports, and MCP share the same semantics.
- `AssetPriceUpdaterJob` sold-asset skip guard, proven by unit tests.
- Asset Sold History modal with DataGrid list, per-type details card, and Restore Asset undo action.
- `DateSoldPrompt` modal for date selection and the active-view Mark as Sold flow with optional proceeds transaction.
- Three new MCP tools (`MarkAssetAsSold`, `UndoAssetSale`, `ListSoldAssets`) and NUnit integration tests for the mark/undo/list cycle.
- English, Portuguese, and Spanish localization for all new user-facing strings.
- Updated `.claude/docs/assets.md` with sold-state behavior and MCP impact.

### What Worked

- **Query-layer filtering** centralized sold-state semantics for every consumer, preventing UI, reports, and MCP from diverging.
- **Reusing existing `AssetViewModel` and `AssetDTO` mapping** for the History details card avoided duplicating per-type layout logic.
- **Injecting `IClock`** into validators and handlers made date logic deterministic and testable.
- **Structured modal result (`WasRestored`)** gave the caller explicit control over refresh and avoided message-loop coupling.
- **Following existing CQRS patterns** (command/validator/handler split, `AssetUpdatedEvent` reuse) kept the change set idiomatic and reviewable.

### What Was Inefficient

- Hardcoded English strings in Phase 30 required a dedicated localization phase; future UI work should stub resource keys up front.
- The `GetLatestLoanState` MCP tool was accidentally removed during a tool-class edit and had to be restored, indicating that MCP tool edits need closer diff review.
- Two pre-existing live-API integration tests (`CoinGeckoProviderTests`, `BitcoinDominanceProviderTests`) failed with `403 Forbidden` during every full test gate, creating noise unrelated to the feature.

### Patterns Established

- **Sold-state commands:** `ICommand<Unit>` with separate validator and handler classes, date validation via `IClock`.
- **Active/sold query split:** `GetAllAsync` returns active assets only; `GetSoldAsync` returns sold assets ordered by `DateSold` descending.
- **Modal result pattern:** typed `Response` record with semantic flags; caller decides what to refresh.
- **Localized label punctuation:** use `Run` elements with a single `Title` key plus a literal colon, rather than duplicating a `Label` key.

### Key Lessons

1. Keep `IsSold` independent from `Visible` so undo can restore the original visibility state without collisions.
2. Do not assume LiteDB serializes `DateOnly?` natively; add a round-trip repository test to prove it.
3. When appending to a monolithic MCP tool class, diff against the file before committing to avoid accidentally deleting existing tools.
4. UI strings introduced in one phase should be resource-key stubs from the start, even if translations happen later.
5. Pre-existing environmental test failures should be filtered out of the feature gate early to avoid repeated false negatives.

### Cost Observations

- Model mix: primarily k2p7 (execution phase)
- Sessions: not tracked
- Notable: The localization plan (31-02) took longer than the average plan because it touched four resource files and the XAML/ViewModel binding surface; batching string changes by UI area reduces churn.

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Sessions | Phases | Key Change |
|-----------|----------|--------|------------|
| v0.5 | — | 3 | Explicit localization phase moved after UI implementation; future milestones should stub resx keys during UI construction. |

### Cumulative Quality

| Milestone | Tests | Coverage | Zero-Dep Additions |
|-----------|-------|----------|-------------------|
| v0.5 | 1666+ | — | 0 |

### Top Lessons (Verified Across Milestones)

1. Centralizing read-model filters prevents consumers from diverging on state semantics.
2. Reusing existing ViewModel/DTO mapping for new UI surfaces reduces duplication and keeps designs consistent.
3. MCP tool classes are high-risk edit targets; review diffs carefully to avoid accidental deletions.

---
