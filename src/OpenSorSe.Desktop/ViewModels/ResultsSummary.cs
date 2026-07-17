namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Provides read-only aggregate values displayed by the results-review page.
/// </summary>
/// <param name="FilesScanned">The number of files supplied for review.</param>
/// <param name="FoldersDiscovered">The number of folders discovered during the scan.</param>
/// <param name="PlannedOperations">The number of accepted planned operations.</param>
/// <param name="ExactDuplicates">The number of files identified as exact duplicates.</param>
/// <param name="Warnings">The number of recoverable planner or conflict warnings.</param>
public sealed record ResultsSummary(long FilesScanned, long FoldersDiscovered, long PlannedOperations, long ExactDuplicates, long Warnings)
{
    /// <summary>
    /// Gets an empty review summary.
    /// </summary>
    public static ResultsSummary Empty { get; } = new(0, 0, 0, 0, 0);
}
