using System.Windows;
using System.Windows.Data;
using System.Globalization;
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

// Converters
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && !boolValue;
    }
}

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolValue && boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ZeroToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is int intValue && intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
