# Phase 50: Goal Summary Modal UI - Pattern Map

**Mapped:** 2026-10-06
**Files analyzed:** 11 (3 new, 7 modified, 1 test-modify)
**Analogs found:** 11 / 11

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml` (new) | component (modal view) | request-response (read-only render) | `src/Valt.UI/Views/Main/Modals/LoanStateHistory/LoanStateHistoryView.axaml` | exact |
| `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml.cs` (new) | component (code-behind) | n/a | `.../LoanStateHistory/LoanStateHistoryView.axaml.cs` | exact |
| `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs` (new) | component (modal VM) | transform (DTO → formatted rows) | `.../LoanStateHistory/LoanStateHistoryViewModel.cs` + `.../FixedExpenseHistory/FixedExpenseHistoryViewModel.cs` | exact |
| `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelViewModel.cs` (modify) | component (panel VM) | request-response | `src/Valt.UI/Views/Main/Tabs/Assets/AssetsViewModel.cs` (`OpenLoanStateHistory`) + own `RecalculateGoalAsync` | exact |
| `src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs` (modify) | component (item VM) | transform | own `CanRecalculate` property (lines 194-195) | exact |
| `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelView.axaml` (modify) | component (view) | event-driven (context menu) | own Recalculate `MenuItem` (lines 144-149) | exact |
| `src/Valt.UI/Views/ApplicationModalNames.cs` (modify) | config (enum) | n/a | `LoanStateHistory = 38` entry | exact |
| `src/Valt.UI/Extensions.cs` (modify) | config (DI composition root) | n/a | `LoanStateHistoryViewModel` registration (line 158) + factory case (lines 304-307) | exact |
| `src/Valt.UI/Lang/language.resx` (modify) | config (localization) | n/a | `Goals_Recalculate` data entry (line 2027) | exact |
| `src/Valt.UI/Lang/language.Designer.cs` (modify) | config (localization) | n/a | `Goals_Recalculate` static property (lines 4044-4048) | exact |
| `tests/Valt.Tests/UI/Screens/GoalsPanelViewModelTests.cs` (modify) | test | n/a | own existing command tests (`CreateViewModel` helper, lines 71-84) | exact |

No files lack an analog — every file has a direct in-codebase precedent.

---

## Pattern Assignments

### 1. `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml` (new — modal view)

**Analog:** `src/Valt.UI/Views/Main/Modals/LoanStateHistory/LoanStateHistoryView.axaml` (primary), `.../FixedExpenseHistory/FixedExpenseHistoryView.axaml` (template columns)

**Window chrome pattern** (LoanStateHistoryView.axaml lines 1-27) — copy verbatim, changing sizes per UI-SPEC (`d:DesignWidth="800" d:DesignHeight="560"`, `MinWidth="720" MinHeight="480"`, **no** MaxWidth/MaxHeight — user-resizable, unlike the fixed-size LoanStateHistory):

```xml
<Window xmlns="https://github.com/avaloniaui"
        ...
        xmlns:userControls="clr-namespace:Valt.UI.UserControls"
        xmlns:lang="clr-namespace:Valt.UI.Lang"
        xmlns:goalSummary="clr-namespace:Valt.UI.Views.Main.Modals.GoalSummary"
        mc:Ignorable="d"
        x:Class="Valt.UI.Views.Main.Modals.GoalSummary.GoalSummaryView"
        Title="{Binding WindowTitle}"
        x:DataType="goalSummary:GoalSummaryViewModel"
        d:DesignWidth="800" d:DesignHeight="560"
        MinWidth="720" MinHeight="480"
        WindowStartupLocation="CenterOwner"
        ExtendClientAreaToDecorationsHint="True"
        WindowDecorations="None"
        ExtendClientAreaTitleBarHeightHint="0">
    <Design.DataContext>
        <goalSummary:GoalSummaryViewModel />
    </Design.DataContext>
    <Window.KeyBindings>
        <KeyBinding Gesture="Escape" Command="{Binding CloseCommand}" />
    </Window.KeyBindings>
```

**CustomTitleBar + root grid pattern** (lines 28-34):

```xml
    <Border>
        <Grid RowDefinitions="Auto, *, Auto">
            <userControls:CustomTitleBar Grid.Row="0" Title="{Binding WindowTitle}"
                                         TitleBarPressed="CustomTitleBarButtonPressed"
                                         CloseClick="CustomTitleBarCloseClicked" />

            <Grid Grid.Row="1" RowDefinitions="Auto, *" Margin="12">
