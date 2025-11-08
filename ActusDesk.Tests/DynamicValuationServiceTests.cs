using ActusDesk.Engine.Services;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActusDesk.Tests;

/// <summary>
/// Tests demonstrating the dynamic, extensible nature of the refactored ValuationService
/// Shows SOLID principles in action
/// </summary>
public class DynamicValuationServiceTests : IDisposable
{
    private readonly GpuContext _gpuContext;
    private readonly ITestOutputHelper _output;
    private readonly ILogger<ValuationService> _valuationLogger;
    private readonly ILogger<ContractsService> _contractsLogger;
    private readonly ILogger<ScenarioService> _scenarioLogger;

    public DynamicValuationServiceTests(ITestOutputHelper output)
    {
        _gpuContext = new GpuContext();
        _output = output;
        
        // Create test loggers
        var valuationLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        _valuationLogger = valuationLoggerFactory.CreateLogger<ValuationService>();
        
        var contractsLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        _contractsLogger = contractsLoggerFactory.CreateLogger<ContractsService>();
        
        var scenarioLoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        _scenarioLogger = scenarioLoggerFactory.CreateLogger<ScenarioService>();
    }

    [Fact]
    public async Task RunValuationAsync_WithoutScenarios_UsesDefaultBaseCase()
    {
        // Arrange
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(
            _contractsLogger,
            _gpuContext,
            new PamGpuProvider(),
            new AnnGpuProvider(),
            registry);
        
        // Explicitly do NOT load scenarios
        var scenarioService = new ScenarioService(_scenarioLogger);
        
        var valuationService = new ValuationService(
            _valuationLogger,
            _gpuContext,
            contractsService,
            scenarioService);
        
        // Load contracts
        await contractsService.LoadMockContractsAsync(20);

        // Act - Run without scenarios
        var result = await valuationService.RunValuationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ScenarioCount); // Should have default base case
        Assert.True(result.ContractCount > 0, "Should have processed contracts");
        Assert.True(result.DayEventValues.Count > 0, "Should have generated events");
        Assert.Contains("1 scenario", result.Message); // Check for single scenario
        
