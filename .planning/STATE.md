---
gsd_state_version: 1.0
milestone: v0.6
milestone_name: — Documentation Site Refresh
current_phase: 37
current_phase_name: MCP Server Page Update
status: ready_to_plan
stopped_at: Phase 37 context gathered
last_updated: "2026-07-16T22:33:27.925Z"
last_activity: 2026-07-16
last_activity_desc: Phase 36 complete, transitioned to Phase 37
progress:
  total_phases: 7
  completed_phases: 5
  total_plans: 12
  completed_plans: 12
  percent: 71
---

# STATE.md

## Project Reference

See: .planning/PROJECT.md (updated 2026-07-15)

**Core value:** Users can see their entire financial picture — cash flow, investments, and loans — denominated in bitcoin, so they always know where they stand in sats.
**Current focus:** Phase 37 — MCP Server Page Update

## Current Position

Phase: 37 — MCP Server Page Update
Plan: Not started
Status: Phase complete — ready for verification
Last activity: 2026-07-16 — Phase 36 complete, transitioned to Phase 37

## Performance Metrics

**Velocity:**

- Total plans completed: 19
- Average duration: 18 min
- Total execution time: 43 min

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| Phase 29 | 4/4 | 18 min | 18 min |
| Phase 30 | 3/3 | - | - |
| Phase 31 | 5/5 | - | - |
| Phase 32 | 2/2 | 25 min | 12.5 min |
| 35 | 3 | - | - |
| 36 | 2 | - | - |

**Recent Trend:**

- Last 5 plans: 18 min
- Trend: —

