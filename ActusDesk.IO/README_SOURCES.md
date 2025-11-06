# PAM Contract Sources and GPU Provider

This directory contains a decoupled architecture for loading PAM contracts from various sources and transferring them to GPU memory.

## Architecture

The design decouples **contract sources** from **contract processing/GPU transfer**:

```
┌─────────────────────────┐
│  IPamContractSource     │  ← Interface for any contract source
└───────────┬─────────────┘
            │
    ┌───────┴────────┬──────────────┬──────────────┐
    │                │              │              │
┌───▼────┐  ┌────────▼─────┐  ┌────▼─────┐  ┌────▼──────┐
│  File  │  │     Mock     │  │ Database │  │  Custom   │
│ Source │  │   Generator  │  │  Source  │  │  Source   │
└────────┘  └──────────────┘  └──────────┘  └───────────┘
            
            All implement IPamContractSource
                        │
                        ▼
            ┌───────────────────┐
            │  PamGpuProvider   │  ← Consumes any source
            └─────────┬─────────┘
                      │
                      ▼
            ┌───────────────────┐
            │  GPU Memory (SoA) │
            └───────────────────┘
```

## Components

### 1. IPamContractSource Interface

Base interface for all contract sources:

```csharp
public interface IPamContractSource
{
    Task<IEnumerable<PamContractModel>> GetContractsAsync(CancellationToken ct = default);
}
```

**Key Benefits:**
- Decouples source from processing
- Enables testing with mock data
- Supports any data source (files, databases, APIs, generators)
- Composable (combine multiple sources)

### 2. PamFileSource

Loads contracts from JSON files (ACTUS test format):

```csharp
// Single file
var source = new PamFileSource("contracts.json");

// Multiple files (loaded in parallel)
var source = new PamFileSource(new[] { "file1.json", "file2.json", "file3.json" });
```

### 3. PamMockSource

Generates synthetic contracts for testing and development:

```csharp
// Generate 1000 random contracts
var source = new PamMockSource(contractCount: 1000);

// Deterministic generation with seed
var source = new PamMockSource(contractCount: 1000, seed: 42);
```

**Features:**
- Generates realistic contract parameters
- Varied currencies (USD, EUR, GBP, CHF, JPY)
- Mixed contract roles (RPA/RPL)
- Different day count conventions
- Deterministic with seed for reproducibility

### 4. PamCompositeSource

Combines multiple sources:

```csharp
var realData = new PamFileSource("production.json");
var stressTest = new PamMockSource(5000);
var composite = new PamCompositeSource(realData, stressTest);

// All sources loaded in parallel
var contracts = await composite.GetContractsAsync();
```

### 5. PamGpuProvider (Refactored)

Now accepts any `IPamContractSource`:

```csharp
public interface IPamGpuProvider
{
    // New: Primary method - accepts any source
    Task<PamDeviceContracts> LoadToGpuAsync(
        IPamContractSource source,
        GpuContext gpuContext,
        CancellationToken ct = default);
    
    // Backward compatible: File paths still work
    Task<PamDeviceContracts> LoadToGpuAsync(string filePath, ...);
    Task<PamDeviceContracts> LoadToGpuAsync(IEnumerable<string> filePaths, ...);
}
```

## Usage Examples

### Example 1: Load from Files

```csharp
using var gpuContext = new GpuContext();
var provider = new PamGpuProvider();

// Create file source
var source = new PamFileSource("actus-tests-pam.json");

// Load to GPU
using var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);
```

### Example 2: Generate Mock Contracts

```csharp
using var gpuContext = new GpuContext();
var provider = new PamGpuProvider();

// Generate 10,000 synthetic contracts for testing
var source = new PamMockSource(10000, seed: 42);

// Load to GPU - same interface!
using var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);
```

### Example 3: Combine Real and Synthetic Data

```csharp
using var gpuContext = new GpuContext();
var provider = new PamGpuProvider();

// Mix real and synthetic data
var realContracts = new PamFileSource("production.json");
var stressScenario = new PamMockSource(5000);
var baseScenario = new PamMockSource(5000, seed: 100);

var composite = new PamCompositeSource(realContracts, stressScenario, baseScenario);

// Load everything to GPU in parallel
using var deviceContracts = await provider.LoadToGpuAsync(composite, gpuContext);
```