```

Note: UI-SPEC spacing contract sets root content `Margin="12"` (multiple-of-4), replacing the LoanStateHistory `Margin="10"` precedent.

**Read-only DataGrid pattern** (lines 42-50) — the GoalSummary grid is the same chrome, minus selection:

```xml
                <DataGrid Grid.Row="1"
                          GridLinesVisibility="All"
                          ItemsSource="{Binding Rows}"
                          CanUserResizeColumns="True"
                          IsReadOnly="True"
                          ScrollViewer.HorizontalScrollBarVisibility="Auto">
```

**Template column precedent** (FixedExpenseHistoryView.axaml lines 59-73) — use `DataGridTemplateColumn` + inner `TextBlock` for cells needing `TextTrimming="CharacterEllipsis"` (Description, Account, Category) or semantic foreground (Fiat, Sats, Running total). `DataGridTextColumn` cannot set per-cell TextTrimming/Foreground:

```xml
<DataGridTemplateColumn Header="{x:Static lang:language.GoalSummary_ColumnDescription}" Width="*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate DataType="goalSummary:GoalSummaryViewModel+RowItemViewModel">
            <TextBlock Text="{Binding Description}"
                       FontSize="{DynamicResource FontSizeSmall}"
                       FontFamily="{DynamicResource Geist}"
                       TextTrimming="CharacterEllipsis"
                       VerticalAlignment="Center" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**Semantic foreground binding** — per UI-SPEC Color contract, amount cells bind Foreground to row-level brush properties derived from the RunningTotal delta sign:

```xml
<TextBlock ... Foreground="{Binding ContributionForeground}" ... />
<!-- ContributionForeground returns SemanticPositive200Brush / SemanticNegative200Brush / null (default) -->
```

Brush keys already exist (`src/Valt.UI/Styles/ColorResources.axaml` lines 103, 114): `SemanticNegative200Brush`, `SemanticPositive200Brush`.

**Close-button footer pattern** (LoanStateHistoryView.axaml lines 70-81) — copy verbatim, `Width="100"` per UI-SPEC exception:

```xml
            <Border Grid.Row="2"
                    Classes="form-fields-area"
                    VerticalAlignment="Stretch"
                    Margin="12"
                    Padding="10">
                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                    <Button Content="{x:Static lang:language.GoalSummary_Close}"
                            HorizontalContentAlignment="Center"
                            Command="{Binding CloseCommand}"
                            Width="100" />
                </StackPanel>
            </Border>
```

**Empty state** (no direct analog — new per UI-SPEC): toggle grid vs. empty panel with `IsVisible="{Binding HasRows}"` / `{Binding !HasRows}"`, empty panel = `Padding="16"` StackPanel with heading + body `TextBlock`s at `FontSizeSmall`, `Foreground="{DynamicResource Text400Brush}"`.

---

### 2. `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryView.axaml.cs` (new — code-behind)

**Analog:** `LoanStateHistoryView.axaml.cs` (entire file, 11 lines) — copy verbatim:

```csharp
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.GoalSummary;

public partial class GoalSummaryView : ValtBaseWindow
{
    public GoalSummaryView()
    {
        InitializeComponent();
    }
}
```

`ValtBaseWindow` wires `CustomTitleBarButtonPressed`/`CustomTitleBarCloseClicked` and `GetUserControlOwnerWindow` automatically (see `src/Valt.UI/Base/ValtBaseWindow.cs` line 33).

---

### 3. `src/Valt.UI/Views/Main/Modals/GoalSummary/GoalSummaryViewModel.cs` (new — modal VM)

**Analog:** `LoanStateHistoryViewModel.cs` (structure: ValtModalViewModel + Request + row item record + CloseCommand), `FixedExpenseHistoryViewModel.cs` (design-time constructor pattern)

**Class skeleton + dual constructor pattern** (LoanStateHistoryViewModel.cs lines 23-68):

