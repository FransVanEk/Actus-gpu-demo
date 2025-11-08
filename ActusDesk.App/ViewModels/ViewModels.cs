using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActusDesk.Engine.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;

namespace ActusDesk.App.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    private readonly ContractsService _contractsService;
    private readonly ILogger<WorkspaceViewModel> _logger;

    [ObservableProperty]
    private string _contractsFilePath = "data/tests/actus-tests-pam.json";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private int _contractCount = 0;

    [ObservableProperty]
    private int _pamContractCount = 0;

    [ObservableProperty]
    private int _annContractCount = 0;

    [ObservableProperty]
    private int _totalMockContracts = 100000;

    [ObservableProperty]
    private double _pamPercentage = 50.0;

    [ObservableProperty]
    private double _annPercentage = 50.0;

    public WorkspaceViewModel(ContractsService contractsService, ILogger<WorkspaceViewModel> logger)
    {
        _contractsService = contractsService;
        _logger = logger;
        
        // Initialize from registry
        UpdatePercentagesFromRegistry();
    }

    partial void OnPamPercentageChanged(double value)
    {
        // Update registry
        _contractsService.ContractRegistry.UpdatePercentage("PAM", value);
        // Auto-adjust ANN to maintain 100% total
        var newAnnPercentage = 100.0 - value;
        if (Math.Abs(AnnPercentage - newAnnPercentage) > 0.01) // Avoid infinite loop
        {
            AnnPercentage = newAnnPercentage;
        }
    }

    partial void OnAnnPercentageChanged(double value)
    {
        // Update registry
        _contractsService.ContractRegistry.UpdatePercentage("ANN", value);
        // Auto-adjust PAM to maintain 100% total
        var newPamPercentage = 100.0 - value;
        if (Math.Abs(PamPercentage - newPamPercentage) > 0.01) // Avoid infinite loop
        {
            PamPercentage = newPamPercentage;
        }
    }

    private void UpdatePercentagesFromRegistry()
    {
        var normalized = _contractsService.ContractRegistry.GetNormalizedPercentages();
        if (normalized.TryGetValue("PAM", out var pamPct))
        {
            PamPercentage = pamPct;
        }
        if (normalized.TryGetValue("ANN", out var annPct))
        {
            AnnPercentage = annPct;
        }
    }

    [RelayCommand]
    private void BrowseContracts()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Contract File",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "tests")
        };

        if (dialog.ShowDialog() == true)
        {
            ContractsFilePath = dialog.FileName;
            _logger.LogInformation("Selected contract file: {Path}", ContractsFilePath);
        }
    }

    [RelayCommand]
    private async Task LoadContractsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading contracts...";
            _logger.LogInformation("Loading contracts from: {Path}", ContractsFilePath);

            if (!File.Exists(ContractsFilePath))
            {
                StatusMessage = $"Error: File not found: {ContractsFilePath}";
                _logger.LogError("File not found: {Path}", ContractsFilePath);
                return;
            }

            await _contractsService.LoadFromJsonAsync(new[] { ContractsFilePath });
            UpdateContractCounts();
            StatusMessage = $"Loaded {ContractCount} contracts successfully (PAM: {PamContractCount}, ANN: {AnnContractCount})";
            _logger.LogInformation("Successfully loaded {Count} contracts", ContractCount);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading contracts: {ex.Message}";
            _logger.LogError(ex, "Error loading contracts");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMockContractsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Generating mock contracts...";
            _logger.LogInformation("Generating mock contracts with distribution PAM: {PamPct}%, ANN: {AnnPct}%", 
                PamPercentage, AnnPercentage);

            // Load mixed PAM and ANN contracts based on registry percentages
            await _contractsService.LoadMixedMockContractsAsync(TotalMockContracts, seed: 42);
            UpdateContractCounts();
            StatusMessage = $"Generated {ContractCount} mock contracts successfully (PAM: {PamContractCount} [{PamPercentage:F1}%], ANN: {AnnContractCount} [{AnnPercentage:F1}%])";
            _logger.LogInformation("Successfully generated {Count} mock contracts", ContractCount);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating mock contracts: {ex.Message}";
            _logger.LogError(ex, "Error generating mock contracts");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateContractCounts()
    {
        ContractCount = _contractsService.ContractCount;
        PamContractCount = _contractsService.PamContractCount;
        AnnContractCount = _contractsService.AnnContractCount;
    }
}

public partial class PortfolioViewModel : ObservableObject
{
}

public partial class ScenariosViewModel : ObservableObject
{
    private readonly ScenarioService _scenarioService;
    private readonly ILogger<ScenariosViewModel> _logger;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private int _scenarioCount = 0;

    public ScenariosViewModel(ScenarioService scenarioService, ILogger<ScenariosViewModel> logger)
    {
        _scenarioService = scenarioService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadDefaultScenariosAsync()
    {
        try
        {
            StatusMessage = "Loading default scenarios...";
            _logger.LogInformation("Loading default scenarios");

            await _scenarioService.LoadDefaultScenariosAsync();
            ScenarioCount = _scenarioService.Scenarios.Count;
            StatusMessage = $"Loaded {ScenarioCount} scenarios successfully";
            _logger.LogInformation("Successfully loaded {Count} scenarios", ScenarioCount);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading scenarios: {ex.Message}";
            _logger.LogError(ex, "Error loading scenarios");
        }
    }
}

public partial class ReportingViewModel : ObservableObject
{
}

public partial class RunConsoleViewModel : ObservableObject
{
    private readonly ValuationService _valuationService;
    private readonly ILogger<RunConsoleViewModel> _logger;

    [ObservableProperty]
    private string _statusMessage = "Ready to run valuation";

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _results = "";

    public RunConsoleViewModel(ValuationService valuationService, ILogger<RunConsoleViewModel> logger)
    {
        _valuationService = valuationService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task RunValuationAsync()
    {
        try
        {
            IsRunning = true;
            StatusMessage = "Running valuation...";
            Results = "Starting valuation run...\n";
            _logger.LogInformation("Starting valuation run");

            var result = await _valuationService.RunValuationAsync();

            Results += $"\nValuation Complete!\n";
            Results += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            Results += $"Total Contracts: {result.ContractCount:N0}\n";
            Results += $"  - PAM Contracts: {result.PamContractCount:N0}\n";
            Results += $"  - ANN Contracts: {result.AnnContractCount:N0}\n";
            Results += $"Scenarios: {result.ScenarioCount}\n";
            Results += $"Valuation Period: {result.ValuationStartDate:yyyy-MM-dd} to {result.ValuationEndDate:yyyy-MM-dd}\n";
            Results += $"Duration: {result.Duration.TotalMilliseconds:N2}ms\n";
            Results += $"Throughput: {(result.ContractCount * result.ScenarioCount / result.Duration.TotalSeconds):N0} contracts/sec\n";
            Results += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            Results += $"\n{result.Message}\n";

            StatusMessage = "Valuation complete";
            _logger.LogInformation("Valuation completed successfully");
        }
        catch (Exception ex)
        {
            Results += $"\nERROR: {ex.Message}\n";
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error running valuation");
        }
        finally
        {
            IsRunning = false;
        }
    }
}
