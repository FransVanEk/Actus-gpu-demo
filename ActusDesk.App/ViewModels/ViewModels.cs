using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActusDesk.Engine.Services;
using ActusDesk.Engine.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Collections.ObjectModel;

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

    public WorkspaceViewModel(ContractsService contractsService, ILogger<WorkspaceViewModel> logger)
    {
        _contractsService = contractsService;
        _logger = logger;
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
            ContractCount = _contractsService.ContractCount;
            StatusMessage = $"Loaded {ContractCount} contracts successfully";
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
            _logger.LogInformation("Generating mock contracts");

            await _contractsService.LoadMockContractsAsync(100000, seed: 42);
            ContractCount = _contractsService.ContractCount;
            StatusMessage = $"Generated {ContractCount} mock contracts successfully";
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
}

public partial class PortfolioViewModel : ObservableObject
{
}

public partial class ScenariosViewModel : ObservableObject
{
    private readonly ScenarioService _scenarioService;
    private readonly ILogger<ScenariosViewModel> _logger;

    [ObservableProperty]
    private string _scenarioFilePath = "data/tests/scenarios.json";

    [ObservableProperty]
    private ObservableCollection<ScenarioDefinition> _scenarios = new();

    [ObservableProperty]
    private ScenarioDefinition? _selectedScenario;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isLoading = false;

    public ScenariosViewModel(ScenarioService scenarioService, ILogger<ScenariosViewModel> logger)
    {
        _scenarioService = scenarioService;
        _logger = logger;
    }

    [RelayCommand]
    private void BrowseScenarios()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Scenario File",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "tests")
        };

        if (dialog.ShowDialog() == true)
        {
            ScenarioFilePath = dialog.FileName;
            _logger.LogInformation("Selected scenario file: {Path}", ScenarioFilePath);
        }
    }

    [RelayCommand]
    private async Task LoadScenariosAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading scenarios...";
            _logger.LogInformation("Loading scenarios from: {Path}", ScenarioFilePath);

            if (!File.Exists(ScenarioFilePath))
            {
                StatusMessage = $"Error: File not found: {ScenarioFilePath}";
                _logger.LogError("File not found: {Path}", ScenarioFilePath);
                return;
            }

            await _scenarioService.LoadScenariosAsync(ScenarioFilePath);
            
            Scenarios.Clear();
            foreach (var scenario in _scenarioService.Scenarios)
            {
                Scenarios.Add(scenario);
            }
            
            StatusMessage = $"Loaded {Scenarios.Count} scenarios successfully";
            _logger.LogInformation("Successfully loaded {Count} scenarios", Scenarios.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading scenarios: {ex.Message}";
            _logger.LogError(ex, "Error loading scenarios");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveScenariosAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Saving scenarios...";
            _logger.LogInformation("Saving scenarios to: {Path}", ScenarioFilePath);

            await _scenarioService.SaveScenariosAsync(ScenarioFilePath);
            StatusMessage = $"Saved {Scenarios.Count} scenarios successfully";
            _logger.LogInformation("Successfully saved {Count} scenarios", Scenarios.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving scenarios: {ex.Message}";
            _logger.LogError(ex, "Error saving scenarios");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AddScenario()
    {
        var newScenario = new ScenarioDefinition
        {
            Name = $"New Scenario {Scenarios.Count + 1}",
            Description = "New scenario description"
        };
        
        _scenarioService.AddScenario(newScenario);
        Scenarios.Add(newScenario);
        SelectedScenario = newScenario;
        StatusMessage = $"Added new scenario: {newScenario.Name}";
    }

    [RelayCommand]
    private void RemoveScenario()
    {
        if (SelectedScenario != null)
        {
            var name = SelectedScenario.Name;
            _scenarioService.RemoveScenario(name);
            Scenarios.Remove(SelectedScenario);
            SelectedScenario = null;
            StatusMessage = $"Removed scenario: {name}";
        }
    }

    [RelayCommand]
    private void AddRateShockEvent()
    {
        if (SelectedScenario != null)
        {
            var rateEvent = new RateShockEvent
            {
                EventType = "RateShock",
                ValueBps = 50,
                ShockType = "parallel"
            };
            SelectedScenario.Events.Add(rateEvent);
            StatusMessage = "Added rate shock event";
            OnPropertyChanged(nameof(SelectedScenario));
        }
    }

    [RelayCommand]
    private void AddValueAdjustmentEvent()
    {
        if (SelectedScenario != null)
        {
            var valueEvent = new ValueAdjustmentEvent
            {
                EventType = "ValueAdjustment",
                PercentageChange = -10,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6))
            };
            SelectedScenario.Events.Add(valueEvent);
            StatusMessage = "Added value adjustment event";
            OnPropertyChanged(nameof(SelectedScenario));
        }
    }
}

public partial class ReportingViewModel : ObservableObject
{
}

public partial class RunConsoleViewModel : ObservableObject
{
}
