using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ActusDesk.App.ViewModels;
using ActusDesk.App.Views;
using ActusDesk.Engine.Services;
using ActusDesk.Gpu;
using ActusDesk.IO;

namespace ActusDesk.App;

/// <summary>
/// Main application entry point for ActusDesk WPF application
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // GPU Context (singleton - keep for entire app lifetime)
        services.AddSingleton<GpuContext>();

        // GPU Providers
        services.AddSingleton<IPamGpuProvider, PamGpuProvider>();
        services.AddSingleton<IAnnGpuProvider, AnnGpuProvider>();

        // Services
        services.AddSingleton<ContractsService>();
        services.AddSingleton<ScenarioService>();
        services.AddSingleton<ValuationService>();
        services.AddSingleton<ReportingService>();

        // IO
        services.AddSingleton<IContractLoader, JsonContractLoader>();
        services.AddSingleton<ICacheService, BinaryCacheService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<WorkspaceViewModel>();
        services.AddTransient<PortfolioViewModel>();
        services.AddTransient<ScenariosViewModel>();
        services.AddTransient<ReportingViewModel>();
        services.AddTransient<RunConsoleViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
