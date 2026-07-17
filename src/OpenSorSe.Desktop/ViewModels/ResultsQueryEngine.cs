using OpenSorSe.Application.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Evaluates local, deterministic result-explorer queries without filesystem or UI dependencies.
/// </summary>
public static class ResultsQueryEngine
{
    private static readonly int[] ApprovedPageSizes = [50, 100, 200, 500];

    /// <summary>
    /// Evaluates a query against an immutable snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to filter and sort.</param>
    /// <param name="query">The user query to normalize and evaluate.</param>
    /// <param name="cancellationToken">Cancels only this local query evaluation.</param>
    /// <returns>A normalized query and a bounded page.</returns>
    public static ResultsQueryResult Evaluate(ResultsSnapshot snapshot, ResultsQuery? query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var normalizedQuery = Normalize(query);
        var filtered = new List<ResultFile>(snapshot.Files.Count);
        for (var index = 0; index < snapshot.Files.Count; index++)
        {
            if ((index & 127) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var file = snapshot.Files[index];
            if (Matches(file, normalizedQuery))
            {
                filtered.Add(file);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var ordered = Order(filtered, normalizedQuery);
        var totalItemCount = ordered.Count;
        var totalPageCount = totalItemCount == 0 ? 0 : (int)Math.Ceiling(totalItemCount / (double)normalizedQuery.PageSize);
        var pageIndex = totalPageCount == 0 ? 0 : Math.Min(normalizedQuery.PageIndex, totalPageCount - 1);
        normalizedQuery = normalizedQuery with { PageIndex = pageIndex };
        var pageItems = totalItemCount == 0
            ? Array.Empty<ResultFile>()
            : ordered.Skip(pageIndex * normalizedQuery.PageSize).Take(normalizedQuery.PageSize).ToArray();
        return new ResultsQueryResult(
            normalizedQuery,
            new ResultsPage(Array.AsReadOnly(pageItems), pageIndex, normalizedQuery.PageSize, totalItemCount, totalPageCount));
    }

    /// <summary>
    /// Normalizes malformed query values to safe documented defaults.
    /// </summary>
    /// <param name="query">The query to normalize.</param>
    /// <returns>A safe query value.</returns>
    public static ResultsQuery Normalize(ResultsQuery? query)
    {
        query ??= ResultsQuery.Default;
        var duplicateFilter = Enum.IsDefined(query.DuplicateFilter) ? query.DuplicateFilter : ResultDuplicateFilter.All;
        var plannedOperationFilter = Enum.IsDefined(query.PlannedOperationFilter) ? query.PlannedOperationFilter : ResultPlannedOperationFilter.All;
        var sortField = Enum.IsDefined(query.SortField) ? query.SortField : ResultsSortField.Name;
        var sortDirection = Enum.IsDefined(query.SortDirection) ? query.SortDirection : SortDirection.Ascending;
        var category = query.Category is { } value && !Enum.IsDefined(value) ? null : query.Category;
        var extension = NormalizeExtension(query.Extension);
        return new ResultsQuery(
            string.IsNullOrWhiteSpace(query.Text) ? null : query.Text.Trim(),
            duplicateFilter,
            extension,
            category,
            plannedOperationFilter,
            sortField,
            sortDirection,
            Math.Max(0, query.PageIndex),
            ApprovedPageSizes.Contains(query.PageSize) ? query.PageSize : ResultsQuery.Default.PageSize,
            string.IsNullOrWhiteSpace(query.DuplicateGroupId) ? null : query.DuplicateGroupId);
    }

    private static bool Matches(ResultFile file, ResultsQuery query)
    {
        return MatchesText(file, query.Text)
            && MatchesDuplicate(file, query.DuplicateFilter)
            && (query.Extension is null || string.Equals(file.NormalizedExtension, query.Extension, StringComparison.Ordinal))
            && MatchesCategory(file, query.Category)
            && MatchesPlannedOperation(file, query.PlannedOperationFilter)
            && (query.DuplicateGroupId is null || string.Equals(file.DuplicateGroupId, query.DuplicateGroupId, StringComparison.Ordinal));
    }

    private static bool MatchesText(ResultFile file, string? text)
    {
        return text is null
            || file.DisplayFileName.Contains(text, StringComparison.OrdinalIgnoreCase)
            || file.FullPath.Contains(text, StringComparison.OrdinalIgnoreCase)
            || file.NormalizedExtension.Contains(text, StringComparison.OrdinalIgnoreCase)
            || file.ClassificationDisplay.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesDuplicate(ResultFile file, ResultDuplicateFilter filter) => filter switch
    {
        ResultDuplicateFilter.All => true,
        ResultDuplicateFilter.ExactDuplicatesOnly => file.DuplicateStatus == DuplicateStatus.Duplicate,
        ResultDuplicateFilter.UniqueOnly => file.DuplicateStatus == DuplicateStatus.Unique,
        ResultDuplicateFilter.UnknownOrUnavailable => file.DuplicateStatus == DuplicateStatus.Unknown,
        _ => false,
    };

    private static bool MatchesCategory(ResultFile file, FileCategory? category)
    {
        if (category is null)
        {
            return true;
        }

        return category == FileCategory.Unknown
            ? file.Category is null or FileCategory.Unknown
            : file.Category == category;
    }

    private static bool MatchesPlannedOperation(ResultFile file, ResultPlannedOperationFilter filter) => filter switch
    {
        ResultPlannedOperationFilter.All => true,
        ResultPlannedOperationFilter.HasAcceptedOperation => file.HasPlannedOperation,
        ResultPlannedOperationFilter.NoAcceptedOperation => !file.HasPlannedOperation,
        _ => false,
    };

    private static List<ResultFile> Order(List<ResultFile> files, ResultsQuery query)
    {
        IOrderedEnumerable<ResultFile> ordered = query.SortField switch
        {
            ResultsSortField.Name => files.OrderBy(file => file.DisplayFileName, StringComparer.OrdinalIgnoreCase),
            ResultsSortField.Path => files.OrderBy(file => file.FullPath, StringComparer.OrdinalIgnoreCase),
            ResultsSortField.Extension => files.OrderBy(file => file.NormalizedExtension, StringComparer.OrdinalIgnoreCase),
            ResultsSortField.Size => files.OrderBy(file => file.SizeInBytes is null).ThenBy(file => file.SizeInBytes),
            ResultsSortField.ModifiedTime => files.OrderBy(file => file.LastWriteTimeUtc is null).ThenBy(file => file.LastWriteTimeUtc),
            ResultsSortField.DuplicateState => files.OrderBy(file => file.DuplicateStatus),
            _ => throw new InvalidOperationException("The query sort field was not normalized."),
        };

        if (query.SortDirection == SortDirection.Descending)
        {
            ordered = query.SortField switch
            {
                ResultsSortField.Name => files.OrderByDescending(file => file.DisplayFileName, StringComparer.OrdinalIgnoreCase),
                ResultsSortField.Path => files.OrderByDescending(file => file.FullPath, StringComparer.OrdinalIgnoreCase),
                ResultsSortField.Extension => files.OrderByDescending(file => file.NormalizedExtension, StringComparer.OrdinalIgnoreCase),
                ResultsSortField.Size => files.OrderBy(file => file.SizeInBytes is not null).ThenByDescending(file => file.SizeInBytes),
                ResultsSortField.ModifiedTime => files.OrderBy(file => file.LastWriteTimeUtc is not null).ThenByDescending(file => file.LastWriteTimeUtc),
                ResultsSortField.DuplicateState => files.OrderByDescending(file => file.DuplicateStatus),
                _ => throw new InvalidOperationException("The query sort field was not normalized."),
            };
        }

        return ordered.ThenBy(file => file.FullPath, StringComparer.Ordinal).ThenBy(file => file.Id, StringComparer.Ordinal).ToList();
    }

    private static string? NormalizeExtension(string? extension)
    {
        if (extension is null)
        {
            return null;
        }

        var trimmed = extension.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return $".{trimmed.TrimStart('.').ToLowerInvariant()}";
    }
}
