namespace OpenSorSe.Application.Catalog;

/// <summary>
/// Represents display-safe metadata for one saved results snapshot without loading it into the Results Explorer.
/// </summary>
public sealed record CatalogEntrySummary(
    string Id,
    DateTimeOffset SavedAtUtc,
    long FileCount,
    long DirectoryCount,
    long WarningCount,
    long ExactDuplicateGroupCount)
{
    /// <summary>Gets the optional user-controlled catalog name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the immutable historical scan roots, or an empty list when legacy scope is unknown.</summary>
    public IReadOnlyList<string> SourceRoots { get; init; } = Array.Empty<string>();
}
