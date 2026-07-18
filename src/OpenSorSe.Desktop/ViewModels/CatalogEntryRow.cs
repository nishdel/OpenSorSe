using OpenSorSe.Application.Catalog;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Provides display-safe, formatted catalog metadata for one saved snapshot.
/// </summary>
public sealed record CatalogEntryRow(
    string Id,
    DateTimeOffset SavedAtUtc,
    long FileCount,
    long DirectoryCount,
    long WarningCount,
    long ExactDuplicateGroupCount,
    string? DisplayName,
    IReadOnlyList<string> SourceRoots)
{
    /// <summary>Gets the user-controlled name or an explicit fallback for unnamed entries.</summary>
    public string Title => DisplayName ?? "Unnamed snapshot";

    /// <summary>Gets concise details suitable for the saved-snapshot list.</summary>
    public string Summary => $"{FileCount} file(s), {DirectoryCount} folder(s), {WarningCount} warning(s), {ExactDuplicateGroupCount} exact duplicate group(s)";

    /// <summary>Gets the immutable historical source scope or an explicit legacy-data fallback.</summary>
    public string SourceScope => SourceRoots.Count == 0
        ? "Source scope unknown (saved before v0.8)."
        : $"Source scope: {string.Join("; ", SourceRoots.Take(3))}{(SourceRoots.Count > 3 ? $" (+{SourceRoots.Count - 3} more)" : string.Empty)}";

    /// <summary>Creates a row from persisted application-owned catalog metadata.</summary>
    public static CatalogEntryRow FromSummary(CatalogEntrySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new CatalogEntryRow(
            summary.Id,
            summary.SavedAtUtc,
            summary.FileCount,
            summary.DirectoryCount,
            summary.WarningCount,
            summary.ExactDuplicateGroupCount,
            summary.DisplayName,
            Array.AsReadOnly(summary.SourceRoots.ToArray()));
    }
}
