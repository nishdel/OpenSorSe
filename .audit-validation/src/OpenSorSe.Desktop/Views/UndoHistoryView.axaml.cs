using Avalonia.Controls;

namespace OpenSorSe.Desktop.Views;

/// <summary>
/// Displays explicit undo-record sessions without executing them.
/// </summary>
public partial class UndoHistoryView : UserControl
{
    /// <summary>
    /// Initializes the undo-history view.
    /// </summary>
    public UndoHistoryView()
    {
        InitializeComponent();
    }
}
