# Quick Start Guide

## Prerequisites

- **.NET 8.0 SDK** or later
- **Windows** (for WPF support)
- **CUDA-capable GPU** (optional, will fallback to CPU)
- **Visual Studio 2022** (recommended) or VS Code

## Installation

### 1. Clone the Repository
```bash
git clone https://github.com/FransVanEk/Actus-gpu-demo.git
cd Actus-gpu-demo
```

### 2. Restore Dependencies
```bash
dotnet restore ActusDesk.sln
```

### 3. Build the Solution
```bash
dotnet build ActusDesk.sln --configuration Release
```

### 4. Run Tests (Optional)
```bash
dotnet test ActusDesk.Tests
```

## Running the Application

### From Command Line
```bash
dotnet run --project ActusDesk.App
```

### From Visual Studio
1. Open `ActusDesk.sln` in Visual Studio 2022
2. Set `ActusDesk.App` as startup project
3. Press F5 to run

## First Steps

### 1. Check GPU Status
- Open the application
- Go to **Help → About ActusDesk**
- Verify GPU information is displayed
- Close the About dialog

### 2. Load Sample Contracts
- Navigate to the **Workspace** tab
- The default path should show: `data/tests/sample_contracts.json`
- Click **Load Contracts** (functionality to be implemented)

### 3. View Sample Data
Sample data files are provided in `data/tests/`:
- `sample_contracts.json` - 3 PAM contracts
- `scenarios.json` - 3 scenarios (Base, +50bps, -100bps)
- `reporting.json` - Standard reporting configuration

## Project Structure Quick Reference

```
ActusDesk.sln
├── ActusDesk.Domain/     → Core ACTUS logic (contracts, day counts, calendars)
├── ActusDesk.Engine/     → Orchestration (scenarios, runs, reporting)
├── ActusDesk.Gpu/        → GPU kernels and device management
├── ActusDesk.IO/         → Data loading and caching
├── ActusDesk.UIKit/      → Reusable WPF controls
├── ActusDesk.App/        → Main WPF application
├── ActusDesk.Tests/      → Unit and integration tests
└── data/                 → Input data files
```

## Implemented Features

✅ **Core Domain**
- 5 contract types (PAM, ANN, LAM, STK, COM)
- 4 day count conventions
- 3 business day conventions with calendar support
- Rate curve provider with interpolation

✅ **GPU Infrastructure**
- CUDA/OpenCL/CPU accelerator support via ILGPU
- Three-stream design for overlap
- SoA memory layout for optimal performance
- Basic valuation kernels

✅ **WPF Application**
- Professional 5-tab UI (Workspace/Portfolio/Scenarios/Reporting/Run Console)
- Menu system (File/View/Help)
- About dialog with GPU information
- MVVM architecture with DI

✅ **Testing**
- 15 unit tests (all passing)
- Day count, calendar, rate provider, and domain tests

## What's Stubbed

The following features have interfaces/stubs but need implementation:
- JSON contract loading (interface exists)
- Binary cache I/O (interface exists)
- ViewModels data binding (ViewModels exist)
- Service orchestration logic (services registered)

## Troubleshooting

### GPU Not Detected
**Problem**: About dialog shows "GPU not initialized" or "Unable to query GPU"

**Solutions**:
- Verify CUDA drivers are installed
- Application will fallback to CPU accelerator automatically
- Check logs in console for detailed error messages

### Build Errors
**Problem**: Build fails with missing dependencies

**Solutions**:
```bash
dotnet clean ActusDesk.sln
dotnet restore ActusDesk.sln
dotnet build ActusDesk.sln
```

### Test Failures
**Problem**: Tests fail unexpectedly

**Solutions**:
```bash
dotnet test ActusDesk.Tests --logger "console;verbosity=detailed"
```

## Development Workflow

### Adding a New Contract Type
1. Create terms class in `ActusDesk.Domain/Contracts/`
2. Add enum value in `IContractTerms.cs`
3. Implement `IEventGenerator` in `ActusDesk.Domain/EventGeneration/`
4. Write tests in `ActusDesk.Tests/`

### Adding a New Test
1. Create test class in `ActusDesk.Tests/`
2. Use xUnit attributes (`[Fact]`, `[Theory]`)
3. Follow AAA pattern (Arrange/Act/Assert)
4. Run: `dotnet test`

### Debugging
- Use Visual Studio debugger (F5)
- Check console output for logs
- Enable verbose logging in `App.xaml.cs` if needed

## Next Steps

After getting familiar with the application:

1. **Explore the Code**
   - Start with `ActusDesk.Domain/` for core logic
   - Review tests in `ActusDesk.Tests/` for usage examples
   - Check `ActusDesk.App/` for UI implementation

2. **Run Tests**
   - All 15 tests should pass
   - Review test output for coverage

3. **Extend Functionality**
   - Implement JSON loaders in `ActusDesk.IO/`
   - Add more contract types following PAM pattern
   - Connect ViewModels to Services

## Resources

- **README.md**: Architecture overview
- **IMPLEMENTATION.md**: Detailed technical guide
- **Sample Data**: `data/tests/` directory
- **Tests**: `ActusDesk.Tests/` for usage examples

## Support

For issues or questions:
1. Check IMPLEMENTATION.md for detailed architecture
2. Review existing tests for usage patterns
3. Check console logs for error details

## License

MIT License - See LICENSE file
