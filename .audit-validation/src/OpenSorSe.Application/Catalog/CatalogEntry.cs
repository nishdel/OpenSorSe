using OpenSorSe.Application.Models;

namespace OpenSorSe.Application.Catalog;

/// <summary>
/// Represents one complete, application-owned saved results snapshot and its accepted non-deterministic tags.
/// </summary>
public sealed record CatalogEntry(
    string Id,
    DateTimeOffset SavedAtUtc,
    ResultsSnapshot Snapshot,
    IReadOnlyList<TagAssociation> AcceptedTags)
{
    /// <summary>Gets an optional user-controlled name stored only with this catalog entry.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the historical scan roots captured from the completed request without live validation.</summary>
    public IReadOnlyList<string> SourceRoots { get; init; } = Array.Empty<string>();
}
