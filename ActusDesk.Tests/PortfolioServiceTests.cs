using ActusDesk.Engine.Services;
using ActusDesk.Engine.Models;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for PortfolioService statistics computation
/// </summary>
public class PortfolioServiceTests : IDisposable
{
    private readonly GpuContext _gpuContext;

    public PortfolioServiceTests()
    {
        _gpuContext = new GpuContext();
    }

    [Fact]
    public void PortfolioService_EmptyContracts_ReturnsEmptyStatistics()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var contractsLogger = loggerFactory.CreateLogger<ContractsService>();
        var portfolioLogger = loggerFactory.CreateLogger<PortfolioService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(contractsLogger, _gpuContext, pamProvider, annProvider, registry);
        var portfolioService = new PortfolioService(portfolioLogger, contractsService);

        // Act
        var statistics = portfolioService.ComputeStatistics();

        // Assert
        Assert.NotNull(statistics);
        Assert.Equal(0, statistics.Summary.TotalContracts);
        Assert.Empty(statistics.TypeStatistics);
        Assert.Equal(0, statistics.Readiness.ReadyCount);
    }

    [Fact]
    public async Task PortfolioService_WithPamContracts_ComputesCorrectStatistics()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var contractsLogger = loggerFactory.CreateLogger<ContractsService>();
        var portfolioLogger = loggerFactory.CreateLogger<PortfolioService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(contractsLogger, _gpuContext, pamProvider, annProvider, registry);
        var portfolioService = new PortfolioService(portfolioLogger, contractsService);

        // Load mock PAM contracts
        await contractsService.LoadMockContractsAsync(100, seed: 42);

        // Act
        var statistics = portfolioService.ComputeStatistics();

        // Assert
        Assert.NotNull(statistics);
        Assert.Equal(100, statistics.Summary.TotalContracts);
        Assert.True(statistics.TypeStatistics.ContainsKey("PAM"));
        
        var pamStats = statistics.TypeStatistics["PAM"];
        Assert.Equal("PAM", pamStats.ContractType);
        Assert.Equal(100, pamStats.Count);
        Assert.Equal(100.0, pamStats.PortfolioPercentage);
        
        // Economic summary
        Assert.True(pamStats.Economics.TotalNotional > 0);
        Assert.True(pamStats.Economics.AverageNotional > 0);
        Assert.True(pamStats.Economics.MinNotional > 0);
        Assert.True(pamStats.Economics.MaxNotional > 0);
        Assert.True(pamStats.Economics.WeightedAverageRate.HasValue);
        Assert.True(pamStats.Economics.WeightedAverageRate.Value > 0);
        
        // Readiness
        Assert.True(pamStats.Readiness.ReadyCount > 0);
        Assert.True(pamStats.Readiness.ReadyPercentage > 0);
    }

    [Fact]
    public async Task PortfolioService_WithMixedContracts_ComputesBothTypes()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var contractsLogger = loggerFactory.CreateLogger<ContractsService>();
        var portfolioLogger = loggerFactory.CreateLogger<PortfolioService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(contractsLogger, _gpuContext, pamProvider, annProvider, registry);
        var portfolioService = new PortfolioService(portfolioLogger, contractsService);

        // Set registry percentages: 60% PAM, 40% ANN
        registry.UpdatePercentage("PAM", 60.0);
        registry.UpdatePercentage("ANN", 40.0);

        // Load mixed contracts
        await contractsService.LoadMixedMockContractsAsync(1000, seed: 42);

        // Act
        var statistics = portfolioService.ComputeStatistics();

        // Assert
        Assert.NotNull(statistics);
        Assert.Equal(1000, statistics.Summary.TotalContracts);
        Assert.Equal(2, statistics.TypeStatistics.Count);
        
        // PAM statistics
        Assert.True(statistics.TypeStatistics.ContainsKey("PAM"));
        var pamStats = statistics.TypeStatistics["PAM"];
        Assert.Equal(600, pamStats.Count);
        Assert.Equal(60.0, pamStats.PortfolioPercentage);
        
        // ANN statistics
        Assert.True(statistics.TypeStatistics.ContainsKey("ANN"));
        var annStats = statistics.TypeStatistics["ANN"];
        Assert.Equal(400, annStats.Count);
        Assert.Equal(40.0, annStats.PortfolioPercentage);
        
        // Overall readiness
        Assert.Equal(1000, statistics.Readiness.ReadyCount + 
                           statistics.Readiness.IncompleteCount + 
                           statistics.Readiness.UnsupportedCount);
    }

    [Fact]
    public async Task PortfolioService_ComputesTopCurrencies()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var contractsLogger = loggerFactory.CreateLogger<ContractsService>();
        var portfolioLogger = loggerFactory.CreateLogger<PortfolioService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(contractsLogger, _gpuContext, pamProvider, annProvider, registry);
        var portfolioService = new PortfolioService(portfolioLogger, contractsService);

        // Load contracts
        await contractsService.LoadMockContractsAsync(500, seed: 42);

        // Act
        var statistics = portfolioService.ComputeStatistics();

        // Assert
        Assert.NotNull(statistics.Summary.TopCurrencies);
        Assert.True(statistics.Summary.TopCurrencies.Count > 0);
        Assert.True(statistics.Summary.TopCurrencies.Count <= 3);
        
        // Verify currency info has all required fields
        foreach (var currency in statistics.Summary.TopCurrencies)
        {
            Assert.False(string.IsNullOrEmpty(currency.Currency));
            Assert.True(currency.Count > 0);
            Assert.True(currency.Percentage > 0);
        }
    }

    [Fact]
    public async Task PortfolioService_ComputesNotionalStatistics()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var contractsLogger = loggerFactory.CreateLogger<ContractsService>();
        var portfolioLogger = loggerFactory.CreateLogger<PortfolioService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(contractsLogger, _gpuContext, pamProvider, annProvider, registry);
        var portfolioService = new PortfolioService(portfolioLogger, contractsService);

        // Load contracts
        await contractsService.LoadMockContractsAsync(100, seed: 42);

        // Act
        var statistics = portfolioService.ComputeStatistics();

        // Assert
        Assert.True(statistics.Summary.TotalNotional > 0);
        Assert.True(statistics.Summary.MinNotional > 0);
        Assert.True(statistics.Summary.MaxNotional > 0);
        Assert.True(statistics.Summary.AverageNotional > 0);
        Assert.True(statistics.Summary.MedianNotional.HasValue);
        
        // Verify relationships
        Assert.True(statistics.Summary.MinNotional <= statistics.Summary.AverageNotional);
        Assert.True(statistics.Summary.AverageNotional <= statistics.Summary.MaxNotional);
        Assert.True(statistics.Summary.MinNotional <= statistics.Summary.MedianNotional);
        Assert.True(statistics.Summary.MedianNotional <= statistics.Summary.MaxNotional);
        
        // Total should equal sum of averages times count
        var expectedTotal = statistics.Summary.AverageNotional * statistics.Summary.TotalContracts;
        Assert.True(Math.Abs(statistics.Summary.TotalNotional - expectedTotal) < 0.01m);
    }

    [Fact]
    public async Task PortfolioService_ComputesMaturityRange()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var contractsLogger = loggerFactory.CreateLogger<ContractsService>();
        var portfolioLogger = loggerFactory.CreateLogger<PortfolioService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(contractsLogger, _gpuContext, pamProvider, annProvider, registry);
        var portfolioService = new PortfolioService(portfolioLogger, contractsService);

        // Load contracts
        await contractsService.LoadMockContractsAsync(100, seed: 42);

        // Act
        var statistics = portfolioService.ComputeStatistics();

        // Assert
        Assert.True(statistics.TypeStatistics.ContainsKey("PAM"));
        var pamStats = statistics.TypeStatistics["PAM"];
        
        Assert.True(pamStats.Economics.MinMaturityDate.HasValue);
        Assert.True(pamStats.Economics.MaxMaturityDate.HasValue);
        Assert.True(pamStats.Economics.AverageMaturityDate.HasValue);
        
        // Verify date ordering
        Assert.True(pamStats.Economics.MinMaturityDate <= pamStats.Economics.AverageMaturityDate);
        Assert.True(pamStats.Economics.AverageMaturityDate <= pamStats.Economics.MaxMaturityDate);
    }

    [Fact]
    public async Task PortfolioService_TracksDataQuality()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var contractsLogger = loggerFactory.CreateLogger<ContractsService>();
        var portfolioLogger = loggerFactory.CreateLogger<PortfolioService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var contractsService = new ContractsService(contractsLogger, _gpuContext, pamProvider, annProvider, registry);
        var portfolioService = new PortfolioService(portfolioLogger, contractsService);

        // Load contracts
        await contractsService.LoadMockContractsAsync(100, seed: 42);

        // Act
        var statistics = portfolioService.ComputeStatistics();

        // Assert
        Assert.True(statistics.TypeStatistics.ContainsKey("PAM"));
        var pamStats = statistics.TypeStatistics["PAM"];
        
        // Data quality metrics should be tracked (even if zero for mock data)
        Assert.True(pamStats.Quality.MissingMaturityCount >= 0);
        Assert.True(pamStats.Quality.MissingNotionalCount >= 0);
        Assert.True(pamStats.Quality.MissingRateCount >= 0);
        Assert.True(pamStats.Quality.MissingCurrencyCount >= 0);
        Assert.True(pamStats.Quality.MissingStartDateCount >= 0);
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
