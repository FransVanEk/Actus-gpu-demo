using ActusDesk.Gpu;
using ActusDesk.IO;

namespace ActusDesk.Tests;

/// <summary>
/// Examples demonstrating ANN contract usage
/// These examples show how to work with ANN contracts in the same way as PAM contracts
/// </summary>
public class AnnExamples : IDisposable
{
    private readonly GpuContext _gpuContext;

    public AnnExamples()
    {
        _gpuContext = new GpuContext();
    }

    [Fact]
    public async Task Example_LoadAnnContractsFromMockSource()
    {
        // Create a mock source with 100 ANN contracts
        var source = new AnnMockSource(contractCount: 100, seed: 42);

        // Load contracts
        var contracts = await source.GetContractsAsync();
        var contractsList = contracts.ToList();

        // Verify contracts loaded correctly
        Assert.Equal(100, contractsList.Count);
        Assert.All(contractsList, c => 
        {
            Assert.NotNull(c.CycleOfPrincipalRedemption);
            Assert.NotNull(c.CycleAnchorDateOfPrincipalRedemption);
        });
    }

    [Fact]
    public async Task Example_LoadAnnContractsToGpu()
    {
        // Create a mock source
        var source = new AnnMockSource(contractCount: 1000, seed: 42);

        // Create GPU provider
        var provider = new AnnGpuProvider();

        // Load to GPU
        using var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);

        // Verify GPU buffers
        Assert.Equal(1000, deviceContracts.Count);
        Assert.NotNull(deviceContracts.NotionalPrincipal);
        Assert.NotNull(deviceContracts.NominalInterestRate);
        Assert.NotNull(deviceContracts.NextPrincipalRedemptionPayment);
    }

    [Fact]
    public async Task Example_CombineMultipleAnnSources()
    {
        // Create multiple sources
        var mockSource1 = new AnnMockSource(contractCount: 100, seed: 1);
        var mockSource2 = new AnnMockSource(contractCount: 200, seed: 2);

        // Combine sources
        var composite = new AnnCompositeSource(mockSource1, mockSource2);

        // Load all contracts
        var contracts = await composite.GetContractsAsync();
        var contractsList = contracts.ToList();

        // Verify combined results
        Assert.Equal(300, contractsList.Count);
    }

    [Fact]
    public async Task Example_ProcessAnnContractsInBatches()
    {
        // Create a large dataset
        var source = new AnnMockSource(contractCount: 10000, seed: 42);
        var provider = new AnnGpuProvider();

        // Load to GPU
        using var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);

        // Process in batches (example: read back in chunks)
        const int batchSize = 1000;
        for (int batchStart = 0; batchStart < deviceContracts.Count; batchStart += batchSize)
        {
            int currentBatchSize = Math.Min(batchSize, deviceContracts.Count - batchStart);
            
            // In real usage, you would launch GPU kernels here for each batch
            // For this example, we just verify the batch is valid
            Assert.True(currentBatchSize > 0);
        }
    }

    [Fact]
    public async Task Example_CompareAnnAndPamDataVolume()
    {
        // Create same number of contracts for both types
        var annSource = new AnnMockSource(contractCount: 1000, seed: 42);
        var pamSource = new PamMockSource(contractCount: 1000, seed: 42);

        var annProvider = new AnnGpuProvider();
        var pamProvider = new PamGpuProvider();

        // Load both to GPU
        using var annContracts = await annProvider.LoadToGpuAsync(annSource, _gpuContext);
        using var pamContracts = await pamProvider.LoadToGpuAsync(pamSource, _gpuContext);

        // Both should have same count
        Assert.Equal(annContracts.Count, pamContracts.Count);

        // ANN has one extra field: NextPrincipalRedemptionPayment
        Assert.NotNull(annContracts.NextPrincipalRedemptionPayment);
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
