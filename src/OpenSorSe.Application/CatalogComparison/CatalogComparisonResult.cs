namespace OpenSorSe.Application.CatalogComparison;

/// <summary>Contains one complete immutable comparison of two application-owned catalog entries.</summary>
public sealed record CatalogComparisonResult(
    string BaselineEntryId,
    string? BaselineDisplayName,
    DateTimeOffset BaselineSavedAtUtc,
    string CurrentEntryId,
    string? CurrentDisplayName,
    DateTimeOffset CurrentSavedAtUtc,
    CatalogScopeMatch ScopeMatch,
    CatalogComparisonStatistics Statistics,
    IReadOnlyList<CatalogFileChange> Changes);
