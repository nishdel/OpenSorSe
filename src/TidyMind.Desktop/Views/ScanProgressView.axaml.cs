using Avalonia.Controls;
using Avalonia.Interactivity;
using TidyMind.Desktop.ViewModels;

namespace TidyMind.Desktop.Views;

/// <summary>
/// Displays scanner progress and delegates cancellation to its presentation model.
/// </summary>
public partial class ScanProgressView : UserControl
{
    /// <summary>
    /// Initializes the scan-progress view.
    /// </summary>
    public ScanProgressView()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs arguments)
    {
        if (DataContext is ScanProgressViewModel viewModel)
        {
            viewModel.RequestCancellation();
        }
    }
}
