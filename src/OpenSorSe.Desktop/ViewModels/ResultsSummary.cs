namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Provides read-only aggregate values displayed by the results-review page.
/// </summary>
/// <param name="FilesScanned">The number of files supplied for review.</param>
/// <param name="PlannedOperations">The number of accepted planned operations.</param>
/// <param name="DuplicateGroups">The number of distinct duplicate groups represented by the supplied files.</param>
/// <param name="Warnings">The number of recoverable planner or conflict warnings.</param>
public sealed record ResultsSummary(long FilesScanned, long PlannedOperations, long DuplicateGroups, long Warnings)
{
    /// <summary>
    /// Gets an empty review summary.
    /// </summary>
    public static ResultsSummary Empty { get; } = new(0, 0, 0, 0);
}
