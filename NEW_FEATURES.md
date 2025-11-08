# New Valuation Features

## Overview

This update adds four major features requested by the user to make the valuation service more flexible and useful.

## 1. Configurable Valuation Time Span

### Before
- Hard-coded to 10 years in `ValuationService.RunValuationAsync()`
- No way to change without modifying code

### After
```csharp
// Signature now accepts years parameter
public async Task<ValuationResults> RunValuationAsync(
    int valuationYears = 10,
    CancellationToken ct = default,
    IProgress<ValuationProgress>? progress = null)
```

### GUI Control
In the Run Console tab, users can now set:
- **Valuation Period (Years)**: Textbox to specify number of years (default: 10)

### Usage Example
```csharp
// Run for 5 years instead of 10
var result = await valuationService.RunValuationAsync(5);

// From GUI: Just change the number in the textbox before clicking Start Valuation
```

## 2. Scenario Tracking in Events

### Before
- Events had no indication of which scenario they belonged to
- Impossible to distinguish between scenarios in output

### After
```csharp
public class ContractEvent
{
    public string ScenarioName { get; set; } = "";  // NEW!
    public string ContractId { get; set; } = "";
    public string ContractType { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public decimal Payoff { get; set; }
    public decimal PresentValue { get; set; }
    public string Currency { get; set; } = "";
}
```

### Example Event Data
```
ScenarioName: "Base Case"
ContractId: "PAM_0"
ContractType: "PAM"
EventType: "IED"
EventDate: 2025-11-08
Payoff: -500000.00
PresentValue: -500000.00
Currency: "USD"
```

## 3. Output Handler Architecture

### Interface Design
```csharp
public interface IValuationOutputHandler
{
    Task WriteAsync(ValuationResults results, string outputPath, CancellationToken ct = default);
    string FileExtension { get; }
    string Description { get; }
}
```

### CSV Implementation
The `CsvValuationOutputHandler` writes results to CSV format with:

**Header Row:**
```
Scenario,Date,ContractId,ContractType,EventType,Payoff,PresentValue,Currency
```

**Data Rows (Example):**
```
Base Case,2025-11-08,PAM_0,PAM,IED,-500000.00,-500000.00,USD
Base Case,2026-02-08,PAM_0,PAM,IP,6250.00,6098.36,USD
Rate +50bps,2025-11-08,PAM_0,PAM,IED,-500000.00,-500000.00,USD
Rate +50bps,2026-02-08,PAM_0,PAM,IP,6562.50,6390.24,USD
Rate -50bps,2025-11-08,PAM_0,PAM,IED,-500000.00,-500000.00,USD
Rate -50bps,2026-02-08,PAM_0,PAM,IP,5937.50,5811.78,USD
```

### Features
- Properly escaped CSV fields (handles commas, quotes, newlines)
- Events sorted by scenario then contract ID
- All scenarios clearly identified in output
- Easy to import into Excel, Python, R, etc.

## 4. GUI Integration

### Run Console Tab Updates

**New Controls:**
1. **Valuation Settings** section
   - Valuation Period (Years): Textbox (default: 10)
   - Export results to CSV: Checkbox
   - CSV Path: Textbox (only visible when export enabled, default: "valuation_results.csv")

2. **Start Valuation** button now:
   - Uses the configured time period
   - Optionally exports to CSV after completion
   - Shows export status in results window

### Example Output
```
Starting valuation run...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Valuation Complete!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total Contracts: 10,000
  - PAM Contracts: 6,000
  - ANN Contracts: 4,000
Scenarios: 3
Valuation Period: 2025-11-08 to 2030-11-08
Duration: 125.50ms
Throughput: 238,805 contracts/sec
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Exporting to CSV: valuation_results.csv...
Successfully exported results to CSV!

Valuation complete: 10,000 contracts × 3 scenarios in 125.5ms
```

## Testing

### New Tests (4 tests in ValuationOutputTests.cs)
1. `RunValuationAsync_With5Years_GeneratesCorrectTimeSpan` - Verifies configurable time span
2. `EventsIncludeScenarioName` - Verifies scenario tracking in events
3. `CsvOutputHandler_WritesValidCsv` - Verifies CSV output format
4. `CsvOutputHandler_IncludesAllScenariosInOutput` - Verifies all scenarios in CSV

### Test Coverage
- All 139 tests passing
- Configurable time span validated
- Scenario names in all events validated
- CSV format and content validated
- Multi-scenario export validated

## Benefits

1. **Flexibility**: Users can analyze different time horizons without code changes
2. **Clarity**: Scenario names make it clear which results belong to which scenario
3. **Extensibility**: New output formats can be added via `IValuationOutputHandler`
4. **Usability**: GUI controls make features accessible to all users
5. **Integration**: CSV format easy to import into other tools for further analysis

## Future Enhancements

Possible additions based on this architecture:
- JSON output handler
- XML output handler
- Database output handler
- Excel output handler with formatting
- Custom output filters (e.g., only certain scenarios or contract types)
