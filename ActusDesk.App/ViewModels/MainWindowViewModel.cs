using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ActusDesk.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private string _title = "ActusDesk - ACTUS Contract Valuation";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public WorkspaceViewModel WorkspaceViewModel { get; }
    public PortfolioViewModel PortfolioViewModel { get; }
    public ScenariosViewModel ScenariosViewModel { get; }
    public RunConsoleViewModel RunConsoleViewModel { get; }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        WorkspaceViewModel workspaceViewModel,
        PortfolioViewModel portfolioViewModel,
        ScenariosViewModel scenariosViewModel,
        RunConsoleViewModel runConsoleViewModel)
    {
        _logger = logger;
        WorkspaceViewModel = workspaceViewModel;
        PortfolioViewModel = portfolioViewModel;
        ScenariosViewModel = scenariosViewModel;
        RunConsoleViewModel = runConsoleViewModel;
        _logger.LogInformation("MainWindowViewModel initialized");
    }
}
