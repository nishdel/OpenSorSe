namespace OpenSorSe.Application.CatalogSearch;

/// <summary>
/// Defines fixed bounds for application-owned saved catalog query presets.
/// </summary>
public static class SavedCatalogSearchLimits
{
    /// <summary>Gets the maximum saved query presets retained.</summary>
    public const int MaximumSearchCount = 25;

    /// <summary>Gets the maximum display-name length.</summary>
    public const int MaximumNameLength = 80;

    /// <summary>Gets the maximum query-text length.</summary>
    public const int MaximumQueryLength = 512;

    /// <summary>Gets the maximum encoded size of the complete saved-search file.</summary>
    public const long MaximumStoreFileBytes = 256L * 1024;
}
