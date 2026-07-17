namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Contains a normalized query and its bounded evaluation result.
/// </summary>
public sealed record ResultsQueryResult(ResultsQuery Query, ResultsPage Page);
