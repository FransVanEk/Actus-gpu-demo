using System.Windows;
using System.Windows.Controls;

namespace ActusDesk.UIKit.Controls;

/// <summary>
/// Reusable folder picker control
/// </summary>
public class FolderPicker : Control
{
    public static readonly DependencyProperty SelectedPathProperty =
        DependencyProperty.Register(nameof(SelectedPath), typeof(string), typeof(FolderPicker));

    public string SelectedPath
    {
        get => (string)GetValue(SelectedPathProperty);
        set => SetValue(SelectedPathProperty, value);
    }

    static FolderPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FolderPicker),
            new FrameworkPropertyMetadata(typeof(FolderPicker)));
    }
}

/// <summary>
/// GPU usage indicator
/// </summary>
public class GpuUsageBar : Control
{
    public static readonly DependencyProperty UsagePercentProperty =
        DependencyProperty.Register(nameof(UsagePercent), typeof(double), typeof(GpuUsageBar));

    public double UsagePercent
    {
        get => (double)GetValue(UsagePercentProperty);
        set => SetValue(UsagePercentProperty, value);
    }

    static GpuUsageBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GpuUsageBar),
            new FrameworkPropertyMetadata(typeof(GpuUsageBar)));
    }
}
