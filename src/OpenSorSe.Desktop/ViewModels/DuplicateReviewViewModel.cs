using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenSorSe.Application.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents existing exact-hash duplicate groups for read-only review.
/// </summary>
public sealed class DuplicateReviewViewModel : ViewModelBase
{
    private readonly ObservableCollection<ResultDuplicateGroup> _visibleGroups = [];
    private readonly ObservableCollection<DuplicateReviewGroupRow> _visibleGroupRows = [];
    private readonly ObservableCollection<ResultFile> _selectedMembers = [];
    private ResultsSnapshot? _snapshot;
    private string? _filterText;
    private ResultDuplicateGroup? _selectedGroup;
    private DuplicateReviewGroupRow? _selectedGroupRow;
    private string _statusText = "No completed scan results are available.";
    private bool _isDuplicateDataAvailable;

    /// <summary>
    /// Initializes duplicate-review commands and immutable collection wrappers.
    /// </summary>
    public DuplicateReviewViewModel()
    {
        VisibleGroups = new ReadOnlyObservableCollection<ResultDuplicateGroup>(_visibleGroups);
        VisibleGroupRows = new ReadOnlyObservableCollection<DuplicateReviewGroupRow>(_visibleGroupRows);
        SelectedMembers = new ReadOnlyObservableCollection<ResultFile>(_selectedMembers);
        ShowGroupFilesCommand = new RelayCommand(ShowGroupFiles, CanShowGroupFiles);
        BackToExplorerCommand = new RelayCommand(RequestBackToExplorer);
    }

    /// <summary>
    /// Occurs when the selected group's known members should be shown in the file explorer.
    /// </summary>
    public event EventHandler<string>? ShowGroupFilesRequested;

    /// <summary>Occurs when the user requests return to the file explorer.</summary>
    public event EventHandler? BackToExplorerRequested;

    /// <summary>Gets visible exact-duplicate groups in detector order.</summary>
    public ReadOnlyObservableCollection<ResultDuplicateGroup> VisibleGroups { get; }

    /// <summary>Gets display summaries for visible exact-duplicate groups in detector order.</summary>
    public ReadOnlyObservableCollection<DuplicateReviewGroupRow> VisibleGroupRows { get; }

    /// <summary>Gets known members of the selected exact-duplicate group.</summary>
    public ReadOnlyObservableCollection<ResultFile> SelectedMembers { get; }

