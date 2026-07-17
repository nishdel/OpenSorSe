namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Contains the read-only dashboard totals available during the current application session.
/// </summary>
/// <param name="FilesScanned">The number of files scanned in the current session.</param>
/// <param name="FoldersDiscovered">The number of folders discovered by the latest completed scan.</param>
/// <param name="ExactDuplicates">The number of files identified as exact duplicates by the latest completed scan.</param>
/// <param name="Warnings">The number of recoverable warnings reported for the latest completed scan.</param>
public sealed record DashboardStatistics(
    long FilesScanned,
    long FoldersDiscovered,
    long ExactDuplicates,
    long Warnings)
{
    /// <summary>
    /// Gets the empty dashboard totals used before a scan completes in the current application session.
    /// </summary>
    public static DashboardStatistics Empty { get; } = new(0, 0, 0, 0);
}
