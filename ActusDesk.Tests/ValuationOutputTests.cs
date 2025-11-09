using ActusDesk.Engine.Services;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for new valuation features: configurable time span, scenario tracking, and CSV output
/// </summary>
public class ValuationOutputTests : IDisposable
{
    private readonly GpuContext _gpuContext;
    private readonly ITestOutputHelper _output;
    private readonly ILogger<ValuationService> _valuationLogger;
    private readonly ILogger<ContractsService> _contractsLogger;
    private readonly ILogger<ScenarioService> _scenarioLogger;
    private readonly ILogger<CsvValuationOutputHandler> _csvLogger;

    public ValuationOutputTests(ITestOutputHelper output)
    {
        _gpuContext = new GpuContext();
        _output = output;
        
        var valuationLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        _valuationLogger = valuationLoggerFactory.CreateLogger<ValuationService>();
        
        var contractsLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        _contractsLogger = contractsLoggerFactory.CreateLogger<ContractsService>();
        
        var scenarioLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        _scenarioLogger = scenarioLoggerFactory.CreateLogger<ScenarioService>();
        
        var csvLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        _csvLogger = csvLoggerFactory.CreateLogger<CsvValuationOutputHandler>();
    }

    [Fact]
    public async Task RunValuationAsync_With5Years_GeneratesCorrectTimeSpan()
    {
        // Arrange
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(
            _contractsLogger,
            _gpuContext,
            new PamGpuProvider(),
            new AnnGpuProvider(),
            registry);
        
        var scenarioService = new ScenarioService(_scenarioLogger);
        await scenarioService.LoadDefaultScenariosAsync();
        
        var valuationService = new ValuationService(
            _valuationLogger,
            _gpuContext,
            contractsService,
            scenarioService);
        
        await contractsService.LoadMockContractsAsync(10);

        // Act - Run with 5 years instead of default 10
        var result = await valuationService.RunValuationAsync(5);

        // Assert
        Assert.NotNull(result);
        var timeSpan = result.ValuationEndDate - result.ValuationStartDate;
        Assert.InRange(timeSpan.TotalDays, 365 * 5 - 1, 365 * 5 + 1); // Allow for leap years
        
        _output.WriteLine($"Time span: {result.ValuationStartDate:yyyy-MM-dd} to {result.ValuationEndDate:yyyy-MM-dd}");
        _output.WriteLine($"Total days: {timeSpan.TotalDays}");
    }

    [Fact]
    public async Task EventsIncludeScenarioName()
    {
        // Arrange
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(
            _contractsLogger,
            _gpuContext,
            new PamGpuProvider(),
            new AnnGpuProvider(),
            registry);
        
        var scenarioService = new ScenarioService(_scenarioLogger);
        await scenarioService.LoadDefaultScenariosAsync();
        
        var valuationService = new ValuationService(
            _valuationLogger,
            _gpuContext,
            contractsService,
            scenarioService);
        
        await contractsService.LoadMockContractsAsync(5);

        // Act
        var result = await valuationService.RunValuationAsync(10);

        // Assert
        Assert.NotNull(result.DayEventValues);
        Assert.NotEmpty(result.DayEventValues);
        
        var allEvents = result.DayEventValues.SelectMany(d => d.Events).ToList();
        Assert.NotEmpty(allEvents);
        
        // Verify all events have scenario names
        Assert.All(allEvents, evt => Assert.NotEmpty(evt.ScenarioName));
        
        // Verify we have events for all scenarios
        var scenarioNames = allEvents.Select(e => e.ScenarioName).Distinct().ToList();
        Assert.Contains("Base Case", scenarioNames);
        Assert.Contains("Rate +50bps", scenarioNames);
        Assert.Contains("Rate -50bps", scenarioNames);
        
        _output.WriteLine($"Found events for scenarios: {string.Join(", ", scenarioNames)}");
        _output.WriteLine($"Total events: {allEvents.Count}");
    }

    [Fact]
    public async Task CsvOutputHandler_WritesValidCsv()
    {
        // Arrange
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(
            _contractsLogger,
            _gpuContext,
            new PamGpuProvider(),
            new AnnGpuProvider(),
            registry);
        
        var scenarioService = new ScenarioService(_scenarioLogger);
        await scenarioService.LoadDefaultScenariosAsync();
        
        var valuationService = new ValuationService(
            _valuationLogger,
            _gpuContext,
            contractsService,
            scenarioService);
        
        await contractsService.LoadMockContractsAsync(3);
        
        var result = await valuationService.RunValuationAsync(2); // Short period for faster test
        
        var csvHandler = new CsvValuationOutputHandler(_csvLogger);
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_valuation_{Guid.NewGuid()}.csv");

        try
        {
            // Act
            await csvHandler.WriteAsync(result, tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            
            var lines = File.ReadAllLines(tempFile);
            Assert.NotEmpty(lines);
            
            // Verify header
            var header = lines[0];
            Assert.Contains("Scenario", header);
            Assert.Contains("Date", header);
            Assert.Contains("ContractId", header);
            Assert.Contains("ContractType", header);
            Assert.Contains("EventType", header);
            Assert.Contains("Payoff", header);
            Assert.Contains("PresentValue", header);
            Assert.Contains("Currency", header);
            
            // Verify data rows
            Assert.True(lines.Length > 1, "Should have data rows");
            
            // Verify scenario names are in the data
            var csvContent = File.ReadAllText(tempFile);
            Assert.Contains("Base Case", csvContent);
            
            _output.WriteLine($"CSV written with {lines.Length} lines (including header)");
            _output.WriteLine($"First data row: {lines[1]}");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task CsvOutputHandler_IncludesAllScenariosInOutput()
    {
        // Arrange
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(
            _contractsLogger,
            _gpuContext,
            new PamGpuProvider(),
            new AnnGpuProvider(),
            registry);
        
        var scenarioService = new ScenarioService(_scenarioLogger);
        await scenarioService.LoadDefaultScenariosAsync();
        
        var valuationService = new ValuationService(
            _valuationLogger,
            _gpuContext,
            contractsService,
            scenarioService);
        
        await contractsService.LoadMockContractsAsync(2);
        
        var result = await valuationService.RunValuationAsync(1); // 1 year for fast test
        
        var csvHandler = new CsvValuationOutputHandler(_csvLogger);
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_scenarios_{Guid.NewGuid()}.csv");

        try
        {
            // Act
            await csvHandler.WriteAsync(result, tempFile);

            // Assert
            var csvContent = File.ReadAllText(tempFile);
            
            // Verify all scenario names appear in CSV
            Assert.Contains("Base Case", csvContent);
            Assert.Contains("Rate +50bps", csvContent);
            Assert.Contains("Rate -50bps", csvContent);
            
            _output.WriteLine("All scenarios found in CSV output");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