    /// <summary>Gets or sets the local group-member text filter.</summary>
    public string? FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>Gets or sets the currently selected duplicate group.</summary>
    public ResultDuplicateGroup? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetProperty(ref _selectedGroup, value))
            {
                var matchingRow = value is null
                    ? null
                    : _visibleGroupRows.FirstOrDefault(row => string.Equals(row.Group.GroupId, value.GroupId, StringComparison.Ordinal));
                if (!ReferenceEquals(_selectedGroupRow, matchingRow))
                {
                    _selectedGroupRow = matchingRow;
                    OnPropertyChanged(nameof(SelectedGroupRow));
                }
                UpdateSelectedMembers();
                ShowGroupFilesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets or sets the selected display row for an exact-duplicate group.</summary>
    public DuplicateReviewGroupRow? SelectedGroupRow
    {
        get => _selectedGroupRow;
        set
        {
            if (SetProperty(ref _selectedGroupRow, value))
            {
                SelectedGroup = value?.Group;
            }
        }
    }

    /// <summary>Gets a user-safe description of the current duplicate-review state.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Gets whether a snapshot with duplicate data is available.</summary>
    public bool IsDuplicateDataAvailable
    {
        get => _isDuplicateDataAvailable;
        private set => SetProperty(ref _isDuplicateDataAvailable, value);
    }

    /// <summary>Gets whether the active snapshot has any exact-duplicate groups.</summary>
    public bool HasDuplicateGroups => _snapshot?.DuplicateGroups.Count > 0 && IsDuplicateDataAvailable;

    /// <summary>Gets whether filtering leaves one or more duplicate groups visible.</summary>
    public bool HasVisibleGroups => _visibleGroups.Count > 0;

    /// <summary>Gets whether no duplicate groups are currently visible.</summary>
    public bool HasNoVisibleGroups => !HasVisibleGroups;

    /// <summary>Gets whether a duplicate group is currently selected.</summary>
    public bool HasSelectedGroup => SelectedGroup is not null;

    /// <summary>Gets display text for the selected group's common known file size.</summary>
    public string SelectedGroupCommonSizeText => SelectedGroup?.CommonFileSizeInBytes is { } size
        ? ResultsFileRow.FormatSize(size)
        : "Unavailable";

    /// <summary>Gets display text for the selected group's theoretical reclaimable bytes.</summary>
    public string SelectedGroupPotentialReclaimableText => SelectedGroup?.PotentialReclaimableBytes is { } bytes
        ? ResultsFileRow.FormatSize(bytes)
        : "Unavailable";

    /// <summary>Gets the command that routes selected known members back to the read-only explorer.</summary>
    public IRelayCommand ShowGroupFilesCommand { get; }

    /// <summary>Gets the command that returns to the existing results explorer.</summary>
    public IRelayCommand BackToExplorerCommand { get; }

    /// <summary>
    /// Replaces review state with a new immutable snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to review, or null when no completed scan exists.</param>
    public void LoadSnapshot(ResultsSnapshot? snapshot)
    {
        _snapshot = snapshot;
        _filterText = null;
        OnPropertyChanged(nameof(FilterText));
        SelectedGroup = null;
        IsDuplicateDataAvailable = snapshot?.IsDuplicateDataAvailable == true;
        ApplyFilter();
        OnPropertyChanged(nameof(HasDuplicateGroups));
    }

    /// <summary>
    /// Applies an incoming opaque group selection when it belongs to the current snapshot.
    /// </summary>
    /// <param name="groupId">The internal duplicate-group key, or null to clear selection.</param>
    public void SelectGroup(string? groupId)
    {
        SelectedGroup = string.IsNullOrWhiteSpace(groupId)
            ? null
            : _visibleGroups.FirstOrDefault(group => string.Equals(group.GroupId, groupId, StringComparison.Ordinal));
    }

    private void ApplyFilter()
    {
        _visibleGroups.Clear();
        _visibleGroupRows.Clear();
        if (_snapshot is null)
        {
            StatusText = "No completed scan results are available.";
        }
        else if (!IsDuplicateDataAvailable)
        {
            StatusText = _snapshot.Issues.FirstOrDefault(issue => issue.SourceStage == "Exact duplicates")?.Message
                ?? "Exact duplicate review was unavailable for this completed scan.";
        }
        else
        {
            var memberById = _snapshot.Files.ToDictionary(file => file.Id, StringComparer.Ordinal);
            var filter = string.IsNullOrWhiteSpace(FilterText) ? null : FilterText.Trim();
            foreach (var group in _snapshot.DuplicateGroups)
            {
                if (filter is null || group.MemberFileIds
                        .Where(memberById.ContainsKey)
                        .Select(memberId => memberById[memberId])
                        .Any(file => MatchesFilter(file, filter)))
                {
                    _visibleGroups.Add(group);
                    _visibleGroupRows.Add(CreateGroupRow(group, memberById));
                }
            }

            StatusText = _snapshot.DuplicateGroups.Count == 0
                ? "No exact duplicate groups were found in this completed scan."
                : _visibleGroups.Count == 0
                    ? "No exact duplicate groups match the active filter."
                    : $"Exact duplicate groups: {_visibleGroups.Count}.";
        }

        if (SelectedGroup is not null && !_visibleGroups.Contains(SelectedGroup))
        {
            SelectedGroup = null;
        }

        OnPropertyChanged(nameof(HasVisibleGroups));
        OnPropertyChanged(nameof(HasNoVisibleGroups));
        OnPropertyChanged(nameof(HasDuplicateGroups));
    }

    private static bool MatchesFilter(ResultFile file, string filter) =>
        file.DisplayFileName.Contains(filter, StringComparison.OrdinalIgnoreCase)
        || file.FullPath.Contains(filter, StringComparison.OrdinalIgnoreCase)
        || file.NormalizedExtension.Contains(filter, StringComparison.OrdinalIgnoreCase)
        || file.ClassificationDisplay.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static DuplicateReviewGroupRow CreateGroupRow(
        ResultDuplicateGroup group,
        IReadOnlyDictionary<string, ResultFile> memberById)
    {
        var memberNames = group.MemberFileIds
            .Where(memberById.ContainsKey)
            .Select(memberId => memberById[memberId].DisplayFileName)
            .Take(2)
            .ToArray();
        var summary = memberNames.Length == 0
            ? "Member details unavailable"
            : string.Join(", ", memberNames) + (group.MemberCount > memberNames.Length ? $" and {group.MemberCount - memberNames.Length} more" : string.Empty);
        return new DuplicateReviewGroupRow(
            group,
            summary,
            group.CommonFileSizeInBytes is { } size ? ResultsFileRow.FormatSize(size) : "Unavailable",
            group.PotentialReclaimableBytes is { } bytes ? ResultsFileRow.FormatSize(bytes) : "Unavailable");
    }

    private void UpdateSelectedMembers()
    {
        _selectedMembers.Clear();
        if (SelectedGroup is not null && _snapshot is not null)
        {
            var memberById = _snapshot.Files.ToDictionary(file => file.Id, StringComparer.Ordinal);
            foreach (var memberId in SelectedGroup.MemberFileIds)
            {
                if (memberById.TryGetValue(memberId, out var member))
                {
                    _selectedMembers.Add(member);
                }
            }

            if (_selectedMembers.Count != SelectedGroup.MemberCount)
            {
                StatusText = "Some exact duplicate members could not be shown from this completed scan.";
            }
        }

        OnPropertyChanged(nameof(HasSelectedGroup));
        OnPropertyChanged(nameof(SelectedGroupCommonSizeText));
        OnPropertyChanged(nameof(SelectedGroupPotentialReclaimableText));
    }

    private bool CanShowGroupFiles() => SelectedGroup is not null;

    private void ShowGroupFiles()
    {
        if (SelectedGroup is not null)
        {
            ShowGroupFilesRequested?.Invoke(this, SelectedGroup.GroupId);
        }
    }

    private void RequestBackToExplorer() => BackToExplorerRequested?.Invoke(this, EventArgs.Empty);
}
