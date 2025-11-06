using System.Windows;
using ActusDesk.App.ViewModels;
using ActusDesk.Gpu;
using Microsoft.Extensions.Logging;

namespace ActusDesk.App.Views;

public partial class MainWindow : Window
{
    private readonly GpuContext _gpuContext;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(MainWindowViewModel viewModel, GpuContext gpuContext, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        DataContext = viewModel;
        _gpuContext = gpuContext;
        _logger = logger;
    }

    private void OpenWorkspace_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement workspace loading
        _logger.LogInformation("Open workspace clicked");
    }

    private void SaveWorkspace_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement workspace saving
        _logger.LogInformation("Save workspace clicked");
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShowGpuStatus_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Show GPU status dialog
        _logger.LogInformation("Show GPU status clicked");
        MessageBox.Show($"GPU: {_gpuContext.Accelerator.Name}\nMemory: {_gpuContext.Accelerator.MemorySize / (1024 * 1024):N0} MB", 
                       "GPU Status", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowCacheManager_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Show cache manager
        _logger.LogInformation("Show cache manager clicked");
    }

    private void ShowDocs_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open documentation
        _logger.LogInformation("Show documentation clicked");
    }

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow(_gpuContext);
        aboutWindow.Owner = this;
        aboutWindow.ShowDialog();
    }
}
