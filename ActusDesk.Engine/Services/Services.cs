using System.Buffers;
using ActusDesk.Domain;
using ActusDesk.Domain.Pam;
using ActusDesk.Engine.Models;
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
/// Handles loading, saving, and applying scenarios with multiple event types
/// </summary>
public class ScenarioService
{
    private readonly ILogger<ScenarioService> _logger;
    private readonly List<ScenarioDefinition> _scenarios = new();

    public ScenarioService(ILogger<ScenarioService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<ScenarioDefinition> Scenarios => _scenarios.AsReadOnly();

    /// <summary>
    /// Load scenarios from a JSON file
    /// </summary>
    public async Task LoadScenariosAsync(string file, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading scenarios from: {File}", file);
        
        if (!File.Exists(file))
        {
            _logger.LogWarning("Scenario file not found: {File}", file);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(file, ct);
            var scenarios = System.Text.Json.JsonSerializer.Deserialize<List<ScenarioDefinition>>(json);
            
            if (scenarios != null)
            {
                _scenarios.Clear();
                _scenarios.AddRange(scenarios);
                _logger.LogInformation("Loaded {Count} scenarios", _scenarios.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scenarios from {File}", file);
            throw;
        }
    }

    /// <summary>
    /// Save scenarios to a JSON file
    /// </summary>
    public async Task SaveScenariosAsync(string file, CancellationToken ct = default)
    {
        _logger.LogInformation("Saving {Count} scenarios to: {File}", _scenarios.Count, file);
        
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize(_scenarios, options);
            await File.WriteAllTextAsync(file, json, ct);
            _logger.LogInformation("Scenarios saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving scenarios to {File}", file);
            throw;
        }
    }

    /// <summary>
    /// Add a new scenario
    /// </summary>
    public void AddScenario(ScenarioDefinition scenario)
    {
        _scenarios.Add(scenario);
        _logger.LogInformation("Added scenario: {Name}", scenario.Name);
    }

    /// <summary>
    /// Remove a scenario by name
    /// </summary>
    public bool RemoveScenario(string name)
    {
        var removed = _scenarios.RemoveAll(s => s.Name == name);
        if (removed > 0)
        {
            _logger.LogInformation("Removed scenario: {Name}", name);
        }
        return removed > 0;
    }

    /// <summary>
    /// Get a scenario by name
    /// </summary>
    public ScenarioDefinition? GetScenario(string name)
    {
        return _scenarios.FirstOrDefault(s => s.Name == name);
    }

    /// <summary>
    /// Update an existing scenario
    /// </summary>
    public bool UpdateScenario(string name, ScenarioDefinition updatedScenario)
    {
        var index = _scenarios.FindIndex(s => s.Name == name);
        if (index >= 0)
        {
            _scenarios[index] = updatedScenario;
            _logger.LogInformation("Updated scenario: {Name}", name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clear all scenarios
    /// </summary>
    public void ClearScenarios()
    {
        _scenarios.Clear();
        _logger.LogInformation("Cleared all scenarios");
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
