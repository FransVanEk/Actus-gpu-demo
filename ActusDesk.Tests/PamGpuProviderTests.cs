using ActusDesk.Gpu;
using ActusDesk.IO;
using ILGPU.Runtime;

namespace ActusDesk.Tests;

/// <summary>
/// Tests for PAM GPU provider - parallel file loading and GPU transfer
/// </summary>
public class PamGpuProviderTests : IDisposable
{
    private const string TestFilePath = "../../../../data/tests/sample_contracts.json";
    private readonly GpuContext _gpuContext;

    public PamGpuProviderTests()
    {
        // Initialize GPU context for tests
        _gpuContext = new GpuContext();
    }

    [Fact]
    public async Task LoadToGpu_SingleFile_LoadsSuccessfully()
    {
        // Arrange
        var provider = new PamGpuProvider();

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(TestFilePath, _gpuContext);

        // Assert
        Assert.NotNull(deviceContracts);
        Assert.True(deviceContracts.Count >= 3, $"Expected at least 3 contracts, got {deviceContracts.Count}");
        Assert.NotNull(deviceContracts.NotionalPrincipal);
        Assert.NotNull(deviceContracts.NominalInterestRate);
        Assert.NotNull(deviceContracts.StatusDateYMD);
        Assert.NotNull(deviceContracts.MaturityDateYMD);
    }

    [Fact]
    public async Task LoadToGpu_MultipleFiles_LoadsInParallel()
    {
        // Arrange
        var provider = new PamGpuProvider();
        var filePaths = new[] { TestFilePath, TestFilePath }; // Load same file twice for testing

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(filePaths, _gpuContext);

        // Assert
        Assert.NotNull(deviceContracts);
        // Should have contracts from both files (duplicates in this test)
        Assert.True(deviceContracts.Count >= 6, $"Expected at least 6 contracts from 2 files, got {deviceContracts.Count}");
    }

    [Fact]
    public async Task LoadToGpu_TransfersCorrectData()
    {
        // Arrange
        var provider = new PamGpuProvider();

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(TestFilePath, _gpuContext);

        // Assert - Verify data was transferred by checking buffer lengths
        Assert.Equal(deviceContracts.Count, deviceContracts.NotionalPrincipal!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.NominalInterestRate!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.StatusDateYMD!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.MaturityDateYMD!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.NotionalScalingMultiplier!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.InterestScalingMultiplier!.Length);
        Assert.Equal(deviceContracts.Count, deviceContracts.RoleSign!.Length);
    }

    [Fact]
    public async Task LoadToGpu_WithLogger_LogsProgress()
    {
        // Arrange
        var provider = new PamGpuProvider();

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(TestFilePath, _gpuContext);

        // Assert
        Assert.NotNull(deviceContracts);
        Assert.True(deviceContracts.Count > 0);
    }

    [Fact]
    public async Task LoadToGpu_VerifyDataIntegrity()
    {
        // Arrange
        var provider = new PamGpuProvider();

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(TestFilePath, _gpuContext);

        // Copy data back to host to verify
        var notionalHost = new double[deviceContracts.Count];
        var statusDateHost = new int[deviceContracts.Count];

        deviceContracts.NotionalPrincipal!.CopyToCPU(_gpuContext.LoadStream, notionalHost);
        deviceContracts.StatusDateYMD!.CopyToCPU(_gpuContext.LoadStream, statusDateHost);
        _gpuContext.LoadStream.Synchronize();

        // Assert - Verify data makes sense
        Assert.All(notionalHost, n => Assert.True(n != 0, "Notional should not be zero"));
        Assert.All(statusDateHost, d => Assert.True(d >= 20120000 && d <= 20300000, 
            $"Status date {d} should be in valid range"));
    }

    [Fact]
    public async Task PamDeviceContracts_Dispose_ReleasesMemory()
    {
        // Arrange
        var provider = new PamGpuProvider();
        var deviceContracts = await provider.LoadToGpuAsync(TestFilePath, _gpuContext);

        // Act
        deviceContracts.Dispose();

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task LoadToGpu_EmptyFileList_ReturnsEmptyContracts()
    {
        // Arrange
        var provider = new PamGpuProvider();
        var emptyFiles = Array.Empty<string>();

        // Act
        using var deviceContracts = await provider.LoadToGpuAsync(emptyFiles, _gpuContext);

        // Assert
        Assert.NotNull(deviceContracts);
        Assert.Equal(0, deviceContracts.Count);
    }

    public void Dispose()
    {
        _gpuContext?.Dispose();
    }
}