```csharp
public partial class GoalSummaryViewModel : ValtModalViewModel
{
    private readonly IQueryDispatcher _queryDispatcher = null!;

    [ObservableProperty] private string _windowTitle = language.GoalSummary_Title;
    [ObservableProperty] private string _goalName = string.Empty;
    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private string _finalTotalFormatted = string.Empty;
    [ObservableProperty] private bool _hasRows;

    public AvaloniaList<RowItemViewModel> Rows { get; set; } = new();

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public GoalSummaryViewModel()
    {
        if (!Design.IsDesignMode) return;
        // seed 2-3 design-time rows so the AXAML preview renders
    }

    public GoalSummaryViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
    }
```

**IMPORTANT DEVIATION from analog** (per 50-CONTEXT.md line 29 + UI-SPEC loading row): unlike `LoanStateHistoryViewModel` which loads async in `OnBindParameterAsync` (lines 70-77), the GoalSummary modal receives the **already-fetched** query result in its Request. `ViewSummaryCommand` in `GoalsPanelViewModel` awaits `DispatchAsync(new GetGoalContributingTransactionsQuery { ... })` BEFORE calling `_modalFactory.CreateAsync(...)`. The modal VM's `OnBindParameterAsync` only projects rows + header data. No loading chrome, no async-load-in-modal.

**Request record + row projection pattern** (LoanStateHistoryViewModel.cs lines 172-192, adapted):

```csharp
    public override Task OnBindParameterAsync()
    {
        if (Parameter is not Request request)
            return Task.CompletedTask;

        GoalName = request.GoalName;
        PeriodLabel = request.PeriodLabel;

        // request.Result is GoalContributingTransactionsResult (Phase 49 contract)
        if (request.Result is not GoalContributingTransactionsResult.Supported supported)
            return Task.CompletedTask;   // unreachable: CanViewSummary gates NetWorthBtc out

        HasRows = supported.Rows.Count > 0;
        decimal previousRunningTotal = 0m;
        foreach (var (row, index) in supported.Rows.Select((r, i) => (r, i)))
        {
            var contribution = index == 0 ? row.RunningTotal : row.RunningTotal - previousRunningTotal;
            Rows.Add(new RowItemViewModel
            {
                DateFormatted = row.Date.ToShortDateString(),   // precedent: LoanStateHistoryViewModel.cs line 90
                Description = row.Description,
                AccountName = row.AccountName,
                CategoryName = row.CategoryName ?? string.Empty,
                FiatFormatted = row.FiatAmount.Value == 0m
                    ? string.Empty                                   // sats-only row: empty cell, never fabricated 0 (Phase 49 Q3)
                    : CurrencyDisplay.FormatFiat(row.FiatAmount.Value, row.FiatCurrencyCode),
                SatsFormatted = row.SatsAmount.Sats == 0
                    ? string.Empty                                   // fiat-only row: empty cell
                    : $"{CurrencyDisplay.FormatSatsAsNumber(row.SatsAmount.Sats)} {language.SatsLabel}",
                RunningTotalFormatted = FormatRunningTotal(row.RunningTotal, request.StrategyUnit),
                ContributionForeground = GetContributionBrush(contribution)
            });
            previousRunningTotal = row.RunningTotal;
        }

        if (supported.Rows.Count > 0)
            FinalTotalFormatted = FormatRunningTotal(supported.Rows[^1].RunningTotal, request.StrategyUnit);

        return Task.CompletedTask;
    }

    public record Request
    {
        public required string GoalId { get; init; }
        public required string GoalName { get; init; }
        public required string PeriodLabel { get; init; }
        public required GoalContributingTransactionsResult Result { get; init; }
        public required GoalSummaryStrategyUnit StrategyUnit { get; init; }   // Fiat | Sats | Count | Percentage
    }
```

**Per-strategy formatting contract** (UI-SPEC Formatting Contract; formatters at `src/Valt.Infra/Kernel/CurrencyDisplay.cs`):

| StrategyUnit | Source goal types (per `ContributingTransactionRow.RunningTotal` doc, lines 43-49) | Format |
|---|---|---|
| Fiat | SpendingLimit, SaveFiat, IncomeFiat, ReduceExpenseCategory | `CurrencyDisplay.FormatFiat(value, rowCurrency)` (line 24) |
| Sats | StackBitcoin, IncomeBtc, BitcoinHodl | `CurrencyDisplay.FormatSatsAsNumber((long)value)` (line 19) |
| Count | Dca | `{value:N0}` |
| Percentage | SavingsRate | `{value:N1}%` |

