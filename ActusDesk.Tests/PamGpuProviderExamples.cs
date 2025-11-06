using ActusDesk.Gpu;
using ActusDesk.IO;
using ILGPU.Runtime;

namespace ActusDesk.Tests;

/// <summary>
/// Examples demonstrating PAM GPU provider usage
/// </summary>
public class PamGpuProviderExamples
{
    [Fact]
    public async Task Example_LoadSingleFileToGpu()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        
        // Create provider
        var provider = new PamGpuProvider();
        
        // Load test cases from file and transfer to GPU
        using var deviceContracts = await provider.LoadToGpuAsync(
            "../../../../data/tests/actus-tests-pam.json",
            gpuContext);
        
        // Verify data was loaded
        Assert.True(deviceContracts.Count > 0);
        Assert.NotNull(deviceContracts.NotionalPrincipal);
        Assert.NotNull(deviceContracts.NominalInterestRate);
        
        // GPU buffers are now ready for kernel execution
        // deviceContracts.NotionalPrincipal can be passed to ILGPU kernels
    }

    [Fact]
    public async Task Example_LoadMultipleFilesInParallel()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        
        // Create provider
        var provider = new PamGpuProvider();
        
        // Load multiple files in parallel
        var filePaths = new[]
        {
            "../../../../data/tests/actus-tests-pam.json",
            "../../../../data/tests/actus-tests-pam.json" // Can be different files
        };
        
        // All files are loaded in parallel and combined into single GPU buffer
        using var deviceContracts = await provider.LoadToGpuAsync(
            filePaths,
            gpuContext);
        
        // Verify combined data
        Assert.True(deviceContracts.Count >= 50); // At least 25 from each file
        
        // All contracts from all files are now in GPU memory as a single SoA
    }

    [Fact]
    public async Task Example_AccessGpuBuffers()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        
        // Load contracts to GPU
        var provider = new PamGpuProvider();
        using var deviceContracts = await provider.LoadToGpuAsync(
            "../../../../data/tests/actus-tests-pam.json",
            gpuContext);
        
        // Access individual buffers for GPU kernels
        var notionalBuffer = deviceContracts.NotionalPrincipal!;
        var rateBuffer = deviceContracts.NominalInterestRate!;
        var statusDateBuffer = deviceContracts.StatusDateYMD!;
        
        Assert.Equal(deviceContracts.Count, notionalBuffer.Length);
        Assert.Equal(deviceContracts.Count, rateBuffer.Length);
        Assert.Equal(deviceContracts.Count, statusDateBuffer.Length);
        
        // These buffers can be passed directly to ILGPU kernels
        // Example: myKernel(notionalBuffer.View, rateBuffer.View, ...)
    }

    [Fact]
    public async Task Example_ReadBackFromGpu()
    {
        // Create GPU context
        using var gpuContext = new GpuContext();
        
        // Load contracts to GPU
        var provider = new PamGpuProvider();
        using var deviceContracts = await provider.LoadToGpuAsync(
            "../../../../data/tests/actus-tests-pam.json",
            gpuContext);
        
        // Read data back from GPU to verify (using same pattern as PamGpuProviderTests)
        var notionals = new double[deviceContracts.Count];
        deviceContracts.NotionalPrincipal!.CopyToCPU(gpuContext.LoadStream, notionals);
        gpuContext.LoadStream.Synchronize();
        
        // Verify data
        Assert.All(notionals, n => Assert.True(n != 0));
    }
}
