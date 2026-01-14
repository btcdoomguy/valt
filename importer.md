# CSV Import Wizard - Implementation Plan

## Overview
Add a CSV import feature with a wizard-style modal that guides users through importing transactions from a constrained CSV format into a fresh Valt database.

## CSV Format Specification

```csv
date,description,amount,account,to_account,to_amount,category
2024-01-15,Salary,5000.00,Checking,,,Income
2024-01-16,Groceries,-150.00,Checking,,,Food
2024-01-17,Transfer to Savings,-1000.00,Checking,Savings,1000.00,Transfer
2024-01-18,Buy BTC,-500.00,Checking,Bitcoin Wallet,50000,Investment
2024-01-19,Sell BTC,-25000,Bitcoin Wallet,Checking,250.00,Investment
```

**Column Rules:**
| Column | Type | Required | Description |
|--------|------|----------|-------------|
| date | YYYY-MM-DD | Yes | Transaction date |
| description | string | Yes | Transaction name |
| amount | decimal/long | Yes | Signed amount (negative = debit, positive = credit). Decimal for fiat, long for BTC (sats) |
| account | string | Yes | Source account name |
| to_account | string | No | Destination account (for transfers/exchanges) |
| to_amount | decimal/long | No | Amount received. Decimal for fiat, long for BTC (sats) |
| category | string | Yes | Category name |

**Transaction Type Inference:**
- `to_account` empty → Single account transaction (FiatDetails or BitcoinDetails)
- `to_account` present, both fiat → FiatToFiatDetails
- `to_account` present, both BTC → BitcoinToBitcoinDetails
- `to_account` present, fiat→BTC → FiatToBitcoinDetails
- `to_account` present, BTC→fiat → BitcoinToFiatDetails

## Sample Template CSV

The "Generate Template" button creates this sample file to help users understand the format:

```csv
date,description,amount,account,to_account,to_amount,category
2024-01-01,Initial Balance,10000.00,Bank Account,,,Opening Balance
2024-01-05,Salary,5000.00,Bank Account,,,Income
2024-01-06,Rent,-1500.00,Bank Account,,,Housing
2024-01-07,Groceries,-250.00,Bank Account,,,Food
2024-01-10,Transfer to Savings,-2000.00,Bank Account,Savings Account,2000.00,Transfer
2024-01-12,Buy Bitcoin,-1000.00,Bank Account,My Bitcoin Wallet,100000,Investment
2024-01-15,Bitcoin from friend,50000,My Bitcoin Wallet,,,Gift
2024-01-18,Consolidate Bitcoin,-30000,My Bitcoin Wallet,Cold Storage,30000,Transfer
2024-01-20,Sell Bitcoin,-20000,My Bitcoin Wallet,Bank Account,200.00,Investment
2024-01-25,Freelance Payment,800.00,Bank Account,,,Income
```

This template demonstrates:
- **Fiat credit**: Salary, Freelance Payment (positive amount, single account)
- **Fiat debit**: Rent, Groceries (negative amount, single account)
- **Fiat-to-Fiat transfer**: Transfer to Savings (negative from, positive to)
- **Fiat-to-Bitcoin**: Buy Bitcoin (fiat negative, sats positive in to_amount)
- **Bitcoin credit**: Bitcoin from friend (positive sats, single BTC account)
- **Bitcoin-to-Bitcoin transfer**: Consolidate Bitcoin (sats transfer)
- **Bitcoin-to-Fiat**: Sell Bitcoin (sats negative, fiat positive in to_amount)

## Wizard Flow

