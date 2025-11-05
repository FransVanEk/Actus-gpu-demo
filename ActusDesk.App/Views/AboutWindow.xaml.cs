using System.Windows;
using ActusDesk.Gpu;
using Microsoft.Extensions.Logging;

namespace ActusDesk.App.Views;

public partial class AboutWindow : Window
{
    public AboutWindow(GpuContext? gpuContext = null, ILogger<AboutWindow>? logger = null)
    {
        InitializeComponent();
        
        // Populate GPU information
        if (gpuContext != null)
        {
            try
            {
                var accelerator = gpuContext.Accelerator;
                GpuNameText.Text = accelerator.Name;
                GpuMemoryText.Text = $"{accelerator.MemorySize / (1024 * 1024):N0} MB";
                GpuTypeText.Text = accelerator.AcceleratorType.ToString();
                GpuMaxThreadsText.Text = accelerator.MaxNumThreadsPerGroup.ToString();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to get GPU information");
                GpuNameText.Text = "Unable to query GPU";
            }
        }
        else
        {
            GpuNameText.Text = "GPU not initialized";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
