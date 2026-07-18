namespace OpenSorSe.Desktop.ViewModels;

/// <summary>Identifies the historical change kinds presented by the comparison surface.</summary>
public enum CatalogComparisonFilter
{
    /// <summary>Shows added, removed, and modified rows.</summary>
    Changed,

    /// <summary>Shows only added rows.</summary>
    Added,

    /// <summary>Shows only removed rows.</summary>
    Removed,

    /// <summary>Shows only modified rows.</summary>
    Modified,

    /// <summary>Shows only unchanged rows.</summary>
    Unchanged,

    /// <summary>Shows every comparison row.</summary>
    All,
}