Strategy unit is derivable from `GoalDTO.GoalType` — same switch idiom as `GoalEntryViewModel.FriendlyName` (lines 40-55). `GoalEntryViewModel` already exposes the typed DTO, so `CanViewSummary` and the unit derivation both key off `_goal.GoalType is NetWorthBtcGoalTypeOutputDTO`.

**CloseCommand pattern** (LoanStateHistoryViewModel.cs lines 160-165) — copy verbatim:

```csharp
    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }
```

---

### 4. `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelViewModel.cs` (modify — add `ViewSummaryCommand`)

**Analog:** own `RecalculateGoalAsync` (lines 218-238, error display) + `AssetsViewModel.OpenLoanStateHistory` (lines 617-634, modal open flow)

**Command pattern** — dispatch query first (Result-wrapped, new for this VM), then open modal:

```csharp
    [RelayCommand]
    private async Task ViewSummary(GoalEntryViewModel? entry)
    {
        if (entry is null)
            return;

        var result = await _queryDispatcher.DispatchAsync(
            new GetGoalContributingTransactionsQuery { GoalId = entry.Id });

        if (result.IsFailure)
        {
            await MessageBoxHelper.ShowErrorAsync(
                language.Error, result.Error!.Message, GetUserControlOwnerWindow()!);
            return;
        }

        var ownerWindow = GetUserControlOwnerWindow();
        var modal = (GoalSummaryView)await _modalFactory.CreateAsync(
            ApplicationModalNames.GoalSummary,
            ownerWindow,
            new GoalSummaryViewModel.Request
            {
                GoalId = entry.Id,
                GoalName = entry.FriendlyName,
                PeriodLabel = entry.GetPeriodLabel(),          // derive from entry.Period/RefDate, e.g. "(01/25)"
                Result = result.Value!,
                StrategyUnit = entry.GetSummaryStrategyUnit()  // derived from _goal.GoalType
            });

        await modal.ShowDialogSafeAsync<GoalSummaryViewModel.Response?>(ownerWindow!);
    }
```

Precedent details:
- Error display: `MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetUserControlOwnerWindow()!)` — `GoalsPanelViewModel.cs` line 231.
- Secure-mode gating is in XAML (`IsEnabled="{Binding !IsSecureModeEnabled}"`), matching Edit/Delete menu items — do NOT add an early-return guard (consistent with `EditGoal`/`DeleteGoal` which rely on XAML gating; `AssetsViewModel` uses VM guards because its triggers are buttons, not menu items).

---

### 5. `src/Valt.UI/Views/Main/Tabs/Transactions/Models/GoalEntryViewModel.cs` (modify — add `CanViewSummary`)

**Analog:** own `CanRecalculate` (lines 194-195):

```csharp
    // Context menu visibility
    public bool CanRecalculate => true;
```

**New property** — placed in the same "Context menu visibility" region, plus notification in `UpdateGoal` alongside `CanRecalculate` (line 226):

```csharp
    public bool CanViewSummary => _goal.GoalType is not NetWorthBtcGoalTypeOutputDTO;
```

Also add `OnPropertyChanged(nameof(CanViewSummary));` in `UpdateGoal` (after line 226). Note `NetWorthBtc = 9` is the only `GoalTypeNames` value whose progress is not transaction-derived (Phase 49 contract: `NetWorthBtc` returns `NotSupported`).

---

### 6. `src/Valt.UI/Views/Main/Tabs/Transactions/GoalsPanelView.axaml` (modify — add MenuItem)

**Analog:** own Recalculate MenuItem (lines 144-149):

```xml
                                <MenuItem
                                    Header="{x:Static local:language.Goals_Recalculate}"
                                    Command="{Binding $parent[UserControl].((vm:GoalsPanelViewModel)DataContext).RecalculateGoalCommand}"
                                    CommandParameter="{Binding .}"
                                    IsVisible="{Binding CanRecalculate}">
                                </MenuItem>
```

**New item** — inserted after the Recalculate item, before the `<Separator />`:

