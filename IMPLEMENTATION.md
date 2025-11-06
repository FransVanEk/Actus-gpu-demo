# ActusDesk Implementation Guide

## Project Overview

ActusDesk is a high-performance desktop application for ACTUS contract valuation using GPU acceleration. The solution is architected for maximum reusability, testability, and extensibility.

## Solution Structure

```
ActusDesk.sln
├── ActusDesk.Domain/        # Pure C# ACTUS domain logic
├── ActusDesk.Engine/        # Orchestration and business logic
├── ActusDesk.Gpu/          # ILGPU kernels and GPU management
├── ActusDesk.IO/           # Data access (JSON, binary cache)
├── ActusDesk.UIKit/        # Reusable WPF controls
├── ActusDesk.App/          # WPF application shell
├── ActusDesk.Tests/        # Unit and integration tests
├── data/                   # Input data files
├── cache/                  # Binary cache files (generated)
└── out/                    # Output reports (generated)
```

## Project Dependencies

```
ActusDesk.App
├── ActusDesk.Domain
├── ActusDesk.Engine
│   ├── ActusDesk.Domain
│   └── ActusDesk.Gpu
├── ActusDesk.Gpu
│   └── ActusDesk.Domain
├── ActusDesk.IO
│   └── ActusDesk.Domain
└── ActusDesk.UIKit

ActusDesk.Tests
├── ActusDesk.Domain
├── ActusDesk.Engine
├── ActusDesk.Gpu
└── ActusDesk.IO
```

## Key Technologies

- **.NET 8.0**: Target framework
- **C# 12**: Language version with modern features
- **WPF**: Windows Presentation Foundation for UI
- **ILGPU 1.5.1**: GPU acceleration framework
- **CommunityToolkit.Mvvm 8.3.2**: MVVM helpers
- **xUnit**: Testing framework

## Domain Model

### Contract Types Implemented
- **PAM** (Principal at Maturity): Fixed notional, interest accrues, principal repaid at maturity
- **ANN** (Annuity): Amortizing loan with regular payments
- **LAM** (Linear Amortizer): Principal reduces linearly
- **STK** (Stock): Equity instrument
- **COM** (Commodity): Commodity forward/spot

### Day Count Conventions
- ACT/360: Actual days / 360
- ACT/365F: Actual days / 365 (Fixed)
- 30/360: 30 days per month, 360 days per year
- ACT/ACT ISDA: Actual/Actual (ISDA)

### Business Day Conventions
- Following: Move forward to next business day
- Modified Following: Move forward unless it changes month
- Preceding: Move backward to previous business day

### Calendars
- TARGET (European Central Bank)
- New York (Federal Reserve)
- London (Bank of England)

## GPU Architecture

### Context Management
- Single `GpuContext` instance for entire application lifetime
- Three streams for pipeline overlap:
  - LoadStream: Host-to-Device transfers
  - ComputeStream: Kernel execution
  - StoreStream: Device-to-Host transfers

### Memory Layout
Struct-of-Arrays (SoA) for optimal GPU performance:
```csharp
public sealed record ContractsSoA
{
    public IMemoryOwner<float> Notional;
    public IMemoryOwner<float> Rate;
    public IMemoryOwner<int> StartYYYYMMDD;
    public IMemoryOwner<int> MaturityYYYYMMDD;
    public IMemoryOwner<byte> TypeCode;
    // ... more arrays
}
```

### Kernels Implemented
1. **ValueContractsSimple**: Basic PV calculation
2. **ApplyParallelShock**: Rate curve shocking
3. **AggregateByType**: Results aggregation with atomics

## Testing

All tests use xUnit framework. Current coverage:

### Day Count Tests (4 tests)
- ACT/360 year fraction calculation
- ACT/365F year fraction calculation
- 30/360 year fraction calculation
- Day count factory creation

### Calendar Tests (4 tests)
- Following convention weekend adjustment
- Preceding convention weekend adjustment
- Modified Following month boundary handling
- None convention (no adjustment)

### Rate Provider Tests (4 tests)
- Constant rate provider
- Exact tenor matching in curves
- Linear interpolation between points
- Flat curve creation

### Domain Tests (3 tests)
- PAM event generation (IED and MD)
- PAM interest payment generation
- Deterministic valuation

### Scenario Tests (16 tests)
- Rate shock event application
- Multiple rate shock combination
- Date range event filtering
- Value adjustment events
- Event type filtering
- ScenarioService CRUD operations
- JSON serialization/deserialization
- Scenario file loading

