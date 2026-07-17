namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Contains the read-only dashboard totals available during the current application session.
/// </summary>
/// <param name="FilesScanned">The number of files scanned in the current session.</param>
/// <param name="FilesOrganized">The number of files organized in the current session.</param>
/// <param name="DuplicateFiles">The number of duplicate files identified in the current session.</param>
/// <param name="StorageSavedBytes">The number of bytes saved in the current session.</param>
public sealed record DashboardStatistics(
    long FilesScanned,
    long FilesOrganized,
    long DuplicateFiles,
    long StorageSavedBytes)
{
    /// <summary>
    /// Gets the empty dashboard totals used before later session features publish activity.
    /// </summary>
    public static DashboardStatistics Empty { get; } = new(0, 0, 0, 0);
}
