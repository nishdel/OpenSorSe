using OpenSorSe.Application.Catalog;

namespace OpenSorSe.Application.CatalogComparison;

/// <summary>Compares two already-loaded historical catalog entries without performing I/O.</summary>
public interface ICatalogComparisonService
{
    /// <summary>Produces a deterministic bounded stored-metadata comparison.</summary>
    CatalogComparisonResult Compare(CatalogEntry baseline, CatalogEntry current, CancellationToken cancellationToken);
}
