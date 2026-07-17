using OpenSorSe.Application.Models;
using OpenSorSe.Desktop.ViewModels;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies bounded, read-only snapshot exploration and exact-duplicate review behavior.
/// </summary>
public sealed class ResultsViewModelTests
{
    /// <summary>
    /// Verifies loading keeps review data in the immutable snapshot and publishes only one bounded page.
    /// </summary>
    [Fact]
    public async Task LoadSnapshotAsync_PresentsSummaryWarningsAndBoundedRows()
    {
        var snapshot = CreateSnapshot(files: [
            CreateFile("file:0", "C:\\Selected\\first.txt", DuplicateStatus.Duplicate, "group:opaque"),
            CreateFile("file:1", "C:\\Selected\\second.txt", DuplicateStatus.Duplicate, "group:opaque")]);
        using var viewModel = new ResultsViewModel();

        await viewModel.LoadSnapshotAsync(snapshot);

        Assert.Same(snapshot, viewModel.Snapshot);
        Assert.Equal(new ResultsSummary(2, 1, 1, 2, 1), viewModel.Summary);
        Assert.Equal(2, viewModel.PageRows.Count);
        Assert.Equal("first.txt", viewModel.PageRows[0].FileName);
        Assert.Equal("Exact duplicate", viewModel.PageRows[0].DuplicateStatus);
        Assert.Equal("Yes", viewModel.PageRows[0].PlannedOperation);
        Assert.Equal(["A test scan warning."], viewModel.Warnings);
        Assert.Single(viewModel.Directories);
        Assert.Single(viewModel.PlannedOperations);
        Assert.True(viewModel.HasResults);
        Assert.True(viewModel.HasWarnings);
        Assert.False(viewModel.IsLoading);
    }

    /// <summary>
    /// Verifies filters reset paging, selection is derived from a bounded row, and no-all page can be requested.
    /// </summary>
    [Fact]
    public async Task QueryAndPaging_FilterSortAndSelectWithoutRetainingStaleDetails()
    {
        var files = Enumerable.Range(0, 250)
            .Select(index => CreateFile(
                $"file:{index}",
                $"C:\\Selected\\{index:D3}.txt",
                index % 2 == 0 ? DuplicateStatus.Duplicate : DuplicateStatus.Unique,
                index % 2 == 0 ? "group:opaque" : null,
                size: index,
                category: index % 3 == 0 ? FileCategory.Document : FileCategory.Code))
            .ToArray();
        using var viewModel = new ResultsViewModel();
        await viewModel.LoadSnapshotAsync(CreateSnapshot(files));

        Assert.Equal(200, viewModel.PageRows.Count);
        Assert.True(viewModel.CanGoNextPage);
        viewModel.NextPageCommand.Execute(null);
        await viewModel.RefreshAsync();
        Assert.Equal(50, viewModel.PageRows.Count);

        viewModel.SelectedDuplicateFilter = ResultDuplicateFilter.ExactDuplicatesOnly;
        await viewModel.RefreshAsync();
        Assert.Equal(125, viewModel.Page.TotalItemCount);
        Assert.Equal(0, viewModel.Page.PageIndex);
        viewModel.SelectedRow = viewModel.PageRows[0];
        Assert.NotNull(viewModel.SelectedDetails);
        Assert.Equal(viewModel.PageRows[0].FullPath, viewModel.SelectedDetails!.FullPath);

        viewModel.QueryText = "does-not-match";
        await viewModel.RefreshAsync();
        Assert.False(viewModel.HasFilterResults);
        Assert.True(viewModel.HasNoFilterResults);
        Assert.Null(viewModel.SelectedDetails);
        Assert.DoesNotContain(viewModel.AvailablePageSizes, pageSize => pageSize <= 0 || pageSize > 500);
    }

