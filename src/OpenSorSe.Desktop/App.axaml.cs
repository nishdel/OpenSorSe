using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OpenSorSe.Core.DependencyInjection;
using OpenSorSe.Core.Lifecycle;
using OpenSorSe.Desktop.ViewModels;
using OpenSorSe.Desktop.Views;
using OpenSorSe.Scanner;
using OpenSorSe.Rules;
using OpenSorSe.Executor;
using OpenSorSe.Application;

namespace OpenSorSe.Desktop;

/// <summary>
/// Provides the Avalonia application entry point and desktop lifetime configuration.
/// </summary>
public partial class App : Avalonia.Application
{
    private ServiceProvider? _serviceProvider;
    private IApplicationHost? _applicationHost;

    /// <summary>
    /// Loads the application's XAML resources.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Creates the initial desktop window after Avalonia has initialized the framework.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _serviceProvider = CreateServiceProvider();
            _applicationHost = _serviceProvider.GetRequiredService<IApplicationHost>();
            _applicationHost.InitializeAsync().GetAwaiter().GetResult();
            desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenSorSe",
            "settings.json");
        var services = new ServiceCollection();
        services.AddOpenSorSeCore(new OpenSorSeCoreOptions { ConfigurationFilePath = settingsPath });
        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<IFileMetadataReader, FileMetadataReader>();
        services.AddSingleton<IFileHasher, FileHasher>();
        services.AddSingleton<IFileClassifier, FileClassifier>();
        services.AddSingleton<IDuplicateDetector, DuplicateDetector>();
        services.AddSingleton<IRuleEngine, RuleEngine>();
        services.AddSingleton<IActionPlanner, ActionPlanner>();
        services.AddSingleton<IConflictResolver, ConflictResolver>();
        services.AddSingleton<IActionExecutor, ActionExecutor>();
        services.AddSingleton<IUndoEngine, UndoEngine>();
        services.AddSingleton<IProcessingOrchestrator, ProcessingOrchestrator>();
        services.AddSingleton<IProcessingSessionManager, ProcessingSessionManager>();
        services.AddSingleton<IApplicationController, ApplicationController>();
        services.AddSingleton<IResultsSnapshotProjector, ResultsSnapshotProjector>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs eventArgs)
    {
        _applicationHost?.ShutdownAsync().GetAwaiter().GetResult();
        _serviceProvider?.Dispose();
    }
}
