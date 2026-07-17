namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Selects the exact-duplicate state shown by the results explorer.
/// </summary>
public enum ResultDuplicateFilter
{
    /// <summary>Shows every result regardless of duplicate state.</summary>
    All,

    /// <summary>Shows files that belong to an existing exact duplicate group.</summary>
    ExactDuplicatesOnly,

    /// <summary>Shows files with one supported, non-shared hash.</summary>
    UniqueOnly,

    /// <summary>Shows files for which duplicate state was unavailable.</summary>
    UnknownOrUnavailable,
}
