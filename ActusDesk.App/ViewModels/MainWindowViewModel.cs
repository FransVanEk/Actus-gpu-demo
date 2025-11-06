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

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        WorkspaceViewModel workspaceViewModel)
    {
        _logger = logger;
        WorkspaceViewModel = workspaceViewModel;
        _logger.LogInformation("MainWindowViewModel initialized");
    }
}
