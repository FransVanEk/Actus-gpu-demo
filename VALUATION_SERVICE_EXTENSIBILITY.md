# ValuationService Extensibility Guide

## Overview

The ValuationService has been refactored to follow SOLID principles, making it easy to add new contract types without modifying existing code.

## Architecture

### Key Components

1. **IContractProcessor** - Interface defining contract processing behavior
2. **BaseContractProcessor** - Abstract base class with common processing logic
3. **Contract-specific Processors** - Concrete implementations (e.g., PamContractProcessor, AnnContractProcessor)
4. **ContractProcessorRegistry** - Manages available processors dynamically
5. **ValuationService** - Orchestrates valuation using registered processors

## Adding a New Contract Type

To add a new contract type (e.g., "NAM" - Negative Amortization):

### Step 1: Create the Processor

```csharp
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Engine.Services;

public class NamContractProcessor : BaseContractProcessor
{
    private readonly NamDeviceContracts? _contracts;

    public NamContractProcessor(NamDeviceContracts? contracts, ILogger<NamContractProcessor> logger)
        : base(logger)
    {
        _contracts = contracts;
    }

    public override string ContractType => "NAM";

    public override int GetContractCount() => _contracts?.Count ?? 0;

    public override async Task<IEnumerable<ContractEvent>> ProcessAsync(
        GpuContext gpuContext,
        ValuationScenario scenario,
        DateTime valuationStart,
        DateTime valuationEnd,
        IProgress<ValuationProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (_contracts == null || _contracts.Count == 0)
        {
            return Array.Empty<ContractEvent>();
        }

        double rateAdjustment = scenario.RateBumpBps / 10000.0;

        return await GenerateEventsWithProgressAsync(
            _contracts.Count,
            ContractType,
            valuationStart,
            valuationEnd,
            rateAdjustment,
            GenerateNamEvents,
            progress,
            ct);
    }

    private ContractEvent[] GenerateNamEvents(
        string contractId,
        DateTime valuationStart,
        DateTime valuationEnd,
        double rateAdjustment)
    {
        // Your NAM-specific event generation logic here
        var events = new List<ContractEvent>();
        // ... generate NAM events ...
        return events.ToArray();
    }
}
```

### Step 2: Register the Processor

Update the `InitializeProcessors()` method in `ValuationService`:

```csharp
private void InitializeProcessors()
{
    _processorRegistry.Clear();

    // Register PAM processor if contracts are available
    var pamContracts = _contractsService.GetPamDeviceContracts();
    if (pamContracts != null)
    {
        var pamLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<PamContractProcessor>.Instance;
        _processorRegistry.Register(new PamContractProcessor(pamContracts, pamLogger));
    }

    // Register ANN processor if contracts are available
    var annContracts = _contractsService.GetAnnDeviceContracts();
    if (annContracts != null)
    {
        var annLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AnnContractProcessor>.Instance;
        _processorRegistry.Register(new AnnContractProcessor(annContracts, annLogger));
    }

    // NEW: Register NAM processor if contracts are available
    var namContracts = _contractsService.GetNamDeviceContracts();
    if (namContracts != null)
    {
        var namLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<NamContractProcessor>.Instance;
        _processorRegistry.Register(new NamContractProcessor(namContracts, namLogger));
    }
}
```

### Step 3: Add Contract Registry Support

Update the `ContractRegistry` constructor to include the new type:

```csharp
public ContractRegistry()
{
    // Register default contract types
    RegisterContractType("PAM", "Principal at Maturity", 40.0);
    RegisterContractType("ANN", "Annuity", 40.0);
    RegisterContractType("NAM", "Negative Amortization", 20.0); // NEW
}
```

## SOLID Principles in Action

### Single Responsibility Principle (SRP)
- Each processor class handles one contract type only
- ValuationService only orchestrates, doesn't process contracts
- ContractProcessorRegistry only manages processors

### Open/Closed Principle (OCP)
- Add new contract types by creating new processors
- No need to modify ValuationService when adding types
- Extensible through interfaces, not modification

### Liskov Substitution Principle (LSP)
- All IContractProcessor implementations are interchangeable
- ValuationService treats all processors the same way

### Interface Segregation Principle (ISP)
- IContractProcessor has only what's needed
- No forced implementation of unused methods

### Dependency Inversion Principle (DIP)
- ValuationService depends on IContractProcessor abstraction
- Not dependent on concrete processor implementations

## Running Without Scenarios

The service now supports running without explicitly loaded scenarios:

```csharp
var scenarioService = new ScenarioService(logger);
// Don't load scenarios - will use default base case

var valuationService = new ValuationService(logger, gpuContext, contractsService, scenarioService);
var result = await valuationService.RunValuationAsync(); // Uses default base case
```

## Dynamic Result Gathering

Results now include contract counts by type dynamically:

```csharp
var result = await valuationService.RunValuationAsync();

// Access counts by type
int pamCount = result.ContractCountsByType["PAM"];
int annCount = result.ContractCountsByType["ANN"];

// Or use legacy properties
int pamCount2 = result.PamContractCount;
int annCount2 = result.AnnContractCount;
```

## Benefits

1. **Extensibility** - Add new contract types without modifying core logic
2. **Maintainability** - Clear separation of concerns
3. **Testability** - Each processor can be tested in isolation
4. **Flexibility** - Support any mix of contract types and scenarios
5. **Clean Code** - No duplication, follows best practices
