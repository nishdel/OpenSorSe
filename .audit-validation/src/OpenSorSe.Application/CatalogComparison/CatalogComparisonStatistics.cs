namespace OpenSorSe.Application.CatalogComparison;

/// <summary>Contains aggregate counts for one complete bounded historical comparison.</summary>
public sealed record CatalogComparisonStatistics(
    int BaselineFileCount,
    int CurrentFileCount,
    int AddedCount,
    int RemovedCount,
    int ModifiedCount,
    int UnchangedCount,
    int IgnoredDuplicateRecordCount);
