using OpenSorSe.Application.Models;
using OpenSorSe.Desktop.ViewModels;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies pure, deterministic in-memory result-query behavior.
/// </summary>
public sealed class ResultsQueryEngineTests
{
    /// <summary>
    /// Verifies text search uses ordinal case-insensitive display values without filesystem access.
    /// </summary>
    [Fact]
    public void Evaluate_TextSearchMatchesNamePathExtensionAndClassification()
    {
        var snapshot = CreateSnapshot(
            CreateFile("file:0", "C:\\Reports\\Invoice.TXT", ".txt", "Document"),
            CreateFile("file:1", "C:\\Code\\build.cs", ".cs", "Code"));

        var byName = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { Text = "invoice" });
        var byPath = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { Text = "CODE" });
        var byExtension = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { Text = ".TXT" });
        var byClassification = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { Text = "document" });

        Assert.Equal("file:0", Assert.Single(byName.Page.Items).Id);
        Assert.Equal("file:1", Assert.Single(byPath.Page.Items).Id);
        Assert.Equal("file:0", Assert.Single(byExtension.Page.Items).Id);
        Assert.Equal("file:0", Assert.Single(byClassification.Page.Items).Id);
    }

    /// <summary>
    /// Verifies invalid input is reset to bounded safe defaults and missing values sort after known values ascending.
    /// </summary>
    [Fact]
    public void Evaluate_NormalizesInvalidQueryAndSortsDeterministically()
    {
        var snapshot = CreateSnapshot(
            CreateFile("file:2", "C:\\two.txt", ".txt", "Document", size: null),
            CreateFile("file:1", "C:\\one.txt", ".txt", "Document", size: 10),
            CreateFile("file:0", "C:\\zero.txt", ".txt", "Document", size: 10));
        var invalid = new ResultsQuery(null, (ResultDuplicateFilter)999, ".TXT", (FileCategory)999, (ResultPlannedOperationFilter)999, (ResultsSortField)999, (SortDirection)999, -3, 9999);

        var normalized = ResultsQueryEngine.Evaluate(snapshot, invalid);
        var sorted = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { SortField = ResultsSortField.Size });
        var descending = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { SortField = ResultsSortField.Size, SortDirection = SortDirection.Descending });

        Assert.Equal(200, normalized.Query.PageSize);
        Assert.Equal(0, normalized.Query.PageIndex);
        Assert.Equal(ResultDuplicateFilter.All, normalized.Query.DuplicateFilter);
        Assert.Equal(["file:1", "file:0", "file:2"], sorted.Page.Items.Select(file => file.Id));
        Assert.Equal(["file:2", "file:1", "file:0"], descending.Page.Items.Select(file => file.Id));
    }

    /// <summary>
    /// Verifies opaque group filtering and page bounds never expose an unbounded result page.
    /// </summary>
    [Fact]
    public void Evaluate_GroupFilterAndPaging_ReturnOnlyRequestedBoundedRows()
    {
        var files = Enumerable.Range(0, 600)
            .Select(index => CreateFile($"file:{index}", $"C:\\items\\{index:D4}.bin", ".bin", "Data", groupId: index < 2 ? "group:opaque" : null))
            .ToArray();
        var snapshot = CreateSnapshot(files);

        var page = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { PageSize = 50, PageIndex = 99 });
        var group = ResultsQueryEngine.Evaluate(snapshot, ResultsQuery.Default with { DuplicateGroupId = "group:opaque" });

        Assert.Equal(11, page.Page.PageIndex);
        Assert.Equal(50, page.Page.Items.Count);
        Assert.Equal(2, group.Page.TotalItemCount);
        Assert.All(group.Page.Items, file => Assert.Equal("group:opaque", file.DuplicateGroupId));
    }

    private static ResultsSnapshot CreateSnapshot(params ResultFile[] files) => new(
        "session:test",
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch,
        Array.AsReadOnly(files),
        Array.AsReadOnly(Array.Empty<ResultDirectory>()),
        Array.AsReadOnly(Array.Empty<ResultDuplicateGroup>()),
        Array.AsReadOnly(Array.Empty<ResultPlannedOperation>()),
        Array.AsReadOnly(Array.Empty<ResultIssue>()),
        new ResultsSnapshotStatistics(files.Length, 0, 0, 0, 0, 0, 0),
        true);

    private static ResultFile CreateFile(string id, string path, string extension, string classification, long? size = 1, string? groupId = null) =>
        new(
            id,
            path,
            Path.GetFileName(path),
            extension,
            size,
            null,
            classification == "Code" ? FileCategory.Code : classification == "Data" ? FileCategory.Data : FileCategory.Document,
            classification,
            groupId is null ? DuplicateStatus.Unique : DuplicateStatus.Duplicate,
            groupId,
            false);
}
