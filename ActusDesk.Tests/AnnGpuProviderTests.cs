using ActusDesk.Gpu;
using ActusDesk.IO;
using ILGPU.Runtime;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for ANN GPU provider - mock source loading and GPU transfer
/// </summary>
public class AnnGpuProviderTests : IDisposable
{
    private readonly GpuContext _gpuContext;

    public AnnGpuProviderTests()
    {
        // Initialize GPU context for tests
        _gpuContext = new GpuContext();
    }

    [Fact]
    public async Task LoadToGpu_FromMockSource_LoadsSuccessfully()
    {
        // Arrange
        var provider = new AnnGpuProvider();
        var source = new AnnMockSource(50, seed: 42);

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);

        // Assert
        Assert.NotNull(deviceContracts);
        Assert.Equal(50, deviceContracts.Count);
        Assert.NotNull(deviceContracts.NotionalPrincipal);
        Assert.NotNull(deviceContracts.NominalInterestRate);
        Assert.NotNull(deviceContracts.StatusDateYMD);
        Assert.NotNull(deviceContracts.MaturityDateYMD);
        Assert.NotNull(deviceContracts.NextPrincipalRedemptionPayment);
    }

    [Fact]
    public async Task LoadToGpu_TransfersCorrectData()
    {
        // Arrange
        var provider = new AnnGpuProvider();
        var source = new AnnMockSource(25, seed: 42);

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);

        // Assert - Verify data was transferred by checking buffer lengths
        Assert.Equal(deviceContracts.Count, deviceContracts.NotionalPrincipal!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.NominalInterestRate!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.StatusDateYMD!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.MaturityDateYMD!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.NotionalScalingMultiplier!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.InterestScalingMultiplier!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.RoleSign!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.NextPrincipalRedemptionPayment!.Length);
    }

    [Fact]
    public async Task LoadToGpu_VerifyDataIntegrity()
    {
        // Arrange
        var provider = new AnnGpuProvider();
        var source = new AnnMockSource(10, seed: 42);

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);

        // Copy data back to host to verify
        var notionalHost = new double[deviceContracts.Count];
        var statusDateHost = new int[deviceContracts.Count];
        var nextPrincipalHost = new double[deviceContracts.Count];

        deviceContracts.NotionalPrincipal!.CopyToCPU(_gpuContext.LoadStream, notionalHost);
        deviceContracts.StatusDateYMD!.CopyToCPU(_gpuContext.LoadStream, statusDateHost);
        deviceContracts.NextPrincipalRedemptionPayment!.CopyToCPU(_gpuContext.LoadStream, nextPrincipalHost);
        _gpuContext.LoadStream.Synchronize();

        // Assert - Verify data makes sense
        Assert.All(notionalHost, n => Assert.True(n > 0, "Notional should be positive"));
        Assert.All(statusDateHost, d => Assert.True(d >= 20240000 && d <= 20300000, 
            $"Status date {d} should be in valid range"));
        // NextPrincipalRedemptionPayment is optional, can be 0
    }

    [Fact]
    public async Task LoadToGpu_CompositeSource_CombinesContracts()
    {
        // Arrange
        var provider = new AnnGpuProvider();
        var source1 = new AnnMockSource(20, seed: 42);
        var source2 = new AnnMockSource(30, seed: 100);
        var composite = new AnnCompositeSource(source1, source2);

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(composite, _gpuContext);

        // Assert
        Assert.Equal(50, deviceContracts.Count); // 20 + 30
    }

    [Fact]
    public async Task AnnDeviceContracts_Dispose_ReleasesMemory()
    {
        // Arrange
        var provider = new AnnGpuProvider();
        var source = new AnnMockSource(10, seed: 42);
        var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);

        // Act
        deviceContracts.Dispose();

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task LoadToGpu_EmptySource_ReturnsEmptyContracts()
    {
        // Arrange
        var provider = new AnnGpuProvider();
        var source = new AnnMockSource(0);

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);

        // Assert
        Assert.NotNull(deviceContracts);
        Assert.Equal(0, deviceContracts.Count);
    }

    [Fact]
    public async Task LoadToGpu_LargeDataset_HandlesEfficiently()
    {
        // Arrange
        var provider = new AnnGpuProvider();
        var source = new AnnMockSource(1000, seed: 42);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var deviceContracts = await provider.LoadToGpuAsync(source, _gpuContext);
        sw.Stop();

        // Assert
        Assert.Equal(1000, deviceContracts.Count);
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Loading took {sw.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
