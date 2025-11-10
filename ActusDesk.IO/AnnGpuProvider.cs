using ActusDesk.Domain.Ann;
using ActusDesk.Gpu;
using ILGPU;
using ILGPU.Runtime;
using Microsoft.Extensions.Logging;

namespace ActusDesk.IO;

/// <summary>
/// Provider interface for loading ANN contracts into GPU memory
/// Decoupled from contract source - works with any IAnnContractSource
/// </summary>
public interface IAnnGpuProvider
{
    /// <summary>
    /// Load ANN contracts from a source and transfer to GPU
    /// </summary>
    /// <param name="source">Contract source (file, mock, composite, etc.)</param>
    /// <param name="gpuContext">GPU context for memory allocation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GPU-ready contract data in SoA format</returns>
    Task<AnnDeviceContracts> LoadToGpuAsync(
        IAnnContractSource source,
        GpuContext gpuContext,
        CancellationToken ct = default);

    /// <summary>
    /// Load ANN contracts from files in parallel and transfer to GPU
    /// </summary>
    /// <param name="filePaths">Paths to JSON files containing ANN test cases</param>
    /// <param name="gpuContext">GPU context for memory allocation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GPU-ready contract data in SoA format</returns>
    Task<AnnDeviceContracts> LoadToGpuAsync(
        IEnumerable<string> filePaths,
        GpuContext gpuContext,
        CancellationToken ct = default);

    /// <summary>
    /// Load ANN contracts from a single file and transfer to GPU
    /// </summary>
    /// <param name="filePath">Path to JSON file containing ANN test cases</param>
    /// <param name="gpuContext">GPU context for memory allocation</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>GPU-ready contract data in SoA format</returns>
    Task<AnnDeviceContracts> LoadToGpuAsync(
        string filePath,
        GpuContext gpuContext,
        CancellationToken ct = default);
}

/// <summary>
/// GPU-ready ANN contract data in Struct-of-Arrays (SoA) format
/// Optimized for coalesced memory access on GPU
/// </summary>
public class AnnDeviceContracts : IDisposable
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

    // ANN-specific: next principal redemption payment (if fixed)
    public MemoryBuffer1D<double, Stride1D.Dense>? NextPrincipalRedemptionPayment { get; init; }

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
        NextPrincipalRedemptionPayment?.Dispose();
    }
}

/// <summary>
/// Implementation of ANN GPU provider with support for any contract source
/// Decouples contract source from GPU transfer logic
/// </summary>
public class AnnGpuProvider : IAnnGpuProvider
{
    private readonly ILogger<AnnGpuProvider>? _logger;

    public AnnGpuProvider(ILogger<AnnGpuProvider>? logger = null)
    {
        _logger = logger;
    }

    public async Task<AnnDeviceContracts> LoadToGpuAsync(
        IAnnContractSource source,
        GpuContext gpuContext,
        CancellationToken ct = default)
    {
        _logger?.LogInformation("Loading contracts from source: {SourceType}", source.GetType().Name);

        // Get contracts from the source
        var contracts = await source.GetContractsAsync(ct);
        var contractsList = contracts.ToList();

        _logger?.LogInformation("Loaded {ContractCount} contracts from source", contractsList.Count);

        // Transfer to GPU using streaming approach
        return await TransferToGpuStreamingAsync(contractsList, gpuContext, ct);
    }

    public async Task<AnnDeviceContracts> LoadToGpuAsync(
        string filePath,
        GpuContext gpuContext,
        CancellationToken ct = default)
    {
        var source = new AnnFileSource(filePath);
        return await LoadToGpuAsync(source, gpuContext, ct);
    }

    public async Task<AnnDeviceContracts> LoadToGpuAsync(
        IEnumerable<string> filePaths,
        GpuContext gpuContext,
        CancellationToken ct = default)
    {
        var source = new AnnFileSource(filePaths);
        return await LoadToGpuAsync(source, gpuContext, ct);
    }

