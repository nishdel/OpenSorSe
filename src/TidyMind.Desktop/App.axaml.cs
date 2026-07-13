using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TidyMind.Desktop.ViewModels;
using TidyMind.Desktop.Views;

namespace TidyMind.Desktop;

/// <summary>
/// Provides the Avalonia application entry point and desktop lifetime configuration.
/// </summary>
public partial class App : Application
{
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
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
