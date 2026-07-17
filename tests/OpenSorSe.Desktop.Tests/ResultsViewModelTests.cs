using OpenSorSe.Desktop.ViewModels;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies deterministic, read-only results-review behavior.
/// </summary>
public sealed class ResultsViewModelTests
{
    /// <summary>
    /// Verifies loading preserves supplied entries and exposes accepted operations and warnings.
    /// </summary>
    [Fact]
    public void Load_PresentsImmutableInputsAndSummary()
    {
        var first = new FileEntry("C:\\first.txt", Duplicate: new DuplicateClassification(DuplicateStatus.Duplicate, "sha256:one"));
        var second = new FileEntry("C:\\second.txt", Duplicate: new DuplicateClassification(DuplicateStatus.Duplicate, "sha256:one"));
        var operation = CreateOperation(first, "plan:0");
        var resolution = new ConflictResolutionResult(
            [operation],
            EmptyStatistics(operationsProcessed: 1, operationsAccepted: 1),
            [new ConflictResolutionIssue(0, "plan:1", ConflictResolutionIssueKind.DestinationConflict, "A destination conflict was found.")]);
        var viewModel = new ResultsViewModel();

        viewModel.Load([first, second], resolution);

        Assert.Equal([first, second], viewModel.Files);
        Assert.Equal([operation], viewModel.Operations);
        Assert.Equal(["A destination conflict was found."], viewModel.Warnings);
        Assert.Equal(new ResultsSummary(2, 1, 1, 1), viewModel.Summary);
        Assert.Equal("Waiting for user confirmation.", viewModel.StatusText);
    }

    /// <summary>
    /// Verifies approval emits the accepted plan only after it has been loaded.
    /// </summary>
    [Fact]
    public void ApproveExecution_EmitsCurrentOperationsWithoutExecutingThem()
    {
        var file = new FileEntry("C:\\first.txt");
        var operation = CreateOperation(file, "plan:0");
        var viewModel = new ResultsViewModel();
        viewModel.Load([file], new ConflictResolutionResult([operation], EmptyStatistics(1, 1), []));
        IReadOnlyList<PlannedOperation>? approvedOperations = null;
        viewModel.ExecutionApproved += (_, operations) => approvedOperations = operations;

        viewModel.ApproveExecutionCommand.Execute(null);

        var approved = Assert.IsAssignableFrom<IReadOnlyList<PlannedOperation>>(approvedOperations);
        Assert.Equal([operation], approved);
        Assert.Equal("Execution approval requested.", viewModel.StatusText);
        Assert.Equal("C:\\first.txt", file.FullPath);
    }

    /// <summary>
    /// Verifies an empty plan cannot emit execution approval and remains a normal review state.
    /// </summary>
    [Fact]
    public void Load_EmptyPlan_DisablesApproval()
    {
        var viewModel = new ResultsViewModel();
        viewModel.Load([], new ConflictResolutionResult([], EmptyStatistics(0, 0), []));

        Assert.False(viewModel.ApproveExecutionCommand.CanExecute(null));
        Assert.Equal(ResultsSummary.Empty, viewModel.Summary);
        Assert.Equal("No operations are awaiting approval.", viewModel.StatusText);
    }

    /// <summary>
    /// Verifies cancellation and back commands only emit review decisions.
    /// </summary>
    [Fact]
    public void ReviewCommands_EmitCancellationAndBackRequests()
    {
        var viewModel = new ResultsViewModel();
        var cancelled = false;
        var returned = false;
        viewModel.ReviewCancelled += (_, _) => cancelled = true;
        viewModel.BackRequested += (_, _) => returned = true;

        viewModel.CancelCommand.Execute(null);
        viewModel.BackCommand.Execute(null);

        Assert.True(cancelled);
        Assert.True(returned);
        Assert.Equal("Review cancelled.", viewModel.StatusText);
    }

    private static PlannedOperation CreateOperation(FileEntry file, string identifier) =>
        new(identifier, PlannedOperationKind.Move, file, file.FullPath, "C:\\Destination\\first.txt", "rule", "Rule", 1);

    private static ConflictResolutionStatistics EmptyStatistics(long operationsProcessed, long operationsAccepted) =>
        new(operationsProcessed, operationsAccepted, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
