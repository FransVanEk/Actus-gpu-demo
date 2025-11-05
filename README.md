# ActusDesk - Desktop ACTUS Valuation Application

A high-performance WPF desktop application for ACTUS contract valuation using GPU acceleration via ILGPU.

## Architecture

- **ActusDesk.Domain**: Pure C# ACTUS implementation (contract types, event generation, state machines, payoffs, day-count conventions, calendars)
- **ActusDesk.Engine**: Orchestration layer (scenarios, valuation runs, reporting, batch management)
- **ActusDesk.Gpu**: ILGPU/CUDA kernels for GPU-accelerated valuation (scenario application, contract valuation by family, aggregation)
- **ActusDesk.IO**: Data access layer (JSON contract/scenario loaders, binary SoA cache with manifest)
- **ActusDesk.UIKit**: Reusable WPF controls (MVVM-friendly)
- **ActusDesk.App**: WPF application shell (Views, ViewModels, DI container)
- **ActusDesk.Tests**: Unit and conformance tests

## Requirements

- .NET 8.0 SDK
- C# 12
- Windows (for WPF)
- CUDA-capable GPU for GPU acceleration (tested with RTX 3060 Ti)
- Visual Studio 2022 (optional, for development)

## Building

```bash
dotnet restore ActusDesk.sln
dotnet build ActusDesk.sln --configuration Release
```

## Running

```bash
dotnet run --project ActusDesk.App
```

## Testing

```bash
dotnet test ActusDesk.Tests
```

## Key Features

### Contract Types Supported
- PAM (Principal at Maturity)
- ANN (Annuity)
- LAM (Linear Amortizer)
- COM (Commodity)
- STK (Stock)
- And more (16+ ACTUS types)

### Performance
- Contracts loaded once per session
- GPU upload performed once, device buffers reused across runs
- Binary columnar cache (SoA format) for fast startup
- Batching and stream overlap for maximum GPU utilization
- Target: 1B contracts × 3 scenarios ≤ 10s on RTX 3060 Ti

### Workflow
1. **Workspace**: Load contract portfolios from JSON, build/use binary cache
2. **Portfolio**: View contracts in virtualized grid, apply filters
3. **Scenarios**: Define shock scenarios (rate bumps, filters, remaps)
4. **Reporting**: Configure outputs (PV, DV01, cashflow buckets), groupings, time grids
5. **Run Console**: Execute valuations with per-stage timing, throughput metrics, GPU usage

### Scenarios
Scenarios are computed first on GPU before valuations:
```json
{
  "name": "FlatPlus50",
  "shocks": [{"kind": "parallel_rate_bump_bps", "value": 50}],
  "portfolio_ops": []
}
```

### Reporting
```json
{
  "name": "Standard",
  "outputs": ["PV", "DV01", "CashflowByMonth"],
  "group_by": ["scenario", "type", "attributes.rating"],
  "time_grid": {"start": "2026-01-01", "end": "2036-12-31", "freq": "M"},
  "aggregation": {"sum": ["PV"], "mean": ["DV01"]},
  "export": {"format": "csv", "path": "out/reports/Standard"}
}
```

## Data Layout

```
data/            - Input contract JSON files
  tests/         - Test datasets
cache/           - Binary SoA cache files (contracts_<hash>.bin + manifest.json)
out/             - Export outputs
  reports/       - Generated reports (CSV, Parquet)
```

## Implementation Notes

- **Struct-of-Arrays (SoA)**: Minimizes divergence and maximizes coalesced GPU reads
- **Contract Family Kernels**: Fixed income, swaps, and options use separate kernels to reduce branching
- **DV01 Calculation**: Bump-and-revalue performed in-device for efficiency
- **Persistent Device Buffers**: Contracts uploaded once at startup, reused for all runs
- **FP Modes**: Default FP32; compile with USE_FP16 for half-precision where safe

## License

MIT License - See LICENSE file for details