*Updated after each plan completion*
| Phase 29 P02 | 8min | 3 tasks | 9 files |
| Phase 29 P03 | 5min | 3 tasks | 8 files |
| Phase 29 P04 | 5min | 2 tasks | 2 files |
| Phase 30-history-ui-and-details-reuse P01 | 11min | 3 tasks | 9 files |
| Phase 30-history-ui-and-details-reuse P02 | 6 min | 3 tasks | 7 files |
| Phase 30 P03 | 15 | 3 tasks | 2 files |
| Phase 31-mcp-localization-documentation-and-verification P01 | 5min | 2 tasks | 1 files |
| Phase 31 P02 | 20min | 3 tasks | 10 files |
| Phase 31 P03 | 5 min | 1 task | 1 file |
| Phase 31-mcp-localization-documentation-and-verification P04 | 8 | 1 tasks | 2 files |
| Phase 31-mcp-localization-documentation-and-verification P05 | 3 min | 2 tasks | 5 files |
| Phase 32 P01 | 20min | 3 tasks | 11 files |
| Phase 32 P02 | 5 min | 2 tasks | 2 files |
| Phase 33 P01 | 12 min | 2 tasks | 2 files |
| Phase 33 P02 | 5min | 2 tasks | 1 files |
| Phase 33-assets-page-rewrite P03 | 4min | 2 tasks | 2 files |
| Phase 34-reports-page-update P01 | 15 min | 2 tasks | 1 files |
| Phase 34-reports-page-update P02 | 4 min | 2 tasks | 1 files |
| Phase 36-fixed-expenses-page-enhancement P01 | 11 min | 3 tasks | 1 files |
| Phase 36-fixed-expenses-page-enhancement P02 | 3 min | 3 tasks | 1 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [v0.5 research]: Add `IsSold` and `DateSold` as first-class properties on the `Asset` aggregate root and `AssetEntity`, not inside the JSON details blob.
- [v0.5 research]: Filter sold assets at the query layer (`IAssetQueries`) so the UI, reports, MCP, and background jobs all share the same semantics.
- [v0.5 research]: Reuse the existing `AssetViewModel` and `AssetDTO` mapping for the History details panel.
- [v0.5 research]: Keep `IsSold` independent from `Visible` so the two concepts do not collide on undo or filtering.
- [Phase 29]: Asset.New kept backward-compatible optional parameters with sold-state defaults — Existing Asset.New call sites must continue to compile without modification. Added optional parameters with active defaults (isSold: false, dateSold: null, previousVisibility: true).
- [Phase 29]: Reused AssetUpdatedEvent instead of introducing new event types — Existing state-change methods (SetVisibility, SetIncludeInNetWorth) emit AssetUpdatedEvent. New MarkAsSold/UndoSale methods follow the same pattern to avoid expanding the event surface in this foundational plan.
- [Phase 29]: DateSold stored as DateOnly? directly on AssetEntity, verified by round-trip test — D-01 specifies DateOnly? on the entity. Research warned that LiteDB BSON natively supports DateTime, not DateOnly. The round-trip repository test proves DateOnly? serializes correctly for this codebase.
- [Phase 29]: Followed SetAssetVisibility command/validator/handler pattern for MarkAssetAsSold and UndoAssetSale
- [Phase 29]: Injected IClock into MarkAssetAsSoldValidator and Handler for future-date rejection and default-to-today behavior
- [Phase ?]: Filtered active vs. sold assets at the IAssetQueries layer so UI, reports, and MCP share the same semantics — Centralizing the filter in the query layer prevents consumers from diverging on sold-state semantics.
- [Phase ?]: Made AssetDTO sold-state fields non-required to avoid breaking existing design-time sample data and with expressions — Non-required init-only properties preserve existing object initializers and record with-expressions that do not set the new fields.
- [Phase ?]: Kept the AssetPriceUpdaterJob sold-asset skip guard in ShouldUpdatePrice so the IAssetRepository contract remains unfiltered — The plan called for an unfiltered repository call with filtering in ShouldUpdatePrice; placing the guard there avoids changing the IAssetRepository contract and keeps the job's data-fetch behavior consistent with other consumers.
- [Phase 30-history-ui-and-details-reuse]: Hardcoded English language strings for the new History UI; Phase 31 will add the corresponding resx entries.
- [Phase 30-history-ui-and-details-reuse]: Reused the existing AssetViewModel and AssetDTO mapping for the details card, keeping NetWorth/Visibility indicators hidden.
- [Phase 30-history-ui-and-details-reuse]: Hardcoded English strings for new user-facing text — Phase 31 will add the corresponding resx entries.
- [Phase 30-history-ui-and-details-reuse]: Combined Tasks 2 and 3 into a single commit to keep the build green — Renaming the command in the ViewModel alone broke the XAML binding until the view was also updated, so both changes were committed together.
- [Phase ?]: Reused CloseDialog typed-result mechanism instead of adding AssetSummaryUpdatedMessage subscription to AssetsViewModel to avoid coupling and refresh loops — Modal result is more explicit and avoids potential infinite loops since AssetsViewModel also sends AssetSummaryUpdatedMessage after its own loads
- [Phase ?]: Left AssetSummaryUpdatedMessage broadcast in modal for other listeners while making caller-side refresh authoritative for active Assets list — Keeps unrelated totals/listeners updated without making the Assets tab depend on the message for its own refresh
- [Phase ?]: Removed the unplanned SoldAssetHistory_DateSold_Label key and used SoldAssetHistory_DateSold_Title with a literal colon via Run elements to match the plan's 20-key list and Title+colon pattern.
- [Phase 31-03]: Updated Asset module docs as a first-class deliverable tied to v0.5 feature completeness, keeping the Domain/Application/UI/MCP sections in sync with the sold-state feature.
- [Phase 31-05]: v0.5 Asset Sold History milestone is ready for `/gsd-verify-work`: all feature tests pass and the user has signed off the end-to-end UI smoke test.
- [Phase ?]: Code wins over stale requirements docs: updated REQUIREMENTS.md ACC-03 and ACC-04 to match the four main tabs and nine asset types found in the Valt application code.
- [Phase ?]: All public docs terminology mirrors the app language files (language.resx / language.pt-BR.resx) to avoid UI/docs drift.
- [Phase ?]: Code wins over stale planning docs: updated ROADMAP.md success criteria and .claude/docs/assets.md to match the four main tabs and nine asset types found in the Valt application code. — When planning docs conflict with the code, correct the docs to reflect the code. This keeps ROADMAP.md success criteria and the internal Assets module doc authoritative for future phases.
- [Phase ?]: Added Record Proceeds prompt note to Mark as Sold section because app language files and AssetsViewModel.cs confirm it is a real wired feature. — Content accuracy: the prompt exists in language.resx / language.pt-BR.resx and is invoked in AssetsViewModel.cs after MarkAssetAsSoldCommand succeeds.
- [Phase ?]: Extended the export note to mention transaction CSV and average-price CSV export from the AvgPrice tab. — CsvExportService exposes both ExportTransactionsAsync and ExportAvgPriceLinesAsync, so the docs should accurately reflect both export paths.
- [Phase ?]: Documented Posições Alavancadas and Empréstimos BTC as conditional panels visible only when user has corresponding data. — ReportsViewModel binds IsLeveragePositionsVisible and IsBtcLoansVisible based on the presence of visible leveraged positions or active BTC-backed loans.
- [Phase ?]: Used exact English UI labels from language.resx for the English Reports page mirror — The plan requires app-aligned terminology; this keeps English docs consistent with the English UI and avoids drift.
- [Phase 36-fixed-expenses-page-enhancement]: Tightened DICAS paragraph to name exact right-click labels Ignorar para essa data and Marcar como pago (old prose covered ignore only vaguely and never mark-as-paid) — Plan optional-tightening clause applied: existing DICAS text did not already cover the right-click mark-as-paid action, so exact labels from language.pt-BR.resx were used while preserving link-to-transaction instructions
- [Phase 36-fixed-expenses-page-enhancement]: [Phase 36-fixed-expenses-page-enhancement] Mirrored wave-1 TIPS tightening to English page (exact labels Ignore for this date / Mark as paid) for full bilingual parity per QA-01 — Wave 1 rewrote the Portuguese DICAS paragraph with exact right-click labels; mirroring only the three structural additions would leave the English TIPS paragraph stale, violating QA-01 full-mirror requirement.

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260714-exz | The History page displays the description on the grid in a very weird format, check the picture: /home/vmabellini/Pictures/valt3 | 2026-07-14 | 87a83ae | [260714-exz-the-history-page-displays-the-descriptio](./quick/260714-exz-the-history-page-displays-the-descriptio/) |
| 260714-hbv | it is still bad. take a look at /home/vmabellini/Pictures/valt3/new.png. it should align properly. just remove the icon and render the description as plain text. also, the button Restore Asset should not occupy the entire horizontal space | 2026-07-14 | 4607275 | [260714-hbv-it-is-still-bad-take-a-look-at-home-vmab](./quick/260714-hbv-it-is-still-bad-take-a-look-at-home-vmab/) |
| 260714-i1p | when I mark as sold, the system asks for date but only month/year. it should use a date picker like the one on the transaction tab to also pick the day | 2026-07-14 | d79acfa | [260714-i1p-when-i-mark-as-sold-the-system-asks-for-](./quick/260714-i1p-when-i-mark-as-sold-the-system-asks-for-/) |
| 260714-kzm | Update Avalonia nuget packages from 12.0.3 to 12.1.0, research breaking changes first | 2026-07-14 | 36ddfe7 | [260714-kzm-update-avalonia-nuget-packages-from-12-0](./quick/260714-kzm-update-avalonia-nuget-packages-from-12-0/) |

