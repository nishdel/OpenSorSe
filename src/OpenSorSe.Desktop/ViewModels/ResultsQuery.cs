using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Describes one local, in-memory result-explorer query.
/// </summary>
public sealed record ResultsQuery(
    string? Text,
    ResultDuplicateFilter DuplicateFilter,
    string? Extension,
    FileCategory? Category,
    ResultPlannedOperationFilter PlannedOperationFilter,
    ResultsSortField SortField,
    SortDirection SortDirection,
    int PageIndex,
    int PageSize,
    string? DuplicateGroupId = null)
{
    /// <summary>Gets the safe default explorer query.</summary>
    public static ResultsQuery Default { get; } = new(
        null,
        ResultDuplicateFilter.All,
        null,
        null,
        ResultPlannedOperationFilter.All,
        ResultsSortField.Name,
        SortDirection.Ascending,
        0,
        200);
}
