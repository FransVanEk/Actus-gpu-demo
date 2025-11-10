using ActusDesk.Gpu;
using ActusDesk.IO;

namespace ActusDesk.Tests;

/// <summary>
/// Examples demonstrating new decoupled PAM GPU provider usage
/// </summary>
public class PamGpuProviderDecoupledExamples
{
    private const string TestFilePath = "../../../../data/tests/sample_contracts.json";

    [Fact]
    public async Task Example_LoadFromFileSource()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        var provider = new PamGpuProvider();
        
        // Create file source
        var source = new PamFileSource(TestFilePath);
        
        // Load to GPU using source abstraction
        using var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);
        
        // Verify
        Assert.True(deviceContracts.Count > 0);
        Assert.NotNull(deviceContracts.NotionalPrincipal);
    }

    [Fact]
    public async Task Example_LoadFromMockSource()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        var provider = new PamGpuProvider();
        
        // Generate 1000 mock contracts for testing
        var source = new PamMockSource(contractCount: 1000, seed: 42);
        
        // Load to GPU - completely decoupled from source type
        using var deviceContracts = await provider.LoadToGpuAsync(source, gpuContext);
        
        // Verify
        Assert.Equal(1000, deviceContracts.Count);
        Assert.NotNull(deviceContracts.NotionalPrincipal);
    }

    [Fact]
    public async Task Example_LoadFromCompositeSource()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        var provider = new PamGpuProvider();
        
        // Combine real data with synthetic data
        var fileSource = new PamFileSource(TestFilePath);
        var mockSource = new PamMockSource(500);
        var compositeSource = new PamCompositeSource(fileSource, mockSource);
        
        // Load everything to GPU in one call
        using var deviceContracts = await provider.LoadToGpuAsync(compositeSource, gpuContext);
        
        // Verify combined dataset
        Assert.True(deviceContracts.Count >= 503); // ~3 from file + 500 mock
    }

    [Fact]
    public async Task Example_LoadMultipleFilesAndMocks()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        var provider = new PamGpuProvider();
        
        // Create complex composite source
        var realData1 = new PamFileSource(TestFilePath);
        var realData2 = new PamFileSource(TestFilePath); // Could be different files
        var stressTest = new PamMockSource(1000, seed: 1);
        var baseCase = new PamMockSource(1000, seed: 2);
        
        var composite = new PamCompositeSource(realData1, realData2, stressTest, baseCase);
        
        // Load all to GPU
        using var deviceContracts = await provider.LoadToGpuAsync(composite, gpuContext);
        
        // Verify
        Assert.True(deviceContracts.Count >= 2006); // ~6 real + 2000 mock
    }

    [Fact]
    public async Task Example_BackwardCompatibility_FilePathStillWorks()
    {
        // Old API still works - backward compatible
        using var gpuContext = new GpuContext();
        var provider = new PamGpuProvider();
        
        // Direct file path - internally uses PamFileSource
        using var deviceContracts = await provider.LoadToGpuAsync(TestFilePath, gpuContext);
        
        // Verify
        Assert.True(deviceContracts.Count > 0);
    }

    [Fact]
    public async Task Example_CustomSourceImplementation()
    {
        // You can implement IPamContractSource for any data source
        // e.g., database, API, stream, etc.
        
        using var gpuContext = new GpuContext();
        var provider = new PamGpuProvider();
        
        // Use mock as example of custom source
        var customSource = new PamMockSource(100);
        
        using var deviceContracts = await provider.LoadToGpuAsync(customSource, gpuContext);
        
        Assert.Equal(100, deviceContracts.Count);
    }
}
