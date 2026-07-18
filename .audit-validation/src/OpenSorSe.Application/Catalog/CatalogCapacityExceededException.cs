namespace OpenSorSe.Application.Catalog;

/// <summary>
/// Represents a bounded catalog-storage request that cannot be retained without truncating a completed snapshot.
/// </summary>
public sealed class CatalogCapacityExceededException : InvalidOperationException
{
    /// <summary>
    /// Initializes a capacity exception with a user-safe reason.
    /// </summary>
    /// <param name="message">The bounded-storage explanation.</param>
    public CatalogCapacityExceededException(string message)
        : base(message)
    {
    }
}
