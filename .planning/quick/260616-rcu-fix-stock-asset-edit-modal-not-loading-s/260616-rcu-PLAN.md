---
phase: "260616"
plan: rcu
type: execute
wave: 1
depends_on: []
files_modified:
  - src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs
  - tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs
autonomous: true
requirements: []

must_haves:
  truths:
    - "Editing a Stock asset opens the Manage Asset modal with the stored AcquisitionDate visible in the date picker"
    - "Saving the edited asset preserves the acquisition date"
  artifacts:
    - path: src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs
      provides: "AcquisitionDate property bound to CalendarDatePicker"
    - path: tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs
      provides: "Regression test for acquisition date load"
  key_links:
    - from: ManageAssetViewModel.AcquisitionDate
      to: ManageAssetView.axaml CalendarDatePicker
      pattern: 'SelectedDate="{Binding AcquisitionDate}"'
---

<objective>
Fix the Stock asset edit modal so the stored acquisition date is loaded into the date picker.

Purpose: Users expect to see and edit the acquisition date they previously saved. Currently the date picker appears empty when editing a Stock asset.
Output: A corrected ViewModel property type, passing regression tests, and a clean build.
</objective>

<execution_context>
@/home/vmabellini/.config/opencode/gsd-core/workflows/execute-plan.md
</execution_context>

<context>
@/home/vmabellini/RiderProjects/valt/AGENTS.md
@/home/vmabellini/RiderProjects/valt/src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs
@/home/vmabellini/RiderProjects/valt/src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetView.axaml
@/home/vmabellini/RiderProjects/valt/tests/Valt.Tests/UI/Screens/UpdateLoanStateViewModelTests.cs
</context>

<tasks>

<task type="auto">
  <name>Align AcquisitionDate VM property with CalendarDatePicker binding</name>
  <files>src/Valt.UI/Views/Main/Modals/ManageAsset/ManageAssetViewModel.cs</files>
  <action>
    Change the `_acquisitionDate` backing field (and generated `AcquisitionDate` property) from `DateTimeOffset?` to `DateTime?` to match the other date fields in the same ViewModel (e.g. `LoanStartDate`, `RepaymentDateOffset`) and the Avalonia `CalendarDatePicker.SelectedDate` binding.

    Then update the four `DateOnly.FromDateTime(AcquisitionDate.Value.DateTime)` calls in `CreateNewAssetAsync` and `EditExistingAssetAsync` to `DateOnly.FromDateTime(AcquisitionDate.Value)`. The two load assignments that use `assetDto.AcquisitionDate.Value.ToDateTime(TimeOnly.MinValue)` already convert correctly to `DateTime?` via implicit conversion and do not need changes.

    Do not change the XAML; `SelectedDate="{Binding AcquisitionDate}"` remains correct once the property type matches.
  </action>
  <verify>
    <automated>dotnet build Valt.sln</automated>
  </verify>
  <done>Solution builds with 0 errors and `AcquisitionDate` is now `DateTime?`.</done>
</task>

<task type="auto">
  <name>Add regression tests for acquisition date prefill</name>
  <files>tests/Valt.Tests/UI/Screens/ManageAssetViewModelTests.cs</files>
  <action>
    Create a new NUnit test fixture following the pattern in `UpdateLoanStateViewModelTests.cs`. Mock `IQueryDispatcher`, `ICommandDispatcher`, `IAssetPriceProviderSelector`, `IConfigurationManager`, `ILogger<ManageAssetViewModel>`, `ILocalDatabase`, and `INotificationPublisher` using NSubstitute. Construct `CurrencySettings` with the mocked dependencies and set `MainFiatCurrency = "USD"`.

    Add tests that:
    1. Load a Stock `AssetDTO` with `AcquisitionDate = new DateOnly(2024, 1, 15)`, call `OnBindParameterAsync`, and assert `vm.AcquisitionDate.Value.Date` equals `new DateTime(2024, 1, 15).Date`.
    2. Load a RealEstate `AssetDTO` with `AcquisitionDate = new DateOnly(2020, 6, 1)` and assert the same mapping.
    3. Load a Stock `AssetDTO` with no acquisition date and assert `vm.AcquisitionDate` is null.
  </action>
  <verify>
    <automated>dotnet test tests/Valt.Tests/Valt.Tests.csproj --filter "FullyQualifiedName~ManageAssetViewModelTests"</automated>
  </verify>
  <done>All new ManageAssetViewModel acquisition-date tests pass.</done>
</task>

</tasks>

<threat_model>
## Trust Boundaries

| Boundary | Description |
|----------|-------------|
| (none new) | This is a UI binding/type fix; no new external input boundary is introduced. |

## STRIDE Threat Register

| Threat ID | Category | Component | Disposition | Mitigation Plan |
|-----------|----------|-----------|-------------|-----------------|
| T-rcu-01 | Information Disclosure | ManageAssetViewModel test data | accept | Test data is synthetic; no PII or live secrets used. |
</threat_model>

<verification>
- `dotnet build Valt.sln` completes with 0 errors.
- `dotnet test tests/Valt.Tests/Valt.Tests.csproj --filter "FullyQualifiedName~ManageAssetViewModelTests"` passes.
- Runtime check: editing a Stock asset with a saved acquisition date shows the date in the picker.
</verification>

<success_criteria>
- The Manage Asset modal loads the stored acquisition date for Stock assets.
- The fix does not break RealEstate acquisition date loading.
- A regression test guards the behavior for future changes.
</success_criteria>

<output>
Create `.planning/quick/260616-rcu-fix-stock-asset-edit-modal-not-loading-s/260616-rcu-SUMMARY.md` when done.
</output>
