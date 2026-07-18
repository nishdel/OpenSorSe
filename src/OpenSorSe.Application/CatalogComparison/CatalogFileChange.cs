using OpenSorSe.Application.Models;

namespace OpenSorSe.Application.CatalogComparison;

/// <summary>Represents one immutable stored-path comparison without any live filesystem state.</summary>
public sealed record CatalogFileChange(
    string PathIdentity,
    CatalogComparisonChangeKind Kind,
    ResultFile? BaselineFile,
    ResultFile? CurrentFile,
    IReadOnlyList<string> BaselineTags,
    IReadOnlyList<string> CurrentTags,
    IReadOnlyList<string> ChangedFields);
