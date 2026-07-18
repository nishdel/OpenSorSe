namespace OpenSorSe.Application.CatalogSearch;

/// <summary>
/// Represents one named deterministic catalog query without retaining its hits.
/// </summary>
public sealed record SavedCatalogSearch(
    string Id,
    string Name,
    string QueryText,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
