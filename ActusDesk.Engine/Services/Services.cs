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
    private readonly ContractRegistry _contractRegistry;
    private PamDeviceContracts? _pamDeviceContracts;
    private AnnDeviceContracts? _annDeviceContracts;

    public ContractsService(
        ILogger<ContractsService> logger, 
        GpuContext gpuContext,
        IPamGpuProvider pamGpuProvider,
        IAnnGpuProvider annGpuProvider,
        ContractRegistry contractRegistry)
    {
        _logger = logger;
        _gpuContext = gpuContext;
        _pamGpuProvider = pamGpuProvider;
        _annGpuProvider = annGpuProvider;
        _contractRegistry = contractRegistry;
    }

    public int ContractCount => (_pamDeviceContracts?.Count ?? 0) + (_annDeviceContracts?.Count ?? 0);
    public int PamContractCount => _pamDeviceContracts?.Count ?? 0;
    public int AnnContractCount => _annDeviceContracts?.Count ?? 0;
    public ContractRegistry ContractRegistry => _contractRegistry;

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

    private const int AnnSeedOffset = 1000; // Offset for ANN seed to ensure variety when loading mixed contracts

    /// <summary>
    /// Load both PAM and ANN mock contracts based on registry percentages
    /// </summary>
    public async Task LoadMixedMockContractsAsync(int totalContracts, int? seed = null, CancellationToken ct = default)
    {
        // Calculate counts based on registry percentages
        var counts = _contractRegistry.CalculateContractCounts(totalContracts);
        
        int pamCount = counts.GetValueOrDefault("PAM", 0);
        int annCount = counts.GetValueOrDefault("ANN", 0);
        
        _logger.LogInformation("Generating {Total} contracts based on registry: PAM={PamCount} ({PamPct:F1}%), ANN={AnnCount} ({AnnPct:F1}%)", 
            totalContracts, 
            pamCount, (pamCount * 100.0 / totalContracts),
            annCount, (annCount * 100.0 / totalContracts));
        
        // Load PAM contracts
        if (pamCount > 0)
        {
            await LoadMockContractsAsync(pamCount, seed, ct);
        }
        
        // Load ANN contracts with different seed to ensure variety
        if (annCount > 0)
        {
            await LoadMockAnnContractsAsync(annCount, seed.HasValue ? seed.Value + AnnSeedOffset : null, ct);
        }
        
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

    public async Task<ValuationResults> RunValuationAsync(
        CancellationToken ct = default,
        IProgress<ValuationProgress>? progress = null)
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
        
        // Report initial progress
        progress?.Report(new ValuationProgress
        {
            Stage = "Initializing",
            ProcessedContracts = 0,
            TotalContracts = totalContracts,
            ProcessedScenarios = 0,
            TotalScenarios = scenarioCount,
            PercentComplete = 0,
            Message = "Starting valuation..."
        });

        // Dictionary to aggregate events by date
        var eventsByDate = new Dictionary<DateOnly, List<ContractEvent>>();
        
        // Process scenarios
        for (int scenarioIdx = 0; scenarioIdx < scenarioCount; scenarioIdx++)
        {
            var scenario = _scenarioService.Scenarios[scenarioIdx];
            
            progress?.Report(new ValuationProgress
            {
                Stage = "Processing Scenarios",
                ProcessedContracts = 0,
                TotalContracts = totalContracts,
                ProcessedScenarios = scenarioIdx,
                TotalScenarios = scenarioCount,
                PercentComplete = (scenarioIdx * 100.0) / scenarioCount,
                Message = $"Processing scenario: {scenario.Name}"
            });
            
            _logger.LogInformation("Processing scenario {ScenarioIdx}/{Total}: {Name}", 
                scenarioIdx + 1, scenarioCount, scenario.Name);

            // Calculate rate adjustment based on scenario
            double rateAdjustment = scenario.RateBumpBps / 10000.0; // Convert basis points to decimal
            
            // Process PAM contracts
            if (pamContracts != null && pamContracts.Count > 0)
            {
                await ProcessPamContractsAsync(
                    pamContracts, 
                    scenarioIdx, 
                    scenario,
                    rateAdjustment,
                    valuationStart, 
                    valuationEnd, 
                    eventsByDate, 
                    progress, 
                    totalContracts, 
                    scenarioCount,
                    ct);
            }
            
            // Process ANN contracts
            if (annContracts != null && annContracts.Count > 0)
            {
                await ProcessAnnContractsAsync(
                    annContracts,
                    scenarioIdx,
                    scenario,
                    rateAdjustment,
                    valuationStart,
                    valuationEnd,
                    eventsByDate,
                    progress,
                    totalContracts,
                    scenarioCount,
                    ct);
            }
            
            // Allow UI to update
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }

        // Aggregate results by day
        progress?.Report(new ValuationProgress
        {
            Stage = "Aggregating Results",
            ProcessedContracts = totalContracts,
            TotalContracts = totalContracts,
            ProcessedScenarios = scenarioCount,
            TotalScenarios = scenarioCount,
            PercentComplete = 95,
            Message = "Aggregating results by day..."
        });
        
        var dayEventValues = new List<DayEventValue>();
        foreach (var kvp in eventsByDate.OrderBy(x => x.Key))
        {
            var dayValue = new DayEventValue
            {
                Date = kvp.Key,
                Events = kvp.Value,
                TotalPayoff = kvp.Value.Sum(e => e.Payoff),
                TotalPresentValue = kvp.Value.Sum(e => e.PresentValue)
            };
            dayEventValues.Add(dayValue);
        }
        
        stopwatch.Stop();
        
        progress?.Report(new ValuationProgress
        {
            Stage = "Complete",
            ProcessedContracts = totalContracts,
            TotalContracts = totalContracts,
            ProcessedScenarios = scenarioCount,
            TotalScenarios = scenarioCount,
            PercentComplete = 100,
            Message = "Valuation complete!"
        });
        
        var results = new ValuationResults
        {
            ContractCount = totalContracts,
            PamContractCount = pamContracts?.Count ?? 0,
            AnnContractCount = annContracts?.Count ?? 0,
            ScenarioCount = scenarioCount,
            Duration = stopwatch.Elapsed,
            ValuationStartDate = valuationStart,
            ValuationEndDate = valuationEnd,
            DayEventValues = dayEventValues,
            Message = $"Valuation complete: {totalContracts:N0} contracts × {scenarioCount} scenarios in {stopwatch.ElapsedMilliseconds}ms"
        };
        
        _logger.LogInformation("Valuation completed in {Duration}ms with {EventDays} event days", 
            stopwatch.ElapsedMilliseconds, dayEventValues.Count);
        
        return results;
    }

    private async Task ProcessPamContractsAsync(
        PamDeviceContracts pamContracts,
        int scenarioIdx,
        ValuationScenario scenario,
        double rateAdjustment,
        DateTime valuationStart,
        DateTime valuationEnd,
        Dictionary<DateOnly, List<ContractEvent>> eventsByDate,
        IProgress<ValuationProgress>? progress,
        int totalContracts,
        int scenarioCount,
        CancellationToken ct)
    {
        // For demo purposes, generate sample events for a subset of contracts
        // In a real implementation, this would use GPU kernels to generate events
        int sampleSize = Math.Min(pamContracts.Count, 100); // Process up to 100 contracts for demo
        
        for (int i = 0; i < sampleSize; i++)
        {
            if (i % 10 == 0) // Report progress every 10 contracts
            {
                progress?.Report(new ValuationProgress
                {
                    Stage = $"Processing PAM Contracts (Scenario: {scenario.Name})",
                    ProcessedContracts = i,
                    TotalContracts = totalContracts,
                    ProcessedScenarios = scenarioIdx,
                    TotalScenarios = scenarioCount,
                    PercentComplete = ((scenarioIdx * 100.0) + (i * 100.0 / totalContracts)) / scenarioCount,
                    Message = $"Processing PAM contract {i + 1}/{sampleSize} in scenario '{scenario.Name}'"
                });
                
                // Allow UI to update
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
            }
            
            // Generate sample events for this contract
            GenerateSamplePamEvents(
                $"PAM_{i}",
                valuationStart,
                valuationEnd,
                rateAdjustment,
                eventsByDate);
        }
        
        _logger.LogInformation("Processed {Count} PAM contracts for scenario {Scenario}", 
            sampleSize, scenario.Name);
    }

    private async Task ProcessAnnContractsAsync(
        AnnDeviceContracts annContracts,
        int scenarioIdx,
        ValuationScenario scenario,
        double rateAdjustment,
        DateTime valuationStart,
        DateTime valuationEnd,
        Dictionary<DateOnly, List<ContractEvent>> eventsByDate,
        IProgress<ValuationProgress>? progress,
        int totalContracts,
        int scenarioCount,
        CancellationToken ct)
    {
        // For demo purposes, generate sample events for a subset of contracts
        int sampleSize = Math.Min(annContracts.Count, 100); // Process up to 100 contracts for demo
        
        for (int i = 0; i < sampleSize; i++)
        {
            if (i % 10 == 0) // Report progress every 10 contracts
            {
                progress?.Report(new ValuationProgress
                {
                    Stage = $"Processing ANN Contracts (Scenario: {scenario.Name})",
                    ProcessedContracts = sampleSize + i,
                    TotalContracts = totalContracts,
                    ProcessedScenarios = scenarioIdx,
                    TotalScenarios = scenarioCount,
                    PercentComplete = ((scenarioIdx * 100.0) + ((sampleSize + i) * 100.0 / totalContracts)) / scenarioCount,
                    Message = $"Processing ANN contract {i + 1}/{sampleSize} in scenario '{scenario.Name}'"
                });
                
                // Allow UI to update
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
            }
            
            // Generate sample events for this contract
            GenerateSampleAnnEvents(
                $"ANN_{i}",
                valuationStart,
                valuationEnd,
                rateAdjustment,
                eventsByDate);
        }
        
        _logger.LogInformation("Processed {Count} ANN contracts for scenario {Scenario}", 
            sampleSize, scenario.Name);
    }

    private void GenerateSamplePamEvents(
        string contractId,
        DateTime valuationStart,
        DateTime valuationEnd,
        double rateAdjustment,
        Dictionary<DateOnly, List<ContractEvent>> eventsByDate)
    {
        var random = new Random(contractId.GetHashCode());
        double notional = 100000 + random.NextDouble() * 900000; // 100k to 1M
        double rate = 0.05 + rateAdjustment; // 5% base rate + scenario adjustment
        
        // IED - Initial Exchange
        var iedDate = DateOnly.FromDateTime(valuationStart);
        AddEvent(eventsByDate, iedDate, new ContractEvent
        {
            ContractId = contractId,
            ContractType = "PAM",
            EventType = "IED",
            Payoff = -(decimal)notional,
            PresentValue = -(decimal)notional,
            Currency = "USD"
        });
        
        // IP - Interest Payments (quarterly)
        var currentDate = valuationStart.AddMonths(3);
        while (currentDate <= valuationEnd)
        {
            double interest = notional * rate * 0.25; // Quarterly interest
            double discountFactor = Math.Pow(1 + rate, -((currentDate - valuationStart).TotalDays / 365.0));
            
            AddEvent(eventsByDate, DateOnly.FromDateTime(currentDate), new ContractEvent
            {
                ContractId = contractId,
                ContractType = "PAM",
                EventType = "IP",
                Payoff = (decimal)interest,
                PresentValue = (decimal)(interest * discountFactor),
                Currency = "USD"
            });
            
            currentDate = currentDate.AddMonths(3);
        }
        
        // MD - Maturity (principal repayment)
        var mdDate = DateOnly.FromDateTime(valuationEnd);
        double mdDiscountFactor = Math.Pow(1 + rate, -((valuationEnd - valuationStart).TotalDays / 365.0));
        AddEvent(eventsByDate, mdDate, new ContractEvent
        {
            ContractId = contractId,
            ContractType = "PAM",
            EventType = "MD",
            Payoff = (decimal)notional,
            PresentValue = (decimal)(notional * mdDiscountFactor),
            Currency = "USD"
        });
    }

    private void GenerateSampleAnnEvents(
        string contractId,
        DateTime valuationStart,
        DateTime valuationEnd,
        double rateAdjustment,
        Dictionary<DateOnly, List<ContractEvent>> eventsByDate)
    {
        var random = new Random(contractId.GetHashCode());
        double notional = 50000 + random.NextDouble() * 450000; // 50k to 500k
        double rate = 0.04 + rateAdjustment; // 4% base rate + scenario adjustment
        int periods = 40; // 10 years quarterly
        
        // Calculate annuity payment
        double payment = notional * (rate / 4) / (1 - Math.Pow(1 + rate / 4, -periods));
        
        // IED - Initial Exchange
        var iedDate = DateOnly.FromDateTime(valuationStart);
        AddEvent(eventsByDate, iedDate, new ContractEvent
        {
            ContractId = contractId,
            ContractType = "ANN",
            EventType = "IED",
            Payoff = -(decimal)notional,
            PresentValue = -(decimal)notional,
            Currency = "USD"
        });
        
        // PR - Principal Redemption and IP - Interest Payment (combined in annuity)
        var currentDate = valuationStart.AddMonths(3);
        double remainingPrincipal = notional;
        int period = 1;
        
        while (currentDate <= valuationEnd && period <= periods)
        {
            double interest = remainingPrincipal * (rate / 4);
            double principal = payment - interest;
            remainingPrincipal -= principal;
            
            double discountFactor = Math.Pow(1 + rate, -((currentDate - valuationStart).TotalDays / 365.0));
            
            // Interest payment event
            AddEvent(eventsByDate, DateOnly.FromDateTime(currentDate), new ContractEvent
            {
                ContractId = contractId,
                ContractType = "ANN",
                EventType = "IP",
                Payoff = (decimal)interest,
                PresentValue = (decimal)(interest * discountFactor),
                Currency = "USD"
            });
            
            // Principal redemption event
            AddEvent(eventsByDate, DateOnly.FromDateTime(currentDate), new ContractEvent
            {
                ContractId = contractId,
                ContractType = "ANN",
                EventType = "PR",
                Payoff = (decimal)principal,
                PresentValue = (decimal)(principal * discountFactor),
                Currency = "USD"
            });
            
            currentDate = currentDate.AddMonths(3);
            period++;
        }
    }

    private void AddEvent(
        Dictionary<DateOnly, List<ContractEvent>> eventsByDate,
        DateOnly date,
        ContractEvent contractEvent)
    {
        if (!eventsByDate.ContainsKey(date))
        {
            eventsByDate[date] = new List<ContractEvent>();
        }
        eventsByDate[date].Add(contractEvent);
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
    public List<DayEventValue> DayEventValues { get; set; } = new();
}

/// <summary>
/// Represents a day with all its events and calculated values
/// </summary>
public class DayEventValue
{
    public DateOnly Date { get; set; }
    public List<ContractEvent> Events { get; set; } = new();
    public decimal TotalPayoff { get; set; }
    public decimal TotalPresentValue { get; set; }
}

/// <summary>
/// Represents a contract event with its calculated values
/// </summary>
public class ContractEvent
{
    public string ContractId { get; set; } = "";
    public string ContractType { get; set; } = "";
    public string EventType { get; set; } = "";
    public decimal Payoff { get; set; }
    public decimal PresentValue { get; set; }
    public string Currency { get; set; } = "";
}

/// <summary>
/// Progress update information for valuation
/// </summary>
public class ValuationProgress
{
    public string Stage { get; set; } = "";
    public int ProcessedContracts { get; set; }
    public int TotalContracts { get; set; }
    public int ProcessedScenarios { get; set; }
    public int TotalScenarios { get; set; }
    public double PercentComplete { get; set; }
    public string Message { get; set; } = "";
}
