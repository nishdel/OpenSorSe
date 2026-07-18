namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Identifies a deterministic sort field for the results explorer.
/// </summary>
public enum ResultsSortField
{
    /// <summary>Sorts text-query matches by deterministic relevance and stable path tie-breakers.</summary>
    Relevance,

    /// <summary>Sorts by display file name.</summary>
    Name,

    /// <summary>Sorts by full path.</summary>
    Path,

    /// <summary>Sorts by normalized extension.</summary>
    Extension,

    /// <summary>Sorts by available size.</summary>
    Size,

    /// <summary>Sorts by available modified time.</summary>
    ModifiedTime,

    /// <summary>Sorts by exact-duplicate state.</summary>
    DuplicateState,
}