```xml
                                <MenuItem
                                    Header="{x:Static local:language.Goals_ViewSummary}"
                                    Command="{Binding $parent[UserControl].((vm:GoalsPanelViewModel)DataContext).ViewSummaryCommand}"
                                    CommandParameter="{Binding .}"
                                    IsVisible="{Binding CanViewSummary}"
                                    IsEnabled="{Binding !$parent[UserControl].((vm:GoalsPanelViewModel)DataContext).IsSecureModeEnabled}">
                                </MenuItem>
```

`IsVisible` (hidden, per locked decision) differs from Recalculate which has no `IsEnabled` — the secure-mode disable matches the Edit item (lines 138-143).

---

### 7. `src/Valt.UI/Views/ApplicationModalNames.cs` (modify)

**Analog:** `LoanStateHistory = 38` (line 38). Next free value is 43 (after `BtcLoanSimulator = 42`, line 42):

```csharp
    BtcLoanSimulator = 42,
    GoalSummary = 43
```

---

### 8. `src/Valt.UI/Extensions.cs` (modify — two insertions)

**Analog A:** `services.AddTransient<LoanStateHistoryViewModel>();` (line 158) — add in the `//modals` block:

```csharp
        services.AddTransient<GoalSummaryViewModel>();
```

**Analog B:** factory switch case (lines 304-307):

```csharp
                ApplicationModalNames.GoalSummary => new GoalSummaryView()
                {
                    DataContext = services.GetRequiredService<GoalSummaryViewModel>(),
                },
```

---

### 9. `src/Valt.UI/Lang/language.resx` (modify — ~13 new keys)

**Analog:** `Goals_Recalculate` entry (`language.resx` line 2027):

```xml
    <data name="Goals_Recalculate" xml:space="preserve">
        <value>Recalculate</value>
    </data>
```

**Keys to add** (neutral/en only — pt-BR/es are Phase 51 GOL-09, per CONTEXT.md line 31):

| Key | Value |
|---|---|
| `Goals_ViewSummary` | View summary |
| `GoalSummary_Title` | Goal summary |
| `GoalSummary_PeriodLabel` | Period |
| `GoalSummary_TotalLabel` | Total |
| `GoalSummary_ColumnDate` | Date |
| `GoalSummary_ColumnDescription` | Description |
| `GoalSummary_ColumnAccount` | Account |
| `GoalSummary_ColumnCategory` | Category |
| `GoalSummary_ColumnFiat` | Fiat |
| `GoalSummary_ColumnSats` | Sats |
| `GoalSummary_ColumnRunningTotal` | Running total |
| `GoalSummary_EmptyTitle` | No transactions yet |
| `GoalSummary_EmptyMessage` | No transactions contribute to this goal yet. |
| `GoalSummary_Close` | Close |

Add under a `<!-- Goal Summary Modal -->` comment section. Note: `Transactions_Columns_Date`/`Transactions_Columns_Description` keys already exist (used by FixedExpenseHistoryView.axaml lines 53-55) and may be reused for Date/Description per UI-SPEC "reuse existing transaction-grid date key if present" — planner decides; reusing avoids 2 keys.

---

### 10. `src/Valt.UI/Lang/language.Designer.cs` (modify — one property per key)

**Analog:** `Goals_Recalculate` property (lines 4044-4048):

```csharp
        public static string Goals_Recalculate {
            get {
                return ResourceManager.GetString("Goals_Recalculate", resourceCulture);
            }
        }
```

**Pattern per key:**

```csharp
        public static string GoalSummary_Title {
            get {
                return ResourceManager.GetString("GoalSummary_Title", resourceCulture);
            }
        }
```

Insert adjacent to the existing Goals_* properties block (around line 4032-4080).

---

### 11. `tests/Valt.Tests/UI/Screens/GoalsPanelViewModelTests.cs` (modify — add ViewSummary tests)

**Analog:** own harness (lines 25-104): `DatabaseTest` base, NSubstitute dispatchers, `CreateViewModel()` helper, `WeakReferenceMessenger.Default.Reset()` in SetUp/TearDown, `CreateStackBitcoinGoalDTO`/`CreateSpendingLimitGoalDTO` DTO factories.

