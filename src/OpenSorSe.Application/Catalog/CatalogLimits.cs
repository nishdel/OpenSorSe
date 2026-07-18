namespace OpenSorSe.Application.Catalog;

/// <summary>
/// Defines fixed v0.4 bounds for application-owned local catalog persistence.
/// </summary>
public static class CatalogLimits
{
    /// <summary>Gets the maximum complete snapshots retained in the local catalog.</summary>
    public const int MaximumEntryCount = 10;

    /// <summary>Gets the maximum files retained by one complete saved snapshot.</summary>
    public const int MaximumFilesPerEntry = 2_000;

    /// <summary>Gets the maximum user-controlled display-name length.</summary>
    public const int MaximumDisplayNameLength = 80;

    /// <summary>Gets the maximum historical source roots retained for one snapshot.</summary>
    public const int MaximumSourceRootCount = 32;

    /// <summary>Gets the maximum length of one historical source-root string.</summary>
    public const int MaximumSourceRootLength = 2_048;

    /// <summary>Gets the maximum encoded size of the complete application-owned catalog file.</summary>
    public const long MaximumCatalogFileBytes = 128L * 1024 * 1024;

    /// <summary>Gets the maximum stored filesystem-path length.</summary>
    public const int MaximumStoredPathLength = 32_768;

    /// <summary>Gets the maximum stored display/diagnostic text length.</summary>
    public const int MaximumDisplayTextLength = 2_048;

    /// <summary>Gets the maximum stored opaque identifier length.</summary>
    public const int MaximumIdentifierLength = 512;
}
