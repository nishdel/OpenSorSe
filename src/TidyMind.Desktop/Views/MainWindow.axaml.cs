using Avalonia.Controls;
using TidyMind.Desktop.ViewModels;

namespace TidyMind.Desktop.Views;

/// <summary>
/// Hosts the initial TidyMind desktop application shell.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initialises the window for Avalonia's XAML runtime loader and designer.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initialises the window with its runtime presentation model.
    /// </summary>
    /// <param name="viewModel">The view model supplied by application composition.</param>
    public MainWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