```
┌─────────────────────────────────────────────────────────┐
│  Step 1: File Selection                                 │
│  - "Generate Template" button → saves sample CSV        │
│  - File picker for CSV                                  │
│  - Parse and validate structure                         │
│  - Show errors if invalid format                        │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Step 2: Account Mapping                                │
│  - List all unique account names from CSV               │
│  - User selects: Fiat (+ currency) or Bitcoin           │
│  - Validate: no existing accounts with same name        │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Step 3: Category Preview                               │
│  - Show categories that will be created                 │
│  - Info: user can customize icons later                 │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Step 4: Summary & Confirm                              │
│  - Transaction count                                    │
│  - Date range                                           │
│  - Account summary                                      │
│  - Category count                                       │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  Step 5: Import Progress                                │
│  - Progress bar                                         │
│  - Create accounts → categories → transactions          │
│  - Show completion summary                              │
└─────────────────────────────────────────────────────────┘
```

## Implementation Steps

### 1. Create CSV Parser Service (Valt.Infra)
**File:** `src/Valt.Infra/Services/CsvImport/CsvImportService.cs`

- Parse CSV file with CsvHelper library (add NuGet package)
- Validate required columns exist
- Parse dates (ISO 8601 format)
- Return structured `CsvImportData` with:
  - List of parsed rows
  - Unique account names
  - Unique category names
  - Validation errors
- **Generate template CSV** with sample transactions demonstrating all operation types

**File:** `src/Valt.Infra/Services/CsvImport/CsvImportRow.cs`
- DTO representing a single CSV row

**File:** `src/Valt.Infra/Services/CsvImport/CsvImportValidationResult.cs`
- Validation result with errors per row

**File:** `src/Valt.Infra/Services/CsvImport/CsvTemplateGenerator.cs`
- Static class that generates the sample template CSV content

### 2. Create Import Executor Service (Valt.Infra)
**File:** `src/Valt.Infra/Services/CsvImport/CsvImportExecutor.cs`

- Takes parsed data + account mappings
- **Stops all background jobs before import** via `BackgroundJobManager.StopAll()`
- Creates accounts (FiatAccount/BtcAccount)
- Creates categories
- Creates transactions with correct detail types
- **Restarts background jobs after import** via `BackgroundJobManager.StartAllJobs()` for each job type (App, PriceDatabase, ValtDatabase)
- Reports progress via callback

**Background Job Control:**
```csharp
// Before import
await _backgroundJobManager.StopAll();

// ... perform import ...

// After import (restart all job types)
_backgroundJobManager.StartAllJobs(BackgroundJobTypes.App);
_backgroundJobManager.StartAllJobs(BackgroundJobTypes.PriceDatabase);
_backgroundJobManager.StartAllJobs(BackgroundJobTypes.ValtDatabase);
```

### 3. Create Import Wizard Modal (Valt.UI)

**Files to create:**
- `src/Valt.UI/Views/Main/Modals/CsvImport/CsvImportView.axaml`
- `src/Valt.UI/Views/Main/Modals/CsvImport/CsvImportView.axaml.cs`
- `src/Valt.UI/Views/Main/Modals/CsvImport/CsvImportViewModel.cs`

**ViewModel structure:**
```csharp
public partial class CsvImportViewModel : ValtModalValidatorViewModel
{
    // Step tracking
    [ObservableProperty] private int _currentStep = 1;

    // Step 1: File selection
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private string _fileError;

    // Commands: GenerateTemplateCommand, SelectFileCommand

    // Step 2: Account mapping
    [ObservableProperty] private ObservableCollection<AccountMappingItem> _accountMappings;

    // Step 3: Category preview
    [ObservableProperty] private ObservableCollection<string> _categoriesToCreate;

    // Step 4: Summary
    [ObservableProperty] private int _transactionCount;
    [ObservableProperty] private DateOnly _minDate;
    [ObservableProperty] private DateOnly _maxDate;

    // Step 5: Progress
    [ObservableProperty] private double _importProgress;
    [ObservableProperty] private string _importStatus;
    [ObservableProperty] private bool _isImporting;
}

public class AccountMappingItem
{
    public string AccountName { get; set; }
    public bool IsBitcoin { get; set; }
    public FiatCurrency? SelectedCurrency { get; set; }
}
```

### 4. Register Modal
**File:** `src/Valt.UI/Views/ApplicationModalNames.cs`
- Add `CsvImport` to enum

