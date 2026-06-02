# Discussion Log — Phase 02: Core Implementation

**Date:** 2026-05-27
**Phase:** 02 — Core Implementation

## Gray Areas Discussed

### 1. Data Aggregation Strategy

**Question:** Should we aggregate monthly totals in the database query or in the ViewModel?

**Options presented:**
- Database-side (LiteDB query aggregates by month)
- In-memory (load transactions, aggregate in ViewModel)
- Hybrid (cache monthly aggregates in ViewModel)

**User decision:** Database-side (LiteDB query aggregates by month)

**Rationale captured:** Faster initial load for 5 years of data. The query returns pre-aggregated monthly totals, and the ViewModel caches and filters in-memory for real-time category selection.

## Discussion Notes

- User only wanted to discuss the data aggregation strategy
- Other implementation details (chart colors, category selector behavior, missing data handling) will follow existing patterns
- Key requirement: real-time updates when categories change, even with database-side aggregation
- Performance target: <500ms for 5 years of data

## Deferred Ideas

None captured during this discussion.

## Agent Discretion Items

- Chart will reuse existing WealthOverviewChartData pattern with dual axes
- Category selector will reuse CategoryTreeElement from ManageCategories
- Cost of living calculation: first month vs last month in selected range
- Missing PriceInSats: show warning banner, exclude from sats calculation
