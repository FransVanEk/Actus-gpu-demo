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
}

public partial class ReportingViewModel : ObservableObject
{
}

public partial class RunConsoleViewModel : ObservableObject
{
}