**New tests to add:**
- `ViewSummary_NullEntry_DoesNothing`
- `ViewSummary_QueryFailure_ShowsErrorAndDoesNotOpenModal` — mock `_queryDispatcher.DispatchAsync(Arg.Any<GetGoalContributingTransactionsQuery>(), ...)` to return `Result<GoalContributingTransactionsResult>.Failure(new Error("GOAL_NOT_FOUND", "..."))`; assert `_modalFactory` not called.
- `ViewSummary_SupportedResult_OpensModalWithRequest` — mock success with `GoalContributingTransactionsResult.Supported(rows)`; assert `_modalFactory.Received(1).CreateAsync(Arg.Is(ApplicationModalNames.GoalSummary), Arg.Any<Window>(), Arg.Any<GoalSummaryViewModel.Request>())`.
- `GoalEntryViewModel_CanViewSummary_FalseForNetWorthBtc` / `_TrueForStackBitcoin` — DTO factory for NetWorthBtc (mirror `CreateStackBitcoinGoalDTO` with `NetWorthBtcGoalTypeOutputDTO`; fields per existing usage in `GoalEntryViewModel` line 101-102: `TargetSats`, `CalculatedSats`).

---

## Shared Patterns

### Modal chrome (applies to files 1, 2, 3)
**Source:** `LoanStateHistoryView.axaml` lines 1-32
Chromeless `Window` (`WindowDecorations="None"` + `ExtendClientAreaToDecorationsHint="True"`), `userControls:CustomTitleBar`, Escape→`CloseCommand`, `WindowStartupLocation="CenterOwner"`. Deviations locked by UI-SPEC: no MaxWidth/MaxHeight; root content margin 12 (not 10).

### Error display (applies to file 4)
**Source:** `GoalsPanelViewModel.cs` lines 229-233
```csharp
if (result.IsFailure)
{
    await MessageBoxHelper.ShowErrorAsync(language.Error, result.Error!.Message, GetUserControlOwnerWindow()!);
    return;
}
```
Query returning `Result<T>` is new to this VM (existing queries return bare values), but the Result handling itself is identical to `RecalculateGoalAsync`'s command handling. Modal does NOT open on failure (UI-SPEC error row).

### Formatting (applies to file 3)
**Source:** `src/Valt.Infra/Kernel/CurrencyDisplay.cs` lines 19-40; precedent usage `LoanStateHistoryViewModel.cs` lines 90-96:
- `CurrencyDisplay.FormatFiat(decimal, string currencyCode)` — 2dp + symbol, culture-aware.
- `CurrencyDisplay.FormatSatsAsNumber(long)` — space-grouped thousands.
- `row.Date.ToShortDateString()` for dates.

### Secure-mode gating (applies to files 5, 6)
**Source:** `GoalsPanelView.axaml` lines 138-143 (Edit item)
`IsEnabled="{Binding !$parent[UserControl].((vm:GoalsPanelViewModel)DataContext).IsSecureModeEnabled}"`. `IsSecureModeEnabled` on `GoalsPanelViewModel` line 104 (`_secureModeState.IsEnabled`), change-notified via `SecureModeStateOnPropertyChanged` (lines 81-87).

### Localization triple (applies to files 9, 10 — and consumed by 1, 6)
**Source:** `language.resx` data entry + `language.Designer.cs` static property + AXAML `{x:Static local:language.<Key>}` (usage precedent: `GoalsPanelView.axaml` line 139). Per AGENTS.md, all resx keys get Designer.cs properties; only `language.resx` (en) gets values this phase.

### Modal registration (applies to files 7, 8)
**Source:** `Extensions.cs` lines 158, 304-307; `ApplicationModalNames.cs` line 38. Three coordinated edits: enum member → `AddTransient<TViewModel>()` → factory switch case. `ModalFactory.CreateAsync` (`src/Valt.UI/Services/ModalFactory.cs` lines 18-31) sets `OwnerWindow`/`Parameter` and awaits `OnBindParameterAsync` before the window is shown.

---

## No Analog Found

None. Every file has an exact or role-match analog in the codebase.

## Metadata

**Analog search scope:** `src/Valt.UI/Views/Main/Modals/`, `src/Valt.UI/Views/Main/Tabs/Transactions/`, `src/Valt.UI/Services/`, `src/Valt.UI/Base/`, `src/Valt.UI/Lang/`, `src/Valt.App/Modules/Goals/`, `src/Valt.Infra/Kernel/`, `src/Valt.Core/Modules/Goals/`, `tests/Valt.Tests/UI/Screens/`
**Files scanned:** 20+ (primary analogs fully read: 14)
**Pattern extraction date:** 2026-10-06
