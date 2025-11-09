using ActusDesk.Engine.Services;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for ValuationService with progress updates and event generation
/// </summary>
public class ValuationServiceTests : IDisposable
{
    private readonly GpuContext _gpuContext;
    private readonly ITestOutputHelper _output;
    private readonly ILogger<ValuationService> _valuationLogger;
    private readonly ILogger<ContractsService> _contractsLogger;
    private readonly ILogger<ScenarioService> _scenarioLogger;

    public ValuationServiceTests(ITestOutputHelper output)
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
    public async Task RunValuationAsync_WithProgress_ReportsProgress()
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
        
        // Load some mock contracts
        await contractsService.LoadMockContractsAsync(50);
        
        // Track progress updates
        var progressUpdates = new List<ValuationProgress>();
        var progress = new Progress<ValuationProgress>(p =>
        {
            progressUpdates.Add(p);
            _output.WriteLine($"Progress: {p.Stage} - {p.Message} ({p.PercentComplete:F1}%)");
        });

        // Act
        var result = await valuationService.RunValuationAsync(10, default, progress);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContractCount > 0, "Should have processed contracts");
        Assert.True(result.ScenarioCount > 0, "Should have processed scenarios");
        Assert.True(progressUpdates.Count > 0, "Should have received progress updates");
        
        // Verify progress goes from 0 to 100
        Assert.Contains(progressUpdates, p => p.PercentComplete >= 0);
        Assert.Contains(progressUpdates, p => p.PercentComplete >= 100);
        
        _output.WriteLine($"Total progress updates: {progressUpdates.Count}");
        _output.WriteLine($"Contracts: {result.ContractCount}, Scenarios: {result.ScenarioCount}");
    }

    [Fact]
    public async Task RunValuationAsync_GeneratesEventsAndValues()
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
        
        // Load some mock contracts
        await contractsService.LoadMockContractsAsync(20);

        // Act
        var result = await valuationService.RunValuationAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DayEventValues);
        Assert.True(result.DayEventValues.Count > 0, "Should have generated events by day");
        
        // Verify events have data
        var firstDay = result.DayEventValues.First();
        Assert.NotNull(firstDay.Events);
        Assert.True(firstDay.Events.Count > 0, "First day should have events");
        
        // Verify event details
        var firstEvent = firstDay.Events.First();
        Assert.NotEmpty(firstEvent.ContractId);
        Assert.NotEmpty(firstEvent.ContractType);
        Assert.NotEmpty(firstEvent.EventType);
        Assert.NotEmpty(firstEvent.Currency);
        
        _output.WriteLine($"Generated {result.DayEventValues.Count} event days");
        _output.WriteLine($"Total events: {result.DayEventValues.Sum(d => d.Events.Count)}");
        _output.WriteLine($"First day: {firstDay.Date}, Events: {firstDay.Events.Count}");
        _output.WriteLine($"Total Payoff: {result.DayEventValues.Sum(d => d.TotalPayoff):N2}");
        _output.WriteLine($"Total PV: {result.DayEventValues.Sum(d => d.TotalPresentValue):N2}");
    }

    [Fact]
    public async Task RunValuationAsync_WithMixedContracts_ProcessesBothTypes()
    {
        // Arrange
        var registry = new ContractRegistry();
        registry.UpdatePercentage("PAM", 60);
        registry.UpdatePercentage("ANN", 40);
        
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
        
        // Load mixed contracts
        await contractsService.LoadMixedMockContractsAsync(100, seed: 42);

        // Act
        var result = await valuationService.RunValuationAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PamContractCount > 0, "Should have PAM contracts");
        Assert.True(result.AnnContractCount > 0, "Should have ANN contracts");
        Assert.True(result.DayEventValues.Count > 0, "Should have generated events");
        
        // Verify both contract types are in events
        var pamEvents = result.DayEventValues.SelectMany(d => d.Events).Count(e => e.ContractType == "PAM");
        var annEvents = result.DayEventValues.SelectMany(d => d.Events).Count(e => e.ContractType == "ANN");
        
        Assert.True(pamEvents > 0, "Should have PAM events");
        Assert.True(annEvents > 0, "Should have ANN events");
        
        _output.WriteLine($"PAM contracts: {result.PamContractCount}, Events: {pamEvents}");
        _output.WriteLine($"ANN contracts: {result.AnnContractCount}, Events: {annEvents}");
    }

    [Fact]
    public async Task RunValuationAsync_WithCancellation_CanBeCancelled()
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
        
        // Load contracts
        await contractsService.LoadMockContractsAsync(100);
        
        var cts = new CancellationTokenSource();
        var progress = new Progress<ValuationProgress>(p =>
        {
            // Cancel after first progress update
            if (p.PercentComplete > 0)
            {
                cts.Cancel();
            }
        });

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await valuationService.RunValuationAsync(10, cts.Token, progress);
        });
        
        _output.WriteLine("Valuation was successfully cancelled");
    }

    [Fact]
    public async Task RunValuationAsync_NoContracts_ReturnsEmptyResults()
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
        
        // Don't load any contracts

        // Act
        var result = await valuationService.RunValuationAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.ContractCount);
        Assert.Empty(result.DayEventValues);
        Assert.Contains("No contracts loaded", result.Message);
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
