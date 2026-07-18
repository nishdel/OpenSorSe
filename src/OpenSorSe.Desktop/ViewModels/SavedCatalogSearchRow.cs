using OpenSorSe.Application.CatalogSearch;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents one bounded named catalog query preset.
/// </summary>
public sealed record SavedCatalogSearchRow(
    string Id,
    string Name,
    string QueryText,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>Creates a display row from an application-owned saved query.</summary>
    public static SavedCatalogSearchRow FromModel(SavedCatalogSearch search) => new(
        search.Id,
        search.Name,
        search.QueryText,
        search.UpdatedAtUtc);
}
