using ActusDesk.Domain.Pam;
using ActusDesk.Gpu;
using ILGPU;
using ILGPU.Runtime;
using Microsoft.Extensions.Logging;

namespace ActusDesk.IO;

/// <summary>
/// Provider interface for loading PAM contracts into GPU memory
/// </summary>
public interface IPamGpuProvider
{
    /// <summary>
    /// Load PAM contracts from files in parallel and transfer to GPU
    /// </summary>
    /// <param name="filePaths">Paths to JSON files containing PAM test cases</param>
    /// <param name="gpuContext">GPU context for memory allocation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GPU-ready contract data in SoA format</returns>
    Task<PamDeviceContracts> LoadToGpuAsync(
        IEnumerable<string> filePaths,
        GpuContext gpuContext,
        CancellationToken ct = default);

    /// <summary>
    /// Load PAM contracts from a single file and transfer to GPU
    /// </summary>
    /// <param name="filePath">Path to JSON file containing PAM test cases</param>
    /// <param name="gpuContext">GPU context for memory allocation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GPU-ready contract data in SoA format</returns>
    Task<PamDeviceContracts> LoadToGpuAsync(
        string filePath,
        GpuContext gpuContext,
        CancellationToken ct = default);
}

/// <summary>
/// GPU-ready PAM contract data in Struct-of-Arrays (SoA) format
/// Optimized for coalesced memory access on GPU
/// </summary>
public class PamDeviceContracts : IDisposable
{
    public int Count { get; init; }
    
    // Core fields
    public MemoryBuffer1D<double, Stride1D.Dense>? NotionalPrincipal { get; init; }
    public MemoryBuffer1D<double, Stride1D.Dense>? NominalInterestRate { get; init; }
    public MemoryBuffer1D<int, Stride1D.Dense>? StatusDateYMD { get; init; }
    public MemoryBuffer1D<int, Stride1D.Dense>? InitialExchangeDateYMD { get; init; }
    public MemoryBuffer1D<int, Stride1D.Dense>? MaturityDateYMD { get; init; }
    
    // Multipliers
    public MemoryBuffer1D<double, Stride1D.Dense>? NotionalScalingMultiplier { get; init; }
    public MemoryBuffer1D<double, Stride1D.Dense>? InterestScalingMultiplier { get; init; }
    
    // Contract role (1 = payer/RPL, -1 = receiver/RPA)
    public MemoryBuffer1D<int, Stride1D.Dense>? RoleSign { get; init; }

    public void Dispose()
    {
        NotionalPrincipal?.Dispose();
        NominalInterestRate?.Dispose();
        StatusDateYMD?.Dispose();
        InitialExchangeDateYMD?.Dispose();
        MaturityDateYMD?.Dispose();
        NotionalScalingMultiplier?.Dispose();
        InterestScalingMultiplier?.Dispose();
        RoleSign?.Dispose();
    }
}

/// <summary>
/// Implementation of PAM GPU provider with parallel file reading
/// </summary>
public class PamGpuProvider : IPamGpuProvider
{
    private readonly ILogger<PamGpuProvider>? _logger;

    public PamGpuProvider(ILogger<PamGpuProvider>? logger = null)
    {
        _logger = logger;
    }

    public async Task<PamDeviceContracts> LoadToGpuAsync(
        string filePath,
        GpuContext gpuContext,
        CancellationToken ct = default)
    {
        return await LoadToGpuAsync(new[] { filePath }, gpuContext, ct);
    }

    public async Task<PamDeviceContracts> LoadToGpuAsync(
        IEnumerable<string> filePaths,
        GpuContext gpuContext,
        CancellationToken ct = default)
    {
        var filePathList = filePaths.ToList();
        _logger?.LogInformation("Loading {FileCount} files in parallel to GPU", filePathList.Count);

        // Load all files in parallel
        var loadTasks = filePathList.Select(path => LoadFileAsync(path, ct)).ToList();
        var allTestCases = await Task.WhenAll(loadTasks);

        // Flatten all test cases from all files
        var allModels = allTestCases
            .SelectMany(testCases => testCases.Values)
            .Select(testCase => ActusPamMapper.MapToPamModel(testCase.Terms))
            .ToList();

        _logger?.LogInformation("Loaded {ContractCount} contracts from {FileCount} files", 
            allModels.Count, filePathList.Count);

        // Convert to SoA format and upload to GPU
        return await TransferToGpuAsync(allModels, gpuContext, ct);
    }

