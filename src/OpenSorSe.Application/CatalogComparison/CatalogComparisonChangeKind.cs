namespace OpenSorSe.Application.CatalogComparison;

/// <summary>Identifies how one stored path differs between two historical snapshots.</summary>
public enum CatalogComparisonChangeKind
{
    /// <summary>The path exists only in the current snapshot.</summary>
    Added,

    /// <summary>The path exists only in the baseline snapshot.</summary>
    Removed,

    /// <summary>The path exists in both snapshots and one or more stored metadata fields differ.</summary>
    Modified,

    /// <summary>The path and compared stored metadata are equal in both snapshots.</summary>
    Unchanged,
}
