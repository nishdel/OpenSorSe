using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents immutable analysis results and emits explicit user-review decisions without executing operations.
/// </summary>
public sealed class ResultsViewModel : ViewModelBase
{
    private readonly ObservableCollection<FileEntry> _files = [];
    private readonly ObservableCollection<PlannedOperation> _operations = [];
    private readonly ObservableCollection<string> _warnings = [];
    private ResultsSummary _summary = ResultsSummary.Empty;
    private string _statusText = "No results are available.";

    /// <summary>
    /// Initializes the review commands.
    /// </summary>
    public ResultsViewModel()
    {
        Files = new ReadOnlyObservableCollection<FileEntry>(_files);
        Operations = new ReadOnlyObservableCollection<PlannedOperation>(_operations);
        Warnings = new ReadOnlyObservableCollection<string>(_warnings);
        ApproveExecutionCommand = new RelayCommand(ApproveExecution, CanApproveExecution);
        CancelCommand = new RelayCommand(CancelReview);
        BackCommand = new RelayCommand(RequestBack);
    }

    /// <summary>
    /// Occurs when the user explicitly approves the currently displayed operation set for a later executor stage.
    /// </summary>
    public event EventHandler<IReadOnlyList<PlannedOperation>>? ExecutionApproved;

    /// <summary>
    /// Occurs when the user cancels the current results review.
    /// </summary>
    public event EventHandler? ReviewCancelled;

    /// <summary>
    /// Occurs when the user requests navigation away from the review page.
    /// </summary>
    public event EventHandler? BackRequested;

    /// <summary>
    /// Gets files in the order supplied to the current review.
    /// </summary>
    public ReadOnlyObservableCollection<FileEntry> Files { get; }

    /// <summary>
    /// Gets accepted operations in conflict-resolution order.
    /// </summary>
    public ReadOnlyObservableCollection<PlannedOperation> Operations { get; }

    /// <summary>
    /// Gets user-safe warnings in conflict-resolution issue order.
    /// </summary>
    public ReadOnlyObservableCollection<string> Warnings { get; }

    /// <summary>
    /// Gets aggregate values for the current review.
    /// </summary>
    public ResultsSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    /// <summary>
    /// Gets the user-safe review status.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the command that emits the current operation list after explicit approval.
    /// </summary>
    public IRelayCommand ApproveExecutionCommand { get; }

    /// <summary>
    /// Gets the command that cancels the review without changing files.
    /// </summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>
    /// Gets the command that requests navigation back to an earlier page.
    /// </summary>
    public IRelayCommand BackCommand { get; }

    /// <summary>
    /// Loads immutable files and resolved operations for review.
    /// </summary>
    /// <param name="files">Files to present in caller-supplied order.</param>
    /// <param name="resolution">The conflict-resolution output to present.</param>
    public void Load(IReadOnlyCollection<FileEntry> files, ConflictResolutionResult resolution)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(resolution);
        if (files.Any(file => file is null))
        {
            throw new ArgumentException("The file collection cannot contain null entries.", nameof(files));
        }

        _files.Clear();
        foreach (var file in files)
        {
            _files.Add(file);
        }
        _operations.Clear();
        foreach (var operation in resolution.Operations)
        {
            _operations.Add(operation);
        }
        _warnings.Clear();
        foreach (var issue in resolution.Issues)
        {
            _warnings.Add(issue.Message);
        }
        var duplicateGroups = _files
            .Select(file => file.Duplicate?.GroupId)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .LongCount();
        Summary = new ResultsSummary(_files.Count, _operations.Count, duplicateGroups, _warnings.Count);
        StatusText = _operations.Count == 0 ? "No operations are awaiting approval." : "Waiting for user confirmation.";
        ApproveExecutionCommand.NotifyCanExecuteChanged();
    }

    private bool CanApproveExecution() => _operations.Count > 0;

    private void ApproveExecution()
    {
        if (_operations.Count == 0)
        {
            return;
        }

        ExecutionApproved?.Invoke(this, Array.AsReadOnly(_operations.ToArray()));
        StatusText = "Execution approval requested.";
    }

    private void CancelReview()
    {
        ReviewCancelled?.Invoke(this, EventArgs.Empty);
        StatusText = "Review cancelled.";
    }

    private void RequestBack() => BackRequested?.Invoke(this, EventArgs.Empty);
}
