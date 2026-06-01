# Plan: Phase 4 — UI Foundation

**Phase:** 4  
**Name:** UI Foundation  
**Goal:** Create fixed price bar in Resumo panel with BTC price display, "Simular" button, and price input modal.  
**Requirements:** UI-01, UI-02, UI-03  
**Depends on:** None  

---

## Success Criteria

1. Fixed price bar is visible in Resumo panel showing current BTC price in main fiat currency
2. "Simular" button in the price bar opens a modal window
3. Modal allows input of custom BTC price with validation (non-negative, numeric)
4. Modal follows existing modal patterns (MinWidth/MinHeight, custom title bar, SystemDecorations=None)

---

## Tasks

### Task 1: Add localization strings
**Files:** `src/Valt.UI/Lang/language.resx`, `language.pt-BR.resx`, `language.es.resx`, `language.Designer.cs`

Add the following strings (with pt-BR and es translations):
- `Reports_FixedPriceBar_Title` - "BTC Price Simulation" / "Simulação de Preço BTC" / "Simulación de Precio BTC"
- `Reports_SimulateButton` - "Simulate" / "Simular" / "Simular"
- `Reports_FixedPriceModal_Title` - "Custom BTC Price" / "Preço Customizado de BTC" / "Precio Personalizado de BTC"
- `Reports_FixedPriceModal_Description` - "Enter a custom BTC price to simulate your portfolio value." / "Digite um preço customizado de BTC para simular o valor da sua carteira." / "Ingrese un precio personalizado de BTC para simular el valor de su cartera."
- `Reports_FixedPriceModal_PriceLabel` - "Price" / "Preço" / "Precio"
- `Reports_FixedPriceModal_Validation_NonNegative` - "Price must be non-negative." / "O preço deve ser não negativo." / "El precio debe ser no negativo."
- `Reports_FixedPriceModal_Validation_Required` - "Price is required." / "O preço é obrigatório." / "El precio es obligatorio."
- `Reports_FixedPriceModal_Save` - "Simulate" / "Simular" / "Simular"
- `Reports_FixedPriceModal_Cancel` - "Cancel" / "Cancelar" / "Cancelar"

**Validation:** All three .resx files have matching entries. Designer.cs compiles.

---

### Task 2: Create FixedPriceConfig modal
**New files:**
- `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml`
- `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigView.axaml.cs`
- `src/Valt.UI/Views/Main/Modals/FixedPriceConfig/FixedPriceConfigViewModel.cs`

**Modal specs:**
- Window with `SystemDecorations="None"`, `ExtendClientAreaToDecorationsHint="True"`
- `MinWidth="500"`, `MinHeight="400"`, `MaxWidth="500"`, `MaxHeight="400"`
- CustomTitleBar with title from localization
- Content: description text, price input field (FiatInput or TextBox with validation), OK/Cancel buttons
- ViewModel: inherits `ValtModalViewModel`, has `Price` property (decimal?), validation, Save/Cancel commands
- Response record: `record Response(bool Ok, decimal? Price)`

**Validation rules:**
- Price must be non-negative
- Price must be a valid number
- Empty input means cancel/reset

**Follow existing patterns from:** `SimulatedPricesConfigView` and `SimulatedPricesConfigViewModel`

**Validation:** Modal opens from a test ViewModel, input validation works, OK/Cancel return correct responses.

---

### Task 3: Register modal in application
**Files:**
- `src/Valt.UI/Views/ApplicationModalNames.cs` - add `FixedPriceConfig = 36`
- `src/Valt.UI/Extensions.cs` (or wherever modal factory switch is) - add case for `FixedPriceConfig`
- `src/Valt.UI/Services/ModalFactory.cs` (if different from Extensions.cs)

**Validation:** Modal can be created via `IModalFactory.CreateAsync(ApplicationModalNames.FixedPriceConfig, ownerWindow)`.

---

### Task 4: Add fixed price bar to ReportsView
**File:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsView.axaml`

Add a persistent bar inside the Summary Expander, above the `DashboardGridPanel`. The bar should:
- Show a BTC icon, label "BTC Price Simulation", and current price
- Have a "Simular" button on the right
- Use existing styling/brushes (follow app visual style)
- Be collapsible or always visible (agent's discretion based on UX)

**Layout idea:**
```xml
<Border Classes="card" Margin="0,0,0,12" IsVisible="{Binding !IsSecureModeEnabled}">
    <Grid ColumnDefinitions="Auto,*,Auto">
        <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="8">
            <TextBlock Classes="icon" Text="&#xE3B1;" />
            <TextBlock Text="{x:Static lang:language.Reports_FixedPriceBar_Title}" 
                       VerticalAlignment="Center" />
        </StackPanel>
        <TextBlock Grid.Column="1" 
                   Text="{Binding CurrentBtcPriceFormatted}"
                   VerticalAlignment="Center" 
                   HorizontalAlignment="Center" />
        <Button Grid.Column="2" 
                Content="{x:Static lang:language.Reports_SimulateButton}"
                Command="{Binding OpenFixedPriceConfigCommand}" />
    </Grid>
</Border>
```

**Validation:** Bar renders in UI, shows current price, button is clickable.

---

### Task 5: Add ViewModel support for fixed price bar
**File:** `src/Valt.UI/Views/Main/Tabs/Reports/ReportsViewModel.cs`

Add:
- `[ObservableProperty] private decimal? _customBtcPrice;` (nullable, null means no custom price)
- `[ObservableProperty] private string _currentBtcPriceFormatted = string.Empty;`
- `[RelayCommand] private async Task OpenFixedPriceConfig()` - opens modal, stores result in `_customBtcPrice`
- Method to format current BTC price from `RatesState`
- When modal returns with a price, store it (don't affect calculations yet - that's Phase 5)

**Validation:** Command opens modal, price is stored in ViewModel property, formatting works.

---

## Test Strategy

- Manual UI testing: open Reports tab, see fixed price bar, click "Simular", enter price in modal, click OK
- Verify modal validation rejects negative numbers
- Verify modal cancel returns null/empty price
- Verify localization strings appear in all three languages

## Dependencies

- None (Phase 4 is first phase)

## Estimated Effort

- Task 1 (localization): ~15 min
- Task 2 (modal): ~45 min
- Task 3 (registration): ~10 min
- Task 4 (ReportsView bar): ~30 min
- Task 5 (ViewModel): ~30 min
- **Total: ~2 hours**

## Notes

- The existing `SimulatedPricesConfig` modal (enum value 26) is for configuring multiple price lines. The new `FixedPriceConfig` modal is for a single quick override. They are complementary features.
- The custom price property (`_customBtcPrice`) added in this phase will be consumed by Phase 5's calculation refactoring.
- No service layer changes in this phase - pure UI only.
