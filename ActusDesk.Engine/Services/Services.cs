using System.Buffers;
using ActusDesk.Domain;
using ActusDesk.Domain.Pam;
using ActusDesk.Domain.Ann;
using ActusDesk.Gpu;
using ActusDesk.IO;
using Microsoft.Extensions.Logging;

namespace ActusDesk.Engine.Services;

/// <summary>
/// Service for loading and managing contract data (PAM and ANN)
/// Handles contract loading from various sources and GPU upload
/// </summary>
public class ContractsService
{
    private readonly ILogger<ContractsService> _logger;
    private readonly GpuContext _gpuContext;
    private readonly IPamGpuProvider _pamGpuProvider;
    private readonly IAnnGpuProvider _annGpuProvider;
    private PamDeviceContracts? _pamDeviceContracts;
    private AnnDeviceContracts? _annDeviceContracts;

    public ContractsService(
        ILogger<ContractsService> logger, 
        GpuContext gpuContext,
        IPamGpuProvider pamGpuProvider,
        IAnnGpuProvider annGpuProvider)
    {
        _logger = logger;
        _gpuContext = gpuContext;
        _pamGpuProvider = pamGpuProvider;
        _annGpuProvider = annGpuProvider;
    }

    public int ContractCount => (_pamDeviceContracts?.Count ?? 0) + (_annDeviceContracts?.Count ?? 0);
    public int PamContractCount => _pamDeviceContracts?.Count ?? 0;
    public int AnnContractCount => _annDeviceContracts?.Count ?? 0;

    /// <summary>
    /// Load PAM contracts from JSON files and upload to GPU
    /// </summary>
    public async Task LoadFromJsonAsync(string[] files, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading contracts from {Count} JSON files", files.Length);
        
        // Dispose previous contracts if any
        _pamDeviceContracts?.Dispose();
        
        // Use file source and load to GPU
        var source = new PamFileSource(files);
        _pamDeviceContracts = await _pamGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Loaded {Count} PAM contracts to GPU", _pamDeviceContracts.Count);
    }

    /// <summary>
    /// Load contracts from a PAM contract source (file, mock, composite, etc.)
    /// </summary>
    public async Task LoadFromSourceAsync(IPamContractSource source, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading PAM contracts from source: {SourceType}", source.GetType().Name);
        
        // Dispose previous contracts if any
        _pamDeviceContracts?.Dispose();
        
        // Load to GPU
        _pamDeviceContracts = await _pamGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Loaded {Count} PAM contracts to GPU", _pamDeviceContracts.Count);
    }

    /// <summary>
    /// Load contracts from an ANN contract source (file, mock, composite, etc.)
    /// </summary>
    public async Task LoadAnnFromSourceAsync(IAnnContractSource source, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading ANN contracts from source: {SourceType}", source.GetType().Name);
        
        // Dispose previous contracts if any
        _annDeviceContracts?.Dispose();
        
        // Load to GPU
        _annDeviceContracts = await _annGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Loaded {Count} ANN contracts to GPU", _annDeviceContracts.Count);
    }

    /// <summary>
    /// Generate mock PAM contracts for testing and upload to GPU
    /// </summary>
    public async Task LoadMockContractsAsync(int contractCount, int? seed = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} mock PAM contracts", contractCount);
        
        // Dispose previous contracts if any
        _pamDeviceContracts?.Dispose();
        
        // Use mock source
        var source = new PamMockSource(contractCount, seed);
        _pamDeviceContracts = await _pamGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Generated and loaded {Count} mock PAM contracts to GPU", _pamDeviceContracts.Count);
    }

    /// <summary>
    /// Generate mock ANN contracts for testing and upload to GPU
    /// </summary>
    public async Task LoadMockAnnContractsAsync(int contractCount, int? seed = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} mock ANN contracts", contractCount);
        
        // Dispose previous contracts if any
        _annDeviceContracts?.Dispose();
        
        // Use mock source
        var source = new AnnMockSource(contractCount, seed);
        _annDeviceContracts = await _annGpuProvider.LoadToGpuAsync(source, _gpuContext, ct);
        
        _logger.LogInformation("Generated and loaded {Count} mock ANN contracts to GPU", _annDeviceContracts.Count);
    }

    /// <summary>
    /// Load both PAM and ANN mock contracts
    /// </summary>
    public async Task LoadMixedMockContractsAsync(int pamCount, int annCount, int? seed = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {PamCount} PAM and {AnnCount} ANN mock contracts", pamCount, annCount);
        
        // Load PAM contracts
        await LoadMockContractsAsync(pamCount, seed, ct);
        
        // Load ANN contracts with different seed to ensure variety
        await LoadMockAnnContractsAsync(annCount, seed.HasValue ? seed.Value + 1000 : null, ct);
        
        _logger.LogInformation("Loaded {Total} total contracts ({Pam} PAM + {Ann} ANN) to GPU", 
            ContractCount, PamContractCount, AnnContractCount);
    }

    /// <summary>
    /// Get the PAM device contracts for GPU operations
    /// </summary>
    public PamDeviceContracts? GetPamDeviceContracts() => _pamDeviceContracts;

    /// <summary>
    /// Get the ANN device contracts for GPU operations
    /// </summary>
    public AnnDeviceContracts? GetAnnDeviceContracts() => _annDeviceContracts;

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
        _pamDeviceContracts?.Dispose();
        _annDeviceContracts?.Dispose();
    }
}

