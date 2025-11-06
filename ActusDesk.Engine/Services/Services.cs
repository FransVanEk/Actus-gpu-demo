using System.Buffers;
using ActusDesk.Domain;
using ActusDesk.Domain.Pam;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Engine.Services;

/// <summary>
/// Service for loading and managing PAM contract data
/// Handles contract loading from various sources and GPU upload
/// </summary>
public class ContractsService
{
    private readonly ILogger<ContractsService> _logger;
    private readonly GpuContext _gpuContext;
    private readonly IPamGpuProvider _pamGpuProvider;
    private PamDeviceContracts? _deviceContracts;

    public ContractsService(
        ILogger<ContractsService> logger, 
        GpuContext gpuContext,
        IPamGpuProvider pamGpuProvider)
    {
        _logger = logger;
        _gpuContext = gpuContext;
        _pamGpuProvider = pamGpuProvider;
    }

    public int ContractCount => _deviceContracts?.Count ?? 0;

    /// <summary>
    /// Load PAM contracts from JSON files and upload to GPU
    /// </summary>
    public async Task LoadFromJsonAsync(string[] files, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading contracts from {Count} JSON files", files.Length);
        
        // Dispose previous contracts if any
        _deviceContracts?.Dispose();
        
        // Use file source and load to GPU
        var source = new PamFileSource(files);
        _deviceContracts = await _pamGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Loaded {Count} contracts to GPU", _deviceContracts.Count);
    }

    /// <summary>
    /// Load contracts from a contract source (file, mock, composite, etc.)
    /// </summary>
    public async Task LoadFromSourceAsync(IPamContractSource source, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading contracts from source: {SourceType}", source.GetType().Name);
        
        // Dispose previous contracts if any
        _deviceContracts?.Dispose();
        
        // Load to GPU
        _deviceContracts = await _pamGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Loaded {Count} contracts to GPU", _deviceContracts.Count);
    }

    /// <summary>
    /// Generate mock contracts for testing and upload to GPU
    /// </summary>
    public async Task LoadMockContractsAsync(int contractCount, int? seed = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} mock contracts", contractCount);
        
        // Dispose previous contracts if any
        _deviceContracts?.Dispose();
        
        // Use mock source
        var source = new PamMockSource(contractCount, seed);
        _deviceContracts = await _pamGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Generated and loaded {Count} mock contracts to GPU", _deviceContracts.Count);
    }

    /// <summary>
    /// Get the device contracts for GPU operations
    /// </summary>
    public PamDeviceContracts? GetDeviceContracts() => _deviceContracts;

    public async Task LoadFromCacheAsync(string cachePath, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading contracts from cache: {Path}", cachePath);
        // TODO: Implement cache loading
        await Task.CompletedTask;
    }

    public async Task UploadToDeviceAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Uploading contracts to GPU device");
        // TODO: This is now handled by LoadFromJsonAsync and LoadFromSourceAsync
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _deviceContracts?.Dispose();
    }
}

/// <summary>
/// Service for managing valuation scenarios
/// </summary>
public class ScenarioService
{
    private readonly ILogger<ScenarioService> _logger;

    public ScenarioService(ILogger<ScenarioService> logger)
    {
        _logger = logger;
    }

    public Task LoadScenariosAsync(string file, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading scenarios from: {File}", file);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Service for executing valuations on GPU
/// </summary>
public class ValuationService
{
    private readonly ILogger<ValuationService> _logger;
    private readonly GpuContext _gpuContext;

    public ValuationService(ILogger<ValuationService> logger, GpuContext gpuContext)
    {
        _logger = logger;
        _gpuContext = gpuContext;
    }

    public Task<ValuationResults> RunValuationAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting valuation run");
        // TODO: Implement GPU valuation
        return Task.FromResult(new ValuationResults());
    }
}

/// <summary>
/// Service for generating reports and exports
/// </summary>
public class ReportingService
{
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(ILogger<ReportingService> logger)
    {
        _logger = logger;
    }

    public Task ExportResultsAsync(ValuationResults results, string outputPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting results to: {Path}", outputPath);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Valuation results container
/// </summary>
public class ValuationResults
{
    public int ContractCount { get; set; }
    public int ScenarioCount { get; set; }
    public TimeSpan Duration { get; set; }
}
