# Requirements: Valt — Bitcoin Price Simulation

**Defined:** 2026-06-01
**Core Value:** Users can understand their financial life denominated in bitcoin, making it clear how fiat inflation affects their purchasing power while maintaining a complete view of their wealth.

## v1 Requirements

### User Interface

- [ ] **UI-01**: Fixed price bar in Resumo panel displays current BTC price in main fiat currency
- [ ] **UI-02**: "Simular" button in fixed price bar opens price input modal
- [ ] **UI-03**: Modal allows input of custom BTC price (non-negative, in main fiat currency)
- [ ] **UI-04**: "Alterar Preço" button shown when custom price is active (replaces "Simular")
- [ ] **UI-05**: "Resetar" button resets price to real-time BTC price (visible only when custom price is active)
- [ ] **UI-06**: Visual indication when custom price is active (e.g., highlighted state, badge, or color change)

### Data & Logic

- [ ] **DATA-01**: Custom BTC price does not persist across sessions (resets on app close)
- [ ] **DATA-02**: Custom price only affects Reports tab calculations (other app areas remain unaffected)
- [ ] **DATA-03**: Service layer accepts optional custom price parameter without breaking existing consumers
- [ ] **DATA-04**: Portfolio calculations (total value, leveraged positions, etc.) use custom price when active
- [ ] **DATA-05**: Input validation prevents negative prices and handles edge cases (zero, very large values)

### Integration

- [ ] **INT-01**: Architecture tests pass with refactored services (no layer violations)
- [ ] **INT-02**: No regression in other app areas using same services (existing functionality preserved)
- [ ] **INT-03**: All UI strings localized in en-US, pt-BR, and es

## v2 Requirements

(None yet — all features fit in v1.1 scope)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Custom price persistence across sessions | Intentionally session-only per user decision |
| Custom price in other tabs/modules | Scope limited to Reports tab only |
| Multiple custom price presets | Single custom price is sufficient for simulation |
| Custom fiat currency selection | Always use main fiat currency per user decision |
| Historical price simulation | Only current price override, not time-travel |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| UI-01 | Phase 1 | Pending |
| UI-02 | Phase 1 | Pending |
| UI-03 | Phase 1 | Pending |
| UI-04 | Phase 2 | Pending |
| UI-05 | Phase 2 | Pending |
| UI-06 | Phase 2 | Pending |
| DATA-01 | Phase 2 | Pending |
| DATA-02 | Phase 2 | Pending |
| DATA-03 | Phase 2 | Pending |
| DATA-04 | Phase 2 | Pending |
| DATA-05 | Phase 2 | Pending |
| INT-01 | Phase 3 | Pending |
| INT-02 | Phase 3 | Pending |
| INT-03 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 14 total
- Mapped to phases: 14
- Unmapped: 0 ✓

---
*Requirements defined: 2026-06-01*
*Last updated: 2026-06-01 after initial definition*