**File:** `src/Valt.UI/Extensions.cs`
- Register `CsvImportViewModel` as transient
- Add factory case for `CsvImport`

### 5. Add Import Menu Item to MainView
**File:** `src/Valt.UI/Views/Main/MainView.axaml`
- Add "Import CSV" MenuItem in the dropdown menu (above "User Guide" option, line 61)

**File:** `src/Valt.UI/Views/Main/MainViewModel.cs`
- Add `ImportCsvCommand` to open import modal

### 6. Add Localization
**Files:**
- `src/Valt.UI/Lang/language.resx` (en-US)
- `src/Valt.UI/Lang/language.pt-BR.resx`

Add strings for:
- Window title
- Step labels
- Button labels
- Error messages
- Success messages

## Files to Modify

| File | Change |
|------|--------|
| `src/Valt.UI/Views/ApplicationModalNames.cs` | Add `CsvImport` enum value |
| `src/Valt.UI/Extensions.cs` | Register ViewModel and factory |
| `src/Valt.UI/Views/Main/MainView.axaml` | Add "Import CSV" menu item above User Guide |
| `src/Valt.UI/Views/Main/MainViewModel.cs` | Add `ImportCsvCommand` |
| `src/Valt.Infra/Extensions.cs` | Register CsvImportService and CsvImportExecutor |
| `src/Valt.UI/Lang/language.resx` | Add English strings |
| `src/Valt.UI/Lang/language.pt-BR.resx` | Add Portuguese strings |
| `Directory.Packages.props` | Add CsvHelper package |

## Files to Create

| File | Purpose |
|------|---------|
| `src/Valt.Infra/Services/CsvImport/ICsvImportService.cs` | Interface |
| `src/Valt.Infra/Services/CsvImport/CsvImportService.cs` | CSV parsing |
| `src/Valt.Infra/Services/CsvImport/CsvImportRow.cs` | Row DTO |
| `src/Valt.Infra/Services/CsvImport/CsvImportValidationResult.cs` | Validation result |
| `src/Valt.Infra/Services/CsvImport/CsvTemplateGenerator.cs` | Generate sample template |
| `src/Valt.Infra/Services/CsvImport/ICsvImportExecutor.cs` | Interface |
| `src/Valt.Infra/Services/CsvImport/CsvImportExecutor.cs` | Import execution |
| `src/Valt.Infra/Services/CsvImport/AccountMapping.cs` | Account mapping DTO |
| `src/Valt.UI/Views/Main/Modals/CsvImport/CsvImportView.axaml` | View |
| `src/Valt.UI/Views/Main/Modals/CsvImport/CsvImportView.axaml.cs` | Code-behind |
| `src/Valt.UI/Views/Main/Modals/CsvImport/CsvImportViewModel.cs` | ViewModel |

## Validation Rules

### Step 1 - File Validation
- File exists and is readable
- Has required columns: date, description, amount, account, category
- All dates parseable as YYYY-MM-DD
- All amounts parseable as numbers
- No empty required fields

### Step 2 - Account Validation
- All account names mapped to a type
- No account name matches existing account in database (block import)
- All `to_account` values present in account list

### Step 3 - Category Validation
- (None - categories created automatically)

### Step 4 - Pre-Import Validation
- At least one transaction to import

## Testing Strategy

1. **Unit tests for CsvImportService**
   - Valid CSV parsing
   - Missing columns detection
   - Invalid date format detection
   - Invalid amount detection

2. **Unit tests for CsvImportExecutor**
   - Correct transaction type inference
   - Account creation
   - Category creation
   - Transaction creation with correct details

3. **Integration test**
   - Full import flow with sample CSV

## Verification

After implementation:
1. Build: `dotnet build Valt.sln`
2. Run tests: `dotnet test`
3. Manual test:
   - Open fresh database
   - Create sample CSV with various transaction types
   - Run import wizard
   - Verify accounts, categories, and transactions created correctly
   - Verify AutoSatAmount is Pending for eligible transactions
