namespace OpenSorSe.Application.Catalog;

/// <summary>
/// Provides bounded persistence for completed display-safe result snapshots in application-owned storage.
/// </summary>
public interface IResultsCatalogStore
{
    /// <summary>Lists available catalog entries from newest to oldest without loading them into the explorer.</summary>
    Task<IReadOnlyList<CatalogEntrySummary>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Loads the complete entry with the supplied catalog identifier, or null when it is absent.</summary>
    Task<CatalogEntry?> LoadAsync(string entryId, CancellationToken cancellationToken);

    /// <summary>Saves or replaces a complete validated catalog entry and applies bounded retention.</summary>
    Task<CatalogEntrySummary> SaveAsync(CatalogEntry entry, CancellationToken cancellationToken);

    /// <summary>Removes one saved entry from application-owned catalog storage when it exists.</summary>
    Task<bool> RemoveAsync(string entryId, CancellationToken cancellationToken);

    /// <summary>Removes all application-owned catalog storage after explicit caller authorization.</summary>
    Task ClearAsync(CancellationToken cancellationToken);
}