/// <summary>
/// Service for managing valuation scenarios
/// </summary>
public class ScenarioService
{
    private readonly ILogger<ScenarioService> _logger;
    private List<ValuationScenario> _scenarios = new();

    public ScenarioService(ILogger<ScenarioService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<ValuationScenario> Scenarios => _scenarios;

    public Task LoadScenariosAsync(string file, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading scenarios from: {File}", file);
        // TODO: Load from file
        return Task.CompletedTask;
    }

    /// <summary>
    /// Load default scenarios for testing
    /// </summary>
    public Task LoadDefaultScenariosAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Loading default scenarios");
        
        _scenarios = new List<ValuationScenario>
        {
            new ValuationScenario 
            { 
                Name = "Base Case", 
                Description = "No rate changes",
                RateBumpBps = 0
            },
            new ValuationScenario 
            { 
                Name = "Rate +50bps", 
                Description = "Parallel rate bump +50 basis points",
                RateBumpBps = 50
            },
            new ValuationScenario 
            { 
                Name = "Rate -50bps", 
                Description = "Parallel rate bump -50 basis points",
                RateBumpBps = -50
            }
        };
        
        _logger.LogInformation("Loaded {Count} default scenarios", _scenarios.Count);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Valuation scenario definition
/// </summary>
public class ValuationScenario
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int RateBumpBps { get; set; }
}

/// <summary>
/// Service for executing valuations on GPU
/// </summary>
public class ValuationService
{
    private readonly ILogger<ValuationService> _logger;
    private readonly GpuContext _gpuContext;
    private readonly ContractsService _contractsService;
    private readonly ScenarioService _scenarioService;

    public ValuationService(
        ILogger<ValuationService> logger, 
        GpuContext gpuContext,
        ContractsService contractsService,
        ScenarioService scenarioService)
    {
        _logger = logger;
        _gpuContext = gpuContext;
        _contractsService = contractsService;
        _scenarioService = scenarioService;
    }

    public async Task<ValuationResults> RunValuationAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.Now;
        var valuationStart = DateTime.Now;
        var valuationEnd = valuationStart.AddYears(10); // 10 years from now
        
        _logger.LogInformation("Starting valuation run from {Start} to {End}", valuationStart, valuationEnd);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Get contracts
        var pamContracts = _contractsService.GetPamDeviceContracts();
        var annContracts = _contractsService.GetAnnDeviceContracts();
        
        int totalContracts = (pamContracts?.Count ?? 0) + (annContracts?.Count ?? 0);
        int scenarioCount = _scenarioService.Scenarios.Count;
        
        if (totalContracts == 0)
        {
            _logger.LogWarning("No contracts loaded");
            return new ValuationResults 
            { 
                ContractCount = 0,
                ScenarioCount = scenarioCount,
                Duration = stopwatch.Elapsed,
                Message = "No contracts loaded"
            };
        }

        if (scenarioCount == 0)
        {
            _logger.LogWarning("No scenarios loaded");
            return new ValuationResults 
            { 
                ContractCount = totalContracts,
                ScenarioCount = 0,
                Duration = stopwatch.Elapsed,
                Message = "No scenarios loaded"
            };
        }
        
        _logger.LogInformation("Running valuation for {Contracts} contracts across {Scenarios} scenarios", 
            totalContracts, scenarioCount);
        
        // Simulate GPU valuation - in real implementation this would:
        // 1. Generate event schedules for all contracts
        // 2. Apply scenarios to rates
        // 3. Calculate present values
        // 4. Aggregate results
        await Task.Delay(100, ct); // Simulate GPU work
        
        stopwatch.Stop();
        
        var results = new ValuationResults
        {
            ContractCount = totalContracts,
            PamContractCount = pamContracts?.Count ?? 0,
            AnnContractCount = annContracts?.Count ?? 0,
            ScenarioCount = scenarioCount,
            Duration = stopwatch.Elapsed,
            ValuationStartDate = valuationStart,
            ValuationEndDate = valuationEnd,
            Message = $"Valuation complete: {totalContracts:N0} contracts × {scenarioCount} scenarios in {stopwatch.ElapsedMilliseconds}ms"
        };
        
        _logger.LogInformation("Valuation completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
        
        return results;
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
    public int PamContractCount { get; set; }
    public int AnnContractCount { get; set; }
    public int ScenarioCount { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ValuationStartDate { get; set; }
    public DateTime ValuationEndDate { get; set; }
    public string Message { get; set; } = "";
}
