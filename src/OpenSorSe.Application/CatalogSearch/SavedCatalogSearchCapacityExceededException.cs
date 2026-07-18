namespace OpenSorSe.Application.CatalogSearch;

/// <summary>
/// Indicates that saving another distinct catalog search would exceed the explicit preset bound.
/// </summary>
public sealed class SavedCatalogSearchCapacityExceededException : InvalidOperationException
{
    /// <summary>Initializes the exception with a user-safe capacity message.</summary>
    public SavedCatalogSearchCapacityExceededException(string message)
        : base(message)
    {
    }
}
