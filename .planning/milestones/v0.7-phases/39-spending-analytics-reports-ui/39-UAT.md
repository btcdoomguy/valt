---
status: complete
phase: 39-spending-analytics-reports-ui
source:
  - 39-01-SUMMARY.md
  - 39-02-SUMMARY.md
  - 39-03-SUMMARY.md
  - 39-04-SUMMARY.md
  - 39-05-SUMMARY.md
  - 39-06-SUMMARY.md
  - 39-07-SUMMARY.md
  - 39-08-SUMMARY.md
started: 2026-08-05T00:00:00Z
updated: 2026-08-05T20:05:00Z
---

<!--
AGENT NOTE: Do not repeatedly update the `updated` timestamp just to "refresh" the file before presenting a checkpoint. Update the timestamp only when test results, summary counts, or gap statuses actually change, then immediately present the checkpoint to the user without further edits.
-->

## Current Test

[testing complete]

## Tests

### 1. Reports tab opens without errors
expected: Launch the app and click the Reports tab. The tab loads without crashing and shows the existing dashboard cards (Monthly totals, Statistics, etc.) and the new analytics sections.
result: pass

### 2. Burn rate card appears after Statistics
expected: In the Reports tab dashboard, a new "Burn rate" card is visible after the "Statistics" card. It has a header with the fire icon and the title "Burn rate".
result: pass

### 3. Burn rate card shows all metric rows
expected: The Burn rate card displays rows for "Spent so far (this month)", "Avg daily spend", "Projected month-end", "Median month (12 mo)", and "vs median" with fiat values and a signed percentage for vs median.
result: pass

### 4. Burn rate vs median uses green/red coloring
expected: The "vs median" row value is colored green when the projected month-end spend is less than or equal to the median, and red when it is above the median.
result: pass

### 5. Burn rate early-month note (days 1–4)
expected: If the current day of the month is between 1 and 4, the Burn rate card shows a muted note "Projection available from day 5" instead of the projected month-end and vs median rows.
result: pass

### 6. Savings rate section with line chart
expected: Below the dashboard cards, a new "Savings rate" expander section is visible and expanded by default. It shows a line chart with monthly savings-rate percentages.
result: pass

### 7. Savings rate chart shows gaps for zero-income months
expected: In the Savings rate line chart, months with no income appear as a break/gap in the line series rather than a zero point.
result: pass

### 8. Fixed vs variable section with stacked bar chart
expected: Below the Savings rate section, a new "Fixed vs variable expenses" expander section is visible. It shows a stacked bar chart with blue "Fixed" and orange "Variable" segments per month.
result: pass

### 9. Fixed vs variable empty state when no fixed expenses exist
expected: When no fixed expenses have been registered, the Fixed vs variable section shows a hint box with the heading "No fixed expenses registered" and body text pointing to the Fixed Expenses tab, instead of rendering an empty chart.
result: pass

### 10. Centralized category filter button in Reports tab
expected: Open the Reports tab. A centralized filter icon button (settings/gear icon) is visible in a top-right, right-aligned area above all sections, outside the Summary tab.
result: pass

### 18. Existing Statistics excluded-category settings migrate to the centralized filter
expected: If the user previously excluded categories via the old Statistics dashboard settings, those excluded categories are preserved and appear in the new centralized filter modal on first load after the update.
result: pass

### 11. Category filter modal opens with all categories
expected: Click the filter icon button. A modal opens titled "Category filter" (or similar) showing a list of all categories with checkboxes, all checked by default.
result: pass

### 12. Exclude a category and save
expected: In the modal, uncheck one expense category, click Save. The modal closes and the Reports analytics panels refresh.
result: pass

### 13. Burn rate panel updates after excluding a category
expected: After excluding a category, the Burn rate card values change — the "Spent so far" and "Avg daily spend" rows should decrease (or at least differ) if that category had expenses this month.
result: pass

### 14. Savings rate panel updates after excluding a category
expected: After excluding a category, the Savings rate line chart values change to reflect the excluded income/expenses.
result: pass

### 15. Fixed vs variable panel updates after excluding a category
expected: After excluding a category, the Fixed vs variable stacked bar chart values change to reflect the excluded category's fixed/variable expenses.
result: pass

### 16. Filter persists across tab navigation
expected: Switch to another tab (e.g., Transactions) and then back to Reports. Re-open the filter modal — the previously unchecked category is still unchecked.
result: pass

### 17. Clear filter restores all categories
expected: Re-open the filter modal, check all categories again, click Save. The Reports analytics panels revert to showing values that include all categories.
result: pass

## Summary

total: 18
passed: 18
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

- truth: "Burn rate calculation supports category exclusion filter similar to Statistics dashboard"
  status: resolved
  reason: "User reported: pass, BUT the feature as is right now should be improved. we should also fit this improvement on this phase 39. what we need here is a filter similar of the one in the Statistics dashboarddata, where the user can exclude some categories from being used to calculate these values"
  severity: major
  test: 3
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Fixed vs variable expenses calculation supports category exclusion filter similar to Statistics dashboard"
  status: resolved
  reason: "User reported: pass, BUT we also need the same category exclusion filter for this one, as mentioned before for the Burn Rate panel"
  severity: major
  test: 8
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Reports tab provides a centralized category exclusion filter button that applies to all relevant analytics panels (Burn rate, Savings rate, Fixed vs variable)"
  status: resolved
  reason: "User reported: thinking about that, we probably should centralize these filters in a button that applies the same filter for all relevant report info"
  severity: major
  test: 8
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""

- truth: "Centralized category filter button uses the Statistics setting icon, is placed outside the Summary tab in a top-right independent area, applies to Statistics DashboardData, and covers Fixed vs variable expenses"
  status: resolved
  reason: "User reported: yes! however, some adjustments must be done: - the DashboardData for Statistics should also use this filter and have his own setting button removed - the new Icon should use the same font/char used on this DashboardData Statistics setting button - this new button should be placed OUTSIDE the Summary tab, in a new independent area on top of all, right-aligned, because it should also be applied to the Fixed Expenses vs Variables"
  severity: major
  test: 10
  root_cause: "The centralized filter button was placed inside the Summary section header, uses a different icon (E3D3 vs the Statistics config E8B8), and the Statistics dashboard still uses its own separate excluded-category configuration instead of the new ReportsAnalyticsCategoryFilterExcluded key."
  artifacts:
    - path: "src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml"
      issue: "Filter button is nested inside the Summary Expander.Header instead of a top-level toolbar area."
    - path: "src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs"
      issue: "StatisticsData is constructed with OpenStatisticsConfigCommand and FetchStatisticsDataAsync reads StatisticsExcludedCategories from IConfigurationManager rather than the centralized analytics filter."
  missing:
    - "Move the filter button out of the Summary Expander to a top-right, right-aligned area above all sections."
    - "Change the filter button icon to match the Statistics dashboard config gear (E8B8)."
    - "Remove the Statistics dashboard's own configuration gear button."
    - "Make the Statistics dashboard consume the centralized ReportsAnalyticsCategoryFilterExcluded filter."
    - "Remove or deprecate the StatisticsConfig modal usage from the Reports tab."
  debug_session: ""

- truth: "Existing Statistics excluded-category settings migrate to the centralized ReportsAnalyticsCategoryFilterExcluded filter so users do not lose their configuration"
  status: resolved
  reason: "User reported: yes perfect. just one small adjustment. we should migrate the existing setup for the settings of the DashboardData Statistics to this generic one, so no user loses the current setup"
  severity: minor
  test: 18
  root_cause: ""
  artifacts: []
  missing: []
  debug_session: ""
