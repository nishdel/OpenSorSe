using OpenSorSe.Application.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Contains one bounded page of immutable result rows.
/// </summary>
public sealed record ResultsPage(
    IReadOnlyList<ResultFile> Items,
    int PageIndex,
    int PageSize,
    int TotalItemCount,
    int TotalPageCount)
{
    /// <summary>Gets an empty first page with the standard page size.</summary>
    public static ResultsPage Empty { get; } = new(Array.AsReadOnly(Array.Empty<ResultFile>()), 0, 200, 0, 0);
}