## Deferred Items

Items acknowledged and carried forward from previous milestone close:

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| Quality | v0.4 quality/hardening items (async void cleanup, god-VM refactor, live-API test isolation, handler unit tests) | Deferred | v0.5 |
| Debug Session | empty-loan-context — Current Loan Context formatted properties are computed read-only properties | Investigating | v0.5 |
| Debug Session | translation-gaps — UpdateLoanState UI strings added to neutral resx file not code-generated | Diagnosed | v0.5 |
| Debug Session | visual-layout — hardcoded input widths exceed available column space and button widths | Diagnosed | v0.5 |
| Quick Task | 001-copy-modal-perf | Unknown | v0.5 |
| Quick Task | reports-summary-simulation | Missing | v0.5 |
| Quick Task | 260616-rcu-fix-stock-asset-edit-modal-not-loading-s | Unknown | v0.5 |

## Session Continuity

Last session: 2026-07-16T22:33:27.919Z
Stopped at: Phase 37 context gathered
Resume file: .planning/phases/37-mcp-server-page-update/37-CONTEXT.md

## Notes

- Phase 31 complete; v0.5 Asset Sold History milestone is ready for `/gsd-verify-work` and milestone closure.
- Two pre-existing live-API integration tests (`CoinGeckoProviderTests`, `BitcoinDominanceProviderTests`) failed with HTTP 403 during the final full test gate. They are unrelated to v0.5 and are logged in the phase deferred-items file.

## Operator Next Steps

- Start the next milestone with /gsd-new-milestone
