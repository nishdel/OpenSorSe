using OpenSorSe.Application.Catalog;

namespace OpenSorSe.Application.CatalogComparison;

/// <summary>Defines fixed bounds for historical catalog comparison.</summary>
public static class CatalogComparisonLimits
{
    /// <summary>Gets the maximum stored files accepted from either snapshot.</summary>
    public const int MaximumFilesPerSnapshot = CatalogLimits.MaximumFilesPerEntry;

    /// <summary>Gets the maximum union of changes produced by one comparison.</summary>
    public const int MaximumChangeCount = MaximumFilesPerSnapshot * 2;
}