### Example 4: Backward Compatibility

```csharp
// Old API still works - no breaking changes
using var gpuContext = new GpuContext();
var provider = new PamGpuProvider();

// Direct file paths (internally uses PamFileSource)
using var deviceContracts = await provider.LoadToGpuAsync("test.json", gpuContext);
```

### Example 5: Custom Source Implementation

Implement `IPamContractSource` for any data source:

```csharp
public class DatabasePamSource : IPamContractSource
{
    private readonly string _connectionString;
    
    public DatabasePamSource(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<IEnumerable<PamContractModel>> GetContractsAsync(CancellationToken ct)
    {
        // Load from database
        using var connection = new SqlConnection(_connectionString);
        // ... query and map to PamContractModel
    }
}

// Use it just like any other source
var source = new DatabasePamSource("Server=...");
using var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);
```

## Testing Benefits

### Unit Testing with Mocks

```csharp
[Fact]
public async Task TestGpuKernel_WithMockData()
{
    using var gpuContext = new GpuContext();
    var provider = new PamGpuProvider();
    
    // Fast test with small mock dataset
    var source = new PamMockSource(100, seed: 42);
    using var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);
    
    // Run your kernel
    // myKernel(..., deviceContracts.NotionalPrincipal, ...);
    
    // Verify results
    // ...
}
```

### Performance Testing at Scale

```csharp
[Fact]
public async Task TestPerformance_10MillionContracts()
{
    using var gpuContext = new GpuContext();
    var provider = new PamGpuProvider();
    
    // Generate massive dataset quickly
    var source = new PamMockSource(10_000_000);
    
    var sw = Stopwatch.StartNew();
    using var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);
    sw.Stop();
    
    // Verify performance metrics
    // ...
}
```

### Deterministic Testing

```csharp
[Fact]
public async Task TestDeterministic_SameSeedSameResults()
{
    using var gpuContext = new GpuContext();
    var provider = new PamGpuProvider();
    
    var source1 = new PamMockSource(1000, seed: 42);
    var source2 = new PamMockSource(1000, seed: 42);
    
    // Both produce identical contracts
    var contracts1 = await source1.GetContractsAsync();
    var contracts2 = await source2.GetContractsAsync();
    
    // Assert identical
}
```

## Performance

- **Parallel Loading**: Multiple sources loaded in parallel via `Task.WhenAll`
- **Parallel File Reading**: Multiple files read in parallel
- **Parallel Array Preparation**: Host arrays filled in parallel
- **Async GPU Transfer**: Non-blocking GPU transfers using streams

## Extension Points

The architecture is designed for extension:

1. **Custom Sources**: Implement `IPamContractSource`
   - Database sources
   - REST API sources
   - Message queue sources
   - Stream sources
   - Cache sources

2. **Source Decorators**: Wrap existing sources
   - Filtering source
   - Transformation source
   - Caching source
   - Logging source

3. **Source Composition**: Combine any sources
   - `PamCompositeSource` for parallel loading
   - Custom composition strategies

## Migration Guide

### Before (Tightly Coupled)

```csharp
// Provider only worked with files
var provider = new PamGpuProvider();
var deviceContracts = await provider.LoadToGpuAsync("file.json", gpuContext);
```

### After (Decoupled)

```csharp
// Provider works with any source
var source = new PamFileSource("file.json");  // Or mock, or database, or...
var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);

// Backward compatible - old code still works
var deviceContracts = await provider.LoadToGpuAsync("file.json", gpuContext);
```

## Summary

This refactoring provides:

✅ **Decoupling**: Source of contracts separated from processing  
✅ **Testability**: Easy to test with mock data  
✅ **Flexibility**: Support any contract source  
✅ **Composability**: Combine multiple sources  
✅ **Backward Compatibility**: Existing code still works  
✅ **Performance**: Parallel loading maintained  
✅ **Extensibility**: Easy to add new source types  

The design follows the **Open/Closed Principle**: open for extension (new sources), closed for modification (provider doesn't change).