    /// <summary>
    /// Verifies exact duplicate review uses opaque group routing and exposes no execution behavior.
    /// </summary>
    [Fact]
    public async Task DuplicateReview_ShowGroupFilesRoutesToFilteredExplorer()
    {
        var snapshot = CreateSnapshot(files: [
            CreateFile("file:0", "C:\\One\\same.txt", DuplicateStatus.Duplicate, "group:opaque"),
            CreateFile("file:1", "C:\\Two\\same.txt", DuplicateStatus.Duplicate, "group:opaque"),
            CreateFile("file:2", "C:\\Two\\other.txt", DuplicateStatus.Unique, null)]);
        using var viewModel = new ResultsViewModel();
        await viewModel.LoadSnapshotAsync(snapshot);

        viewModel.OpenDuplicateReviewCommand.Execute(null);
        Assert.True(viewModel.IsDuplicateReviewVisible);
        var groupRow = Assert.Single(viewModel.DuplicateReview.VisibleGroupRows);
        Assert.Equal("same.txt, same.txt", groupRow.MemberSummary);
        Assert.Equal("2 B", groupRow.CommonFileSize);
        Assert.Equal("2 B", groupRow.PotentialReclaimableSpace);
        viewModel.DuplicateReview.SelectedGroup = Assert.Single(viewModel.DuplicateReview.VisibleGroups);
        Assert.Equal("2 B", viewModel.DuplicateReview.SelectedGroupPotentialReclaimableText);
        viewModel.DuplicateReview.ShowGroupFilesCommand.Execute(null);
        await viewModel.RefreshAsync();

        Assert.True(viewModel.IsExplorerVisible);
        Assert.Equal("group:opaque", viewModel.Query.DuplicateGroupId);
        Assert.Equal(2, viewModel.PageRows.Count);
        Assert.All(viewModel.PageRows, row => Assert.Equal("Exact duplicate", row.DuplicateStatus));
    }

    /// <summary>
    /// Verifies an unavailable duplicate detector result remains a usable explorer with a clear limitation.
    /// </summary>
    [Fact]
    public async Task LoadSnapshotAsync_DuplicateDataUnavailable_DisablesDuplicateReview()
    {
        var snapshot = CreateSnapshot([CreateFile("file:0", "C:\\Only\\entry.txt", DuplicateStatus.Unknown, null)], duplicateDataAvailable: false);
        using var viewModel = new ResultsViewModel();

        await viewModel.LoadSnapshotAsync(snapshot);

        Assert.False(viewModel.CanOpenDuplicateReview);
        Assert.Contains(viewModel.Warnings, warning => warning.Contains("unavailable", StringComparison.OrdinalIgnoreCase));
        Assert.Single(viewModel.PageRows);
    }

    private static ResultsSnapshot CreateSnapshot(IReadOnlyList<ResultFile> files, bool duplicateDataAvailable = true)
    {
        var groups = duplicateDataAvailable && files.Count(file => file.DuplicateGroupId == "group:opaque") >= 2
            ? Array.AsReadOnly<ResultDuplicateGroup>([
                new ResultDuplicateGroup("group:opaque", 1, Array.AsReadOnly(["file:0", "file:1"]), 2, 2, 2),
            ])
            : Array.AsReadOnly(Array.Empty<ResultDuplicateGroup>());
        var issues = duplicateDataAvailable
            ? Array.AsReadOnly<ResultIssue>([new ResultIssue("Scanning", ResultIssueSeverity.Warning, "A test scan warning.")])
            : Array.AsReadOnly<ResultIssue>([new ResultIssue("Exact duplicates", ResultIssueSeverity.Warning, "Exact duplicate review was unavailable for this completed scan.")]);
        var operations = Array.AsReadOnly<ResultPlannedOperation>([
            new ResultPlannedOperation("plan:0", PlannedOperationKind.Move, files[0].Id, "C:\\Destination\\first.txt", "Rule"),
        ]);
        var updatedFiles = files.Select((file, index) => file with { HasPlannedOperation = index == 0 }).ToArray();
        return new ResultsSnapshot(
            "session:test",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            Array.AsReadOnly(updatedFiles),
            Array.AsReadOnly<ResultDirectory>([new ResultDirectory("C:\\Selected", "Selected")]),
            groups,
            operations,
            issues,
            new ResultsSnapshotStatistics(updatedFiles.Length, 1, groups.Count, groups.Sum(group => group.MemberCount), 1, issues.Count, 0),
            duplicateDataAvailable);
    }

    private static ResultFile CreateFile(
        string id,
        string path,
        DuplicateStatus duplicateStatus,
        string? groupId,
        long size = 2,
        FileCategory category = FileCategory.Document) =>
        new(
            id,
            path,
            Path.GetFileName(path),
            ".txt",
            size,
            DateTimeOffset.UnixEpoch,
            category,
            category.ToString(),
            duplicateStatus,
            groupId,
            false);
}