**Total: 119 tests, all passing**

## Running the Application

### Build
```bash
dotnet build ActusDesk.sln --configuration Release
```

### Run
```bash
dotnet run --project ActusDesk.App --configuration Release
```

### Test
```bash
dotnet test ActusDesk.Tests
```

## WPF Application Features

### Main Window
- **Workspace Tab**: Load contracts, manage cache
- **Portfolio Tab**: View contracts, apply filters
- **Scenarios Tab**: Define rate shocks
- **Reporting Tab**: Configure outputs, groupings
- **Run Console Tab**: Execute valuations, view metrics

### Menu System
- **File**: Open/Save workspace, Exit
- **View**: GPU Status, Cache Manager
- **Help**: Documentation, About

### About Dialog
Displays:
- Application version
- GPU device name
- GPU memory size
- GPU type (CUDA/OpenCL/CPU)
- Max threads per block
- Build configuration

## Extension Points

### Adding New Contract Types
1. Create terms class inheriting `IContractTerms`
2. Define state structure
3. Implement `IEventGenerator`
4. Add to contract type enum mapping

### Adding New Day Counts
1. Implement `IDayCount` interface
2. Add to `DayCountFactory`
3. Write unit tests

### Adding New GPU Kernels
1. Define kernel as static method in `ValuationKernels`
2. Load kernel in `GpuContext`
3. Call from `ValuationService`

## Scenario Module

### Overview
The scenario module provides a flexible event-based system for defining market shocks and portfolio adjustments. Scenarios can include multiple events that occur on specific dates or over date ranges.

### Event Types

#### 1. Rate Shock Events
Apply interest rate shocks to contracts:
```json
{
  "eventType": "RateShock",
  "valueBps": 50,
  "shockType": "parallel",
  "startDate": "2024-01-01",
  "endDate": "2024-12-31"
}
```

#### 2. Value Adjustment Events
Apply percentage changes to contract values (e.g., for early abandonment):
```json
{
  "eventType": "ValueAdjustment",
  "percentageChange": -10,
  "startDate": "2024-06-01",
  "endDate": "2024-12-01"
}
```

#### 3. Portfolio Operation Events
Filter or transform portfolios:
```json
{
  "eventType": "PortfolioOperation",
  "operation": "filter",
  "parameters": {
    "currency": "USD"
  }
}
```

### Scenario Definition Format
```json
{
  "name": "StressScenario",
  "description": "Combined stress scenario: rate increase and value decline",
  "events": [
    {
      "eventType": "RateShock",
      "valueBps": 200,
      "shockType": "parallel",
      "startDate": "2024-01-01"
    },
    {
      "eventType": "ValueAdjustment",
      "percentageChange": -15,
      "startDate": "2024-03-01",
      "endDate": "2024-09-01"
    }
  ]
}
```

### Date Range Behavior
- **No dates**: Event is always active
- **StartDate only**: Active from start date onwards
- **EndDate only**: Active until end date
- **Both dates**: Active within range (inclusive)

### Using Scenarios in Code
```csharp
// Create a scenario
var scenario = new PamScenario("StressTest", "Stress scenario");
scenario.AddEvent(new ScenarioEventDefinition
{
    EventType = ScenarioEventType.RateShock,
    ValueBps = 200
});

// Apply to contract events
PamEventApplier.ApplyEvents(events, model, scenario, state);
```

### Managing Scenarios in the UI
The Scenarios tab provides:
- Load/Save scenarios from JSON files
- Add/Remove scenarios
- Create rate shock events
- Create value adjustment events
- View scenario details and event lists

## Performance Considerations

### GPU Optimization
- Contracts uploaded once at startup
- Device buffers reused across runs
- Coalesced memory access via SoA layout
- Atomic operations for aggregation

### Caching Strategy
- Binary SoA cache for fast startup
- Manifest tracks schema version and hashes
- 4K-aligned pages for optimal I/O

## Future Enhancements

### Immediate (Stubs in Place)
- JSON contract loading
- Binary cache implementation
- Additional event generators
- State machines and payoffs

### Medium Term
- Remaining 11 ACTUS contract types
- DV01 bump-and-revalue on device
- Cashflow bucketing
- Parquet export

### Long Term
- Distributed GPU computation
- Real-time market data integration
- Advanced reporting dashboard
- Scenario optimization

## License

MIT License - See LICENSE file for details

## Authors

Built with ActusDesk architecture specification
