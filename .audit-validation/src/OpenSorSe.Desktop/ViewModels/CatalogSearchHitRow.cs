using OpenSorSe.Application.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Represents one bounded deterministic metadata match from an application-owned saved catalog snapshot.
/// </summary>
public sealed record CatalogSearchHitRow(
    string CatalogEntryId,
    DateTimeOffset SavedAtUtc,
    string? CatalogDisplayName,
    string FileId,
    string FileName,
    string FullPath,
    string Classification,
    int MatchScore,
    string MatchExplanation)
{
    /// <summary>Gets a concise historical snapshot label for the search results surface.</summary>
    public string SnapshotLabel => string.IsNullOrWhiteSpace(CatalogDisplayName)
        ? $"Unnamed snapshot - saved {SavedAtUtc:u}"
        : $"{CatalogDisplayName} - saved {SavedAtUtc:u}";

    /// <summary>Creates a display-safe row from an existing result file and deterministic match information.</summary>
    public static CatalogSearchHitRow Create(
        string catalogEntryId,
        DateTimeOffset savedAtUtc,
        string? catalogDisplayName,
        ResultFile file,
        ResultSearchMatch match)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogEntryId);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(match);
        return new CatalogSearchHitRow(
            catalogEntryId,
            savedAtUtc,
            catalogDisplayName,
            file.Id,
            file.DisplayFileName,
            file.FullPath,
            file.ClassificationDisplay,
            match.Score,
            match.Explanation);
    }
}
