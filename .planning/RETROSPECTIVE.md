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

## Milestone: v0.6 — Documentation Site Refresh

**Shipped:** 2026-07-17
**Phases:** 7 | **Plans:** 17 | **Tasks:** 42

### What Was Built

- Updated public Valt documentation (`valt-docs`) to reflect all features shipped through v0.5, including the Assets page rewrite with Asset Sold History and BTC-backed Loans, the Reports page with custom BTC price simulation, the Goals page with all current goal types, and the Fixed Expenses page with record states.
- Corrected factual errors across Installation (SQLite → LiteDB), FAQ, Getting Started, Assets, and Reports pages in both Portuguese and English.
- Updated the MCP Server page to document the complete current toolset (90 tools), including AssetTools, loan-state tools, and sold-asset tools.
- Created a new Settings & Configuration page in both languages, wired it into the MkDocs navigation with dual `nav_translations`, and converted existing bare settings references into cross-links.
- Fixed pre-existing MCP tool-name drift by replacing 7 category tables with code-verified names, eliminating 28 phantom tool names and adding 32 missing real tools.
- Applied a 12-check content review matrix to all 18 v0.6 documentation files and produced the `38-QA-CHECKLIST.md` artifact.
- Verified the documentation site builds cleanly with `mkdocs build --strict` and reports 19 translated navigation elements.

### What Worked

- **Two-repo protocol** (app changes in `valt`, docs changes in `valt-docs`) kept documentation commits isolated and reviewable.
- **App-verbatim labels** from `language.resx` / `language.pt-BR.resx` prevented UI/docs drift and made the docs immediately credible to users.
- **Copy-paste-grade research examples** for the MCP tool tables eliminated hand-rolled errors and made the 28-phantom-name fix reliable.
- **Strict build gate** plus bilingual heading/table-row parity checks caught broken links and structural mismatches early.
- **Per-category `<!-- Source: -->` evidence comments** made it easy to trace doc content back to the source code.

### What Was Inefficient

- The `38-VERIFICATION.md` file was generated before the 38-03 QA sweep plan was executed, so the audit tool continued to report a stale `gaps_found` status even after QA-03 was closed. Future verification artifacts should be regenerated after gap-closure plans complete.
- `ROADMAP.md` was briefly corrupted during the 38-03 metadata update (Phase 35 details were overwritten with Phase 38 plan entries). This was caught and fixed, but it suggests ROADMAP edits should be validated with a diff before commit.
- The full v0.6 scope lived in a single ROADMAP file with detailed Phase Details; archiving collapsed the file and improved readability, but this should happen at the end of the milestone rather than mid-stream.

### Patterns Established

- **Two-repo commit protocol:** planning metadata in `valt`, docs content in `valt-docs`, with explicit `docs(XX-YY):` prefixes.
- **Bilingual parity checks:** heading count and table-row count must match between `.md` and `.en.md` files before a plan closes.
- **Code-verified tables:** MCP tool tables and similar reference tables must be copied verbatim from research examples that were grep-verified against source code, never hand-rolled.
- **QA checklist artifact:** final documentation phase must produce a `XX-QA-CHECKLIST.md` covering all touched files with accuracy, completeness, and tone checks.
- **Nav translation verification:** `mkdocs build` log must show the expected number of translated navigation elements (e.g., 19 for pt).

### Key Lessons

1. Public docs should mirror the app language files, not paraphrase them; exact labels prevent drift.
2. When a verification report finds gaps, regenerate it after the gap-closure plan so the audit artifact reflects reality.
3. Large reference tables (e.g., MCP tools) are high-risk for stale data; replace them as whole units from verified sources rather than editing individual cells.
4. The final phase of a docs milestone should explicitly own the QA checklist; do not assume earlier phases will cover it.
5. Archive the completed milestone's ROADMAP details immediately after close to keep the active roadmap readable.

### Cost Observations

- Model mix: primarily k2p7 (execution phase and milestone close)
- Sessions: not tracked
- Notable: The v0.6 milestone was documentation-only and completed in 7 sequential phases over 3 days; the final QA sweep plan (38-03) added only ~7 minutes but was essential to close QA-03.

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Sessions | Phases | Key Change |
|-----------|----------|--------|------------|
| v0.5 | — | 3 | Explicit localization phase moved after UI implementation; future milestones should stub resx keys during UI construction. |
| v0.6 | — | 7 | Documentation-only milestone adopted a two-repo protocol and a final QA checklist phase to ensure every touched file is reviewed. |

### Cumulative Quality

| Milestone | Tests | Coverage | Zero-Dep Additions |
|-----------|-------|----------|-------------------|
| v0.5 | 1666+ | — | 0 |
| v0.6 | — | — | 0 |

### Top Lessons (Verified Across Milestones)

1. Centralizing read-model filters prevents consumers from diverging on state semantics.
2. Reusing existing ViewModel/DTO mapping for new UI surfaces reduces duplication and keeps designs consistent.
3. MCP tool classes and reference tables are high-risk edit targets; review diffs carefully and replace from verified sources.
4. App-verbatim labels in documentation prevent UI/docs drift and improve trust.
5. A final QA checklist phase is cost-effective insurance for docs milestones.

---