    private async Task<Dictionary<string, ActusTestCase>> LoadFileAsync(string filePath, CancellationToken ct)
    {
        _logger?.LogDebug("Loading file: {FilePath}", filePath);
        return await ActusPamMapper.LoadTestCasesAsync(filePath, ct);
    }

    private async Task<PamDeviceContracts> TransferToGpuAsync(
        List<PamContractModel> models,
        GpuContext gpuContext,
        CancellationToken ct)
    {
        int count = models.Count;
        _logger?.LogInformation("Transferring {Count} contracts to GPU", count);

        var accelerator = gpuContext.Accelerator;
        var loadStream = gpuContext.LoadStream;

        // Allocate device buffers
        var deviceContracts = new PamDeviceContracts
        {
            Count = count,
            NotionalPrincipal = accelerator.Allocate1D<double>(count),
            NominalInterestRate = accelerator.Allocate1D<double>(count),
            StatusDateYMD = accelerator.Allocate1D<int>(count),
            InitialExchangeDateYMD = accelerator.Allocate1D<int>(count),
            MaturityDateYMD = accelerator.Allocate1D<int>(count),
            NotionalScalingMultiplier = accelerator.Allocate1D<double>(count),
            InterestScalingMultiplier = accelerator.Allocate1D<double>(count),
            RoleSign = accelerator.Allocate1D<int>(count)
        };

        // Prepare host arrays
        var notionalPrincipal = new double[count];
        var nominalInterestRate = new double[count];
        var statusDateYMD = new int[count];
        var initialExchangeDateYMD = new int[count];
        var maturityDateYMD = new int[count];
        var notionalScalingMultiplier = new double[count];
        var interestScalingMultiplier = new double[count];
        var roleSign = new int[count];

        // Fill host arrays (in parallel for performance)
        await Task.Run(() =>
        {
            Parallel.For(0, count, i =>
            {
                var model = models[i];
                notionalPrincipal[i] = model.NotionalPrincipal;
                nominalInterestRate[i] = model.NominalInterestRate ?? 0.0;
                statusDateYMD[i] = DateToYMD(model.StatusDate);
                initialExchangeDateYMD[i] = model.InitialExchangeDate.HasValue 
                    ? DateToYMD(model.InitialExchangeDate.Value) 
                    : 0;
                maturityDateYMD[i] = DateToYMD(model.MaturityDate);
                notionalScalingMultiplier[i] = model.NotionalScalingMultiplier;
                interestScalingMultiplier[i] = model.InterestScalingMultiplier;
                roleSign[i] = model.ContractRole == "RPL" ? 1 : -1;
            });
        }, ct);

        // Transfer to GPU using the load stream
        deviceContracts.NotionalPrincipal!.CopyFromCPU(loadStream, notionalPrincipal);
        deviceContracts.NominalInterestRate!.CopyFromCPU(loadStream, nominalInterestRate);
        deviceContracts.StatusDateYMD!.CopyFromCPU(loadStream, statusDateYMD);
        deviceContracts.InitialExchangeDateYMD!.CopyFromCPU(loadStream, initialExchangeDateYMD);
        deviceContracts.MaturityDateYMD!.CopyFromCPU(loadStream, maturityDateYMD);
        deviceContracts.NotionalScalingMultiplier!.CopyFromCPU(loadStream, notionalScalingMultiplier);
        deviceContracts.InterestScalingMultiplier!.CopyFromCPU(loadStream, interestScalingMultiplier);
        deviceContracts.RoleSign!.CopyFromCPU(loadStream, roleSign);

        // Synchronize to ensure all transfers complete
        loadStream.Synchronize();

        _logger?.LogInformation("GPU transfer complete");

        return deviceContracts;
    }

    /// <summary>
    /// Convert DateTime to packed YYYYMMDD integer format for GPU
    /// </summary>
    private static int DateToYMD(DateTime date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }
}
