namespace OpenSorSe.Application.CatalogSearch;

/// <summary>
/// Provides bounded persistence for named deterministic catalog queries.
/// </summary>
public interface ISavedCatalogSearchStore
{
    /// <summary>Lists saved searches from newest update to oldest.</summary>
    Task<IReadOnlyList<SavedCatalogSearch>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Saves or replaces one validated query preset.</summary>
    Task<SavedCatalogSearch> SaveAsync(SavedCatalogSearch search, CancellationToken cancellationToken);

    /// <summary>Removes one saved query by opaque identifier when it exists.</summary>
    Task<bool> RemoveAsync(string searchId, CancellationToken cancellationToken);

    /// <summary>Clears only the configured saved-query file after explicit caller authorization.</summary>
    Task ClearAsync(CancellationToken cancellationToken);
}
