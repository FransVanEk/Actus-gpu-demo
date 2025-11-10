using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ActusDesk.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly Gpu.GpuContext _gpuContext;
    private System.Timers.Timer? _gpuUpdateTimer;

    [ObservableProperty]
    private string _title = "ActusDesk - ACTUS Contract Valuation";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _gpuName = "Initializing...";

    [ObservableProperty]
    private string _gpuMemoryStatus = "0 MB / 0 MB";

    [ObservableProperty]
    private double _gpuUtilizationPercent = 0.0;

    public WorkspaceViewModel WorkspaceViewModel { get; }
    public PortfolioViewModel PortfolioViewModel { get; }
    public ScenariosViewModel ScenariosViewModel { get; }
    public RunConsoleViewModel RunConsoleViewModel { get; }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        Gpu.GpuContext gpuContext,
        WorkspaceViewModel workspaceViewModel,
        PortfolioViewModel portfolioViewModel,
        ScenariosViewModel scenariosViewModel,
        RunConsoleViewModel runConsoleViewModel)
    {
        _logger = logger;
        _gpuContext = gpuContext;
        WorkspaceViewModel = workspaceViewModel;
        PortfolioViewModel = portfolioViewModel;
        ScenariosViewModel = scenariosViewModel;
        RunConsoleViewModel = runConsoleViewModel;
        _logger.LogInformation("MainWindowViewModel initialized");

        // Initialize GPU info
        UpdateGpuInfo();

        // Start periodic GPU monitoring (every 500ms)
        _gpuUpdateTimer = new System.Timers.Timer(500);
        _gpuUpdateTimer.Elapsed += (s, e) => UpdateGpuInfo();
        _gpuUpdateTimer.Start();
    }

    private void UpdateGpuInfo()
    {
        try
        {
            GpuName = _gpuContext.GpuName;
            
            var totalMemoryMB = _gpuContext.TotalMemoryBytes / (1024.0 * 1024.0);
            var allocatedMemoryMB = _gpuContext.AllocatedMemoryBytes / (1024.0 * 1024.0);
            
            GpuMemoryStatus = $"{allocatedMemoryMB:F0} MB / {totalMemoryMB:F0} MB";
            GpuUtilizationPercent = _gpuContext.MemoryUtilizationPercent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GPU info");
        }
    }

    public void Dispose()
    {
        _gpuUpdateTimer?.Stop();
        _gpuUpdateTimer?.Dispose();
    }
}
