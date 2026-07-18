namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Describes the deterministic local matching signals used to rank one result row.
/// </summary>
public sealed record ResultSearchMatch(int Score, string Explanation);