        _output.WriteLine($"Ran valuation without explicit scenarios: {result.ScenarioCount} scenario(s)");
        _output.WriteLine($"Contracts: {result.ContractCount}, Event days: {result.DayEventValues.Count}");
        _output.WriteLine($"Message: {result.Message}");
    }

    [Fact]
    public async Task RunValuationAsync_WithOnlyPamContracts_ProcessesSuccessfully()
    {
        // Arrange - demonstrates that PAM-only processing works
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
        
        // Load only PAM contracts
        await contractsService.LoadMockContractsAsync(30);

        // Act
        var result = await valuationService.RunValuationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PamContractCount > 0, "Should have PAM contracts");
        Assert.Equal(0, result.AnnContractCount); // No ANN contracts
        Assert.True(result.DayEventValues.Count > 0, "Should have generated events");
        
        // Verify all events are PAM type
        var allEvents = result.DayEventValues.SelectMany(d => d.Events).ToList();
        Assert.All(allEvents, evt => Assert.Equal("PAM", evt.ContractType));
        
        _output.WriteLine($"Processed PAM-only contracts: {result.PamContractCount}");
        _output.WriteLine($"Total events: {allEvents.Count}");
    }

    [Fact]
    public async Task RunValuationAsync_WithOnlyAnnContracts_ProcessesSuccessfully()
    {
        // Arrange - demonstrates that ANN-only processing works
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
        
        // Load only ANN contracts
        await contractsService.LoadMockAnnContractsAsync(25);

        // Act
        var result = await valuationService.RunValuationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.PamContractCount); // No PAM contracts
        Assert.True(result.AnnContractCount > 0, "Should have ANN contracts");
        Assert.True(result.DayEventValues.Count > 0, "Should have generated events");
        
        // Verify all events are ANN type
        var allEvents = result.DayEventValues.SelectMany(d => d.Events).ToList();
        Assert.All(allEvents, evt => Assert.Equal("ANN", evt.ContractType));
        
        _output.WriteLine($"Processed ANN-only contracts: {result.AnnContractCount}");
        _output.WriteLine($"Total events: {allEvents.Count}");
    }

    [Fact]
    public async Task RunValuationAsync_WithCustomContractMix_ProcessesFlexibly()
    {
        // Arrange - demonstrates flexible contract mixing
        var registry = new ContractRegistry();
        registry.UpdatePercentage("PAM", 70);
        registry.UpdatePercentage("ANN", 30);
        
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
        
        // Load mixed contracts with custom ratio
        await contractsService.LoadMixedMockContractsAsync(100, seed: 42);

        // Act
        var result = await valuationService.RunValuationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PamContractCount > 0, "Should have PAM contracts");
        Assert.True(result.AnnContractCount > 0, "Should have ANN contracts");
        Assert.Equal(result.PamContractCount + result.AnnContractCount, result.ContractCount);
        
        // Verify ContractCountsByType includes both types
        Assert.True(result.ContractCountsByType.ContainsKey("PAM"));
        Assert.True(result.ContractCountsByType.ContainsKey("ANN"));
        Assert.Equal(result.PamContractCount, result.ContractCountsByType["PAM"]);
        Assert.Equal(result.AnnContractCount, result.ContractCountsByType["ANN"]);
        
        // Verify events exist for both types
        var allEvents = result.DayEventValues.SelectMany(d => d.Events).ToList();
        Assert.Contains(allEvents, evt => evt.ContractType == "PAM");
        Assert.Contains(allEvents, evt => evt.ContractType == "ANN");
        
        _output.WriteLine($"Custom mix - PAM: {result.PamContractCount}, ANN: {result.AnnContractCount}");
        _output.WriteLine($"Total events: {allEvents.Count}");
    }

    [Fact]
    public async Task RunValuationAsync_WithSingleScenario_ProcessesCorrectly()
    {
        // Arrange - demonstrates single scenario processing
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(
            _contractsLogger,
            _gpuContext,
            new PamGpuProvider(),
            new AnnGpuProvider(),
            registry);
        
        var scenarioService = new ScenarioService(_scenarioLogger);
        // Don't load scenarios - will get default base case
        
        var valuationService = new ValuationService(
            _valuationLogger,
            _gpuContext,
            contractsService,
            scenarioService);
        
        await contractsService.LoadMockContractsAsync(15);

        // Act
        var result = await valuationService.RunValuationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ScenarioCount); // Single default scenario
        Assert.True(result.ContractCount > 0);
        Assert.True(result.DayEventValues.Count > 0);
        
        _output.WriteLine($"Single scenario valuation completed");
        _output.WriteLine($"Duration: {result.Duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task RunValuationAsync_ResultsIncludeContractCountsByType()
    {
        // Arrange - demonstrates dynamic result gathering
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
        
        await contractsService.LoadMixedMockContractsAsync(50);

        // Act
        var result = await valuationService.RunValuationAsync();

        // Assert - verify dynamic result structure
        Assert.NotNull(result.ContractCountsByType);
        Assert.NotEmpty(result.ContractCountsByType);
        
        // Should have entries for loaded contract types
        Assert.Contains("PAM", result.ContractCountsByType.Keys);
        Assert.Contains("ANN", result.ContractCountsByType.Keys);
        
        // Counts should match
        int totalFromDict = result.ContractCountsByType.Values.Sum();
        Assert.Equal(result.ContractCount, totalFromDict);
        
        _output.WriteLine("Contract counts by type:");
        foreach (var kvp in result.ContractCountsByType)
        {
            _output.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }

    [Fact]
    public async Task RunValuationAsync_EventsIncludeDateInformation()
    {
        // Arrange - verify event data completeness
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

        // Act
        var result = await valuationService.RunValuationAsync();

        // Assert - verify event structure
        Assert.NotNull(result.DayEventValues);
        Assert.NotEmpty(result.DayEventValues);
        
        var firstDay = result.DayEventValues.First();
        Assert.NotNull(firstDay.Events);
        Assert.NotEmpty(firstDay.Events);
        
        var firstEvent = firstDay.Events.First();
        Assert.NotEqual(default(DateOnly), firstEvent.EventDate);
        Assert.NotEmpty(firstEvent.ContractId);
        Assert.NotEmpty(firstEvent.ContractType);
        Assert.NotEmpty(firstEvent.EventType);
        Assert.NotEmpty(firstEvent.Currency);
        
        _output.WriteLine($"First event: {firstEvent.ContractType}/{firstEvent.EventType} on {firstEvent.EventDate}");
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
