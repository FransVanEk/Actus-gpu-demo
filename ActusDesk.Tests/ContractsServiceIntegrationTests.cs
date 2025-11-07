using ActusDesk.Engine.Services;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Tests;

/// <summary>
/// Integration tests for ContractsService with PAM GPU provider
/// </summary>
public class ContractsServiceIntegrationTests : IDisposable
{
    private const string TestFilePath = "../../../../data/tests/actus-tests-pam.json";
    private readonly GpuContext _gpuContext;

    public ContractsServiceIntegrationTests()
    {
        _gpuContext = new GpuContext();
    }

    [Fact]
    public async Task ContractsService_LoadFromJson_LoadsToGpu()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<ContractsService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var service = new ContractsService(logger, _gpuContext, pamProvider, annProvider, registry);

        // Act
        await service.LoadFromJsonAsync(new[] { TestFilePath });

        // Assert
        Assert.True(service.ContractCount >= 25);
        var deviceContracts = service.GetPamDeviceContracts();
        Assert.NotNull(deviceContracts);
        Assert.NotNull(deviceContracts.NotionalPrincipal);
    }

    [Fact]
    public async Task ContractsService_LoadMockContracts_GeneratesAndLoads()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<ContractsService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var service = new ContractsService(logger, _gpuContext, pamProvider, annProvider, registry);

        // Act
        await service.LoadMockContractsAsync(1000, seed: 42);

        // Assert
        Assert.Equal(1000, service.PamContractCount);
        var deviceContracts = service.GetPamDeviceContracts();
        Assert.NotNull(deviceContracts);
        Assert.Equal(1000, deviceContracts.Count);
    }

    [Fact]
    public async Task ContractsService_LoadFromSource_WithComposite()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<ContractsService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var service = new ContractsService(logger, _gpuContext, pamProvider, annProvider, registry);

        // Create composite source
        var fileSource = new PamFileSource(TestFilePath);
        var mockSource = new PamMockSource(100);
        var compositeSource = new PamCompositeSource(fileSource, mockSource);

        // Act
        await service.LoadFromSourceAsync(compositeSource);

        // Assert
        Assert.True(service.ContractCount >= 125); // ~25 from file + 100 mock
    }

    [Fact]
    public async Task ContractsService_MultipleLoads_DisposesOldContracts()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<ContractsService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var service = new ContractsService(logger, _gpuContext, pamProvider, annProvider, registry);

        // Act - Load multiple times
        await service.LoadMockContractsAsync(100);
        var firstCount = service.ContractCount;
        
        await service.LoadMockContractsAsync(200);
        var secondCount = service.ContractCount;

        // Assert - Second load replaces first
        Assert.Equal(100, firstCount);
        Assert.Equal(200, secondCount);
    }

    [Fact]
    public async Task ContractsService_Dispose_CleansUpResources()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<ContractsService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        var service = new ContractsService(logger, _gpuContext, pamProvider, annProvider, registry);

        await service.LoadMockContractsAsync(100);

        // Act
        service.Dispose();

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task ContractsService_LoadMixedContracts_LoadsBothTypes()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        var logger = loggerFactory.CreateLogger<ContractsService>();
        var pamProvider = new PamGpuProvider();
        var annProvider = new AnnGpuProvider();
        var registry = new ContractRegistry();
        registry.UpdatePercentage("PAM", 62.5); // 500/800
        registry.UpdatePercentage("ANN", 37.5); // 300/800
        var service = new ContractsService(logger, _gpuContext, pamProvider, annProvider, registry);

        // Act
        await service.LoadMixedMockContractsAsync(totalContracts: 800, seed: 42);

        // Assert
        Assert.Equal(800, service.ContractCount);
        Assert.Equal(500, service.PamContractCount);
        Assert.Equal(300, service.AnnContractCount);
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
