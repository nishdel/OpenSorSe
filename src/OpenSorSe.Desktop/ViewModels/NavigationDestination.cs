namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Identifies a top-level destination hosted by the desktop application shell.
/// </summary>
public enum NavigationDestination
{
    /// <summary>Displays the application overview.</summary>
    Dashboard,

    /// <summary>Displays scan-related controls.</summary>
    Scan,

    /// <summary>Displays processed file results.</summary>
    Results,

    /// <summary>Displays rule-management controls.</summary>
    Rules,

    /// <summary>Displays application settings.</summary>
    Settings,

    /// <summary>Displays local logging health and aggregate diagnostics.</summary>
    Logs,

    /// <summary>Displays explicit undo-record sessions supplied by the application controller.</summary>
    History,

    /// <summary>Displays static application metadata and external-resource requests.</summary>
    About,
}