    /// <summary>
    /// Transfer contracts to GPU using streaming/chunked approach for better parallelism
    /// This allows converting and transferring in parallel chunks
    /// </summary>
    private async Task<AnnDeviceContracts> TransferToGpuStreamingAsync(
        List<AnnContractModel> models,
        GpuContext gpuContext,
        CancellationToken ct)
    {
        int count = models.Count;
        _logger?.LogInformation("Transferring {Count} contracts to GPU using streaming approach", count);

        var accelerator = gpuContext.Accelerator;
        var loadStream = gpuContext.LoadStream;

        // Allocate device buffers upfront
        var deviceContracts = new AnnDeviceContracts
        {
            Count = count,
            NotionalPrincipal = accelerator.Allocate1D<double>(count),
            NominalInterestRate = accelerator.Allocate1D<double>(count),
            StatusDateYMD = accelerator.Allocate1D<int>(count),
            InitialExchangeDateYMD = accelerator.Allocate1D<int>(count),
            MaturityDateYMD = accelerator.Allocate1D<int>(count),
            NotionalScalingMultiplier = accelerator.Allocate1D<double>(count),
            InterestScalingMultiplier = accelerator.Allocate1D<double>(count),
            RoleSign = accelerator.Allocate1D<int>(count),
            NextPrincipalRedemptionPayment = accelerator.Allocate1D<double>(count)
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
        var nextPrincipalRedemptionPayment = new double[count];

        // Fill host arrays in parallel (chunked processing for better progress feedback)
        int chunkSize = Math.Min(10000, Math.Max(1000, count / 10));
        int numChunks = (count + chunkSize - 1) / chunkSize;

        _logger?.LogInformation("Converting {Count} contracts in {Chunks} chunks of ~{ChunkSize}", 
            count, numChunks, chunkSize);

        // Process conversion in chunks for better responsiveness
        for (int chunkIdx = 0; chunkIdx < numChunks; chunkIdx++)
        {
            int startIdx = chunkIdx * chunkSize;
            int endIdx = Math.Min(startIdx + chunkSize, count);

            // Convert chunk in parallel
            await Task.Run(() =>
            {
                Parallel.For(startIdx, endIdx, i =>
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
                    nextPrincipalRedemptionPayment[i] = model.NextPrincipalRedemptionPayment ?? 0.0;
                });
            }, ct);

            // Allow other async work to proceed between chunks
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }

        _logger?.LogInformation("Transferring converted data to GPU...");

        // Transfer all data to GPU in one go (ILGPU handles this efficiently)
        deviceContracts.NotionalPrincipal!.CopyFromCPU(loadStream, notionalPrincipal);
        deviceContracts.NominalInterestRate!.CopyFromCPU(loadStream, nominalInterestRate);
        deviceContracts.StatusDateYMD!.CopyFromCPU(loadStream, statusDateYMD);
        deviceContracts.InitialExchangeDateYMD!.CopyFromCPU(loadStream, initialExchangeDateYMD);
        deviceContracts.MaturityDateYMD!.CopyFromCPU(loadStream, maturityDateYMD);
        deviceContracts.NotionalScalingMultiplier!.CopyFromCPU(loadStream, notionalScalingMultiplier);
        deviceContracts.InterestScalingMultiplier!.CopyFromCPU(loadStream, interestScalingMultiplier);
        deviceContracts.RoleSign!.CopyFromCPU(loadStream, roleSign);
        deviceContracts.NextPrincipalRedemptionPayment!.CopyFromCPU(loadStream, nextPrincipalRedemptionPayment);

        // Synchronize to ensure all transfers complete
        loadStream.Synchronize();

        _logger?.LogInformation("GPU transfer complete");

        return deviceContracts;
    }

    /// <summary>
    /// Legacy transfer method (kept for backward compatibility)
    /// </summary>
    private async Task<AnnDeviceContracts> TransferToGpuAsync(
        List<AnnContractModel> models,
        GpuContext gpuContext,
        CancellationToken ct)
    {
        // Delegate to streaming version
        return await TransferToGpuStreamingAsync(models, gpuContext, ct);
    }

    /// <summary>
    /// Convert DateTime to packed YYYYMMDD integer format for GPU
    /// </summary>
    private static int DateToYMD(DateTime date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }
}
