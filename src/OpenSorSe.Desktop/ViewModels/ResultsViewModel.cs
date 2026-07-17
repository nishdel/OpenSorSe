using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Owns read-only exploration state for one immutable completed-results snapshot.
/// </summary>
public sealed class ResultsViewModel : ViewModelBase, IDisposable
{
    private static readonly IReadOnlyList<int> PageSizes = [50, 100, 200, 500];
    private static readonly IReadOnlyList<ResultDuplicateFilter> DuplicateFilters = Enum.GetValues<ResultDuplicateFilter>();
    private static readonly IReadOnlyList<ResultPlannedOperationFilter> PlannedOperationFilters = Enum.GetValues<ResultPlannedOperationFilter>();
    private static readonly IReadOnlyList<ResultsSortField> SortFields = Enum.GetValues<ResultsSortField>();
    private static readonly IReadOnlyList<SortDirection> SortDirections = Enum.GetValues<SortDirection>();
    private readonly ObservableCollection<ResultsFileRow> _pageRows = [];
    private readonly ObservableCollection<ResultDirectory> _directories = [];
    private readonly ObservableCollection<ResultPlannedOperation> _plannedOperations = [];
    private readonly ObservableCollection<string> _warnings = [];
    private readonly ObservableCollection<ResultsExtensionFilterOption> _extensionOptions = [];
    private readonly ObservableCollection<ResultsCategoryFilterOption> _categoryOptions = [];
    private readonly Dictionary<string, IReadOnlyList<TagAssociation>> _tagsByFile = new(StringComparer.Ordinal);
    private CancellationTokenSource? _queryCancellation;
    private long _queryVersion;
    private ResultsSnapshot? _snapshot;
    private ResultsQuery _query = ResultsQuery.Default;
    private ResultsPage _page = ResultsPage.Empty;
    private ResultsFileRow? _selectedRow;
    private SelectedResultDetails? _selectedDetails;
    private ResultsSummary _summary = ResultsSummary.Empty;
    private string _statusText = "No completed scan results are available.";
    private bool _isLoading;
    private ResultsDisplayMode _displayMode = ResultsDisplayMode.Explorer;

    /// <summary>
    /// Initializes the result explorer and its non-mutating navigation commands.
    /// </summary>
    public ResultsViewModel()
        : this(new PreviewConfigurationService(), null)
    {
    }

    /// <summary>
    /// Initializes the result explorer with optional application-owned AI suggestion review.
    /// </summary>
    /// <param name="configurationService">The centralized configuration source used only by the optional suggestion workflow.</param>
    /// <param name="aiSuggestionService">The optional application-owned suggestion service.</param>
    public ResultsViewModel(IConfigurationService configurationService, IAiSuggestionService? aiSuggestionService)
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        PageRows = new ReadOnlyObservableCollection<ResultsFileRow>(_pageRows);
        Directories = new ReadOnlyObservableCollection<ResultDirectory>(_directories);
        PlannedOperations = new ReadOnlyObservableCollection<ResultPlannedOperation>(_plannedOperations);
        Warnings = new ReadOnlyObservableCollection<string>(_warnings);
        ExtensionOptions = new ReadOnlyObservableCollection<ResultsExtensionFilterOption>(_extensionOptions);
        CategoryOptions = new ReadOnlyObservableCollection<ResultsCategoryFilterOption>(_categoryOptions);
        DuplicateReview = new DuplicateReviewViewModel();
        DuplicateReview.ShowGroupFilesRequested += OnShowGroupFilesRequested;
        DuplicateReview.BackToExplorerRequested += OnBackToExplorerRequested;
        AiSuggestions = new AiSuggestionsViewModel(configurationService, aiSuggestionService);
        AiSuggestions.TagsAccepted += OnTagsAccepted;
        ClearFiltersCommand = new RelayCommand(ClearFilters, CanClearFilters);
        PreviousPageCommand = new RelayCommand(GoToPreviousPage, () => CanGoPreviousPage);
        NextPageCommand = new RelayCommand(GoToNextPage, () => CanGoNextPage);
        OpenDuplicateReviewCommand = new RelayCommand(OpenDuplicateReview, () => CanOpenDuplicateReview);
        BackToExplorerCommand = new RelayCommand(BackToExplorer);
    }

    /// <summary>Gets the immutable snapshot currently owned by this review surface.</summary>
    public ResultsSnapshot? Snapshot => _snapshot;

    /// <summary>Gets bounded, display-safe file rows for the active page.</summary>
    public ReadOnlyObservableCollection<ResultsFileRow> PageRows { get; }

    /// <summary>Gets display-safe directories from the current snapshot.</summary>
    public ReadOnlyObservableCollection<ResultDirectory> Directories { get; }

    /// <summary>Gets display-only accepted planned operations from the current snapshot.</summary>
    public ReadOnlyObservableCollection<ResultPlannedOperation> PlannedOperations { get; }

    /// <summary>Gets user-safe issues from the current snapshot.</summary>
    public ReadOnlyObservableCollection<string> Warnings { get; }

    /// <summary>Gets supported extension filter choices.</summary>
    public ReadOnlyObservableCollection<ResultsExtensionFilterOption> ExtensionOptions { get; }

    /// <summary>Gets supported deterministic-category filter choices.</summary>
    public ReadOnlyObservableCollection<ResultsCategoryFilterOption> CategoryOptions { get; }

    /// <summary>Gets fixed page-size choices that keep result rendering bounded.</summary>
    public IReadOnlyList<int> AvailablePageSizes => PageSizes;

    /// <summary>Gets duplicate-state filter choices.</summary>
    public IReadOnlyList<ResultDuplicateFilter> AvailableDuplicateFilters => DuplicateFilters;

    /// <summary>Gets planned-operation filter choices.</summary>
    public IReadOnlyList<ResultPlannedOperationFilter> AvailablePlannedOperationFilters => PlannedOperationFilters;

    /// <summary>Gets sort-field choices.</summary>
    public IReadOnlyList<ResultsSortField> AvailableSortFields => SortFields;

    /// <summary>Gets sort-direction choices.</summary>
    public IReadOnlyList<SortDirection> AvailableSortDirections => SortDirections;

    /// <summary>Gets the child view model for exact-duplicate review.</summary>
    public DuplicateReviewViewModel DuplicateReview { get; }

    /// <summary>Gets the preview-only optional AI suggestion workflow for the selected result.</summary>
    public AiSuggestionsViewModel AiSuggestions { get; }

    /// <summary>Gets the current normalized explorer query.</summary>
    public ResultsQuery Query
    {
        get => _query;
        private set
        {
            if (SetProperty(ref _query, value))
            {
                OnPropertyChanged(nameof(QueryText));
                OnPropertyChanged(nameof(SelectedDuplicateFilter));
                OnPropertyChanged(nameof(SelectedExtensionOption));
                OnPropertyChanged(nameof(SelectedCategoryOption));
                OnPropertyChanged(nameof(SelectedPlannedOperationFilter));
                OnPropertyChanged(nameof(SelectedSortField));
                OnPropertyChanged(nameof(SelectedSortDirection));
                OnPropertyChanged(nameof(SelectedPageSize));
                OnPropertyChanged(nameof(ActiveQueryDescription));
                ClearFiltersCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets or sets the local text query.</summary>
    public string? QueryText
    {
        get => Query.Text;
        set => ChangeQuery(Query with { Text = value, PageIndex = 0 });
    }

    /// <summary>Gets or sets the duplicate-state filter.</summary>
    public ResultDuplicateFilter SelectedDuplicateFilter
    {
        get => Query.DuplicateFilter;
        set => ChangeQuery(Query with { DuplicateFilter = value, PageIndex = 0 });
    }

    /// <summary>Gets or sets the extension filter option.</summary>
    public ResultsExtensionFilterOption? SelectedExtensionOption
    {
        get => ExtensionOptions.FirstOrDefault(option => string.Equals(option.Value, Query.Extension, StringComparison.Ordinal));
        set => ChangeQuery(Query with { Extension = value?.Value, PageIndex = 0 });
    }

    /// <summary>Gets or sets the deterministic-category filter option.</summary>
    public ResultsCategoryFilterOption? SelectedCategoryOption
    {
        get => CategoryOptions.FirstOrDefault(option => option.Value == Query.Category);
        set => ChangeQuery(Query with { Category = value?.Value, PageIndex = 0 });
    }

    /// <summary>Gets or sets the planned-operation filter.</summary>
    public ResultPlannedOperationFilter SelectedPlannedOperationFilter
    {
        get => Query.PlannedOperationFilter;
        set => ChangeQuery(Query with { PlannedOperationFilter = value, PageIndex = 0 });
    }

    /// <summary>Gets or sets the active sort field.</summary>
    public ResultsSortField SelectedSortField
    {
        get => Query.SortField;
        set => ChangeQuery(Query with { SortField = value, PageIndex = 0 });
    }

    /// <summary>Gets or sets the active sort direction.</summary>
    public SortDirection SelectedSortDirection
    {
        get => Query.SortDirection;
        set => ChangeQuery(Query with { SortDirection = value, PageIndex = 0 });
    }

    /// <summary>Gets or sets the active bounded page size.</summary>
    public int SelectedPageSize
    {
        get => Query.PageSize;
        set => ChangeQuery(Query with { PageSize = value, PageIndex = 0 });
    }

    /// <summary>Gets the current bounded page.</summary>
    public ResultsPage Page
    {
        get => _page;
        private set
        {
            if (SetProperty(ref _page, value))
            {
                OnPropertyChanged(nameof(PageRangeText));
                OnPropertyChanged(nameof(CanGoPreviousPage));
                OnPropertyChanged(nameof(CanGoNextPage));
                OnPropertyChanged(nameof(HasFilterResults));
                OnPropertyChanged(nameof(HasNoFilterResults));
                PreviousPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets or sets the selected bounded page row.</summary>
    public ResultsFileRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                UpdateSelectedDetails();
            }
        }
    }

    /// <summary>Gets details derived only from the selected immutable result row.</summary>
    public SelectedResultDetails? SelectedDetails
    {
        get => _selectedDetails;
        private set
        {
            if (SetProperty(ref _selectedDetails, value))
            {
                OnPropertyChanged(nameof(HasSelectedDetails));
            }
        }
    }

    /// <summary>Gets aggregate snapshot values for the existing results summary.</summary>
    public ResultsSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    /// <summary>Gets user-safe local-review state text.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Gets whether local query work is pending.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>Gets or sets the active child review surface.</summary>
    public ResultsDisplayMode DisplayMode
    {
        get => _displayMode;
        private set
        {
            if (SetProperty(ref _displayMode, value))
            {
                OnPropertyChanged(nameof(IsExplorerVisible));
                OnPropertyChanged(nameof(IsDuplicateReviewVisible));
            }
        }
    }

    /// <summary>Gets whether a completed snapshot is available.</summary>
    public bool HasSnapshot => Snapshot is not null;

    /// <summary>Gets whether no completed snapshot is available.</summary>
    public bool HasNoSnapshot => !HasSnapshot;

    /// <summary>Gets whether the completed snapshot contains files.</summary>
    public bool HasResults => Snapshot?.Files.Count > 0;

    /// <summary>Gets whether active filters match one or more files.</summary>
    public bool HasFilterResults => Page.TotalItemCount > 0;

    /// <summary>Gets whether active filters match no file in an otherwise populated snapshot.</summary>
    public bool HasNoFilterResults => HasSnapshot && HasResults && !HasFilterResults;

    /// <summary>Gets whether current state represents an empty completed scan.</summary>
    public bool IsEmptyCompletedScan => HasSnapshot && !HasResults;

    /// <summary>Gets whether source issues are available.</summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>Gets whether no source issues are available.</summary>
    public bool HasNoWarnings => !HasWarnings;

    /// <summary>Gets whether display-only planned operations are available.</summary>
    public bool HasPlannedOperations => PlannedOperations.Count > 0;

    /// <summary>Gets whether selected result details are available.</summary>
    public bool HasSelectedDetails => SelectedDetails is not null;

    /// <summary>Gets whether the previous bounded page is available.</summary>
    public bool CanGoPreviousPage => Page.PageIndex > 0;

    /// <summary>Gets whether the next bounded page is available.</summary>
    public bool CanGoNextPage => Page.TotalPageCount > 0 && Page.PageIndex < Page.TotalPageCount - 1;

    /// <summary>Gets whether exact duplicate review can open for the current snapshot.</summary>
    public bool CanOpenDuplicateReview => Snapshot?.IsDuplicateDataAvailable == true;

    /// <summary>Gets whether the explorer child surface is active.</summary>
    public bool IsExplorerVisible => DisplayMode == ResultsDisplayMode.Explorer;

    /// <summary>Gets whether the exact-duplicate child surface is active.</summary>
    public bool IsDuplicateReviewVisible => DisplayMode == ResultsDisplayMode.ExactDuplicates;

    /// <summary>Gets the current range/count text for the bounded file page.</summary>
    public string PageRangeText => Page.TotalItemCount == 0
        ? "No matching files"
        : $"Showing {Page.PageIndex * Page.PageSize + 1}–{Page.PageIndex * Page.PageSize + Page.Items.Count} of {Page.TotalItemCount} files";

    /// <summary>Gets a concise description of active result filters.</summary>
    public string ActiveQueryDescription => !HasActiveFilters()
        ? "All completed-scan results"
        : "Active filters are applied to this completed scan.";

    /// <summary>Gets the command that clears every non-default local filter.</summary>
    public IRelayCommand ClearFiltersCommand { get; }

    /// <summary>Gets the command that opens the previous bounded page.</summary>
    public IRelayCommand PreviousPageCommand { get; }

    /// <summary>Gets the command that opens the next bounded page.</summary>
    public IRelayCommand NextPageCommand { get; }

    /// <summary>Gets the command that opens the exact-duplicate child surface.</summary>
    public IRelayCommand OpenDuplicateReviewCommand { get; }

    /// <summary>Gets the command that returns from duplicate review to the file explorer.</summary>
    public IRelayCommand BackToExplorerCommand { get; }

    /// <summary>
    /// Replaces the current in-memory review state with a completed immutable snapshot.
    /// </summary>
    /// <param name="snapshot">The completed snapshot to own until a later completed scan replaces it.</param>
    /// <returns>A task that completes once the initial bounded page has been published.</returns>
    public Task LoadSnapshotAsync(ResultsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot;
        Summary = new ResultsSummary(
            snapshot.Statistics.FilesDiscovered,
            snapshot.Statistics.DirectoriesDiscovered,
            snapshot.Statistics.PlannedOperationCount,
            snapshot.Statistics.ExactDuplicateFileCount,
            snapshot.Statistics.WarningCount + snapshot.Statistics.ErrorCount);
        ReplaceSnapshotCollections(snapshot);
        Query = ResultsQuery.Default;
        Page = ResultsPage.Empty;
        SelectedRow = null;
        DisplayMode = ResultsDisplayMode.Explorer;
        DuplicateReview.LoadSnapshot(snapshot);
        OnSnapshotStateChanged();
        return RefreshAsync();
    }

    /// <summary>
    /// Starts a versioned local query evaluation and publishes only the newest result.
    /// </summary>
    /// <returns>A task that completes after the latest invocation publishes or is superseded.</returns>
    public async Task RefreshAsync()
    {
        var snapshot = Snapshot;
        if (snapshot is null)
        {
            ResetPageForNoSnapshot();
            return;
        }

        var cancellation = ReplaceQueryCancellation();
        var version = Interlocked.Increment(ref _queryVersion);
        var tagsSnapshot = _tagsByFile.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        IsLoading = true;
        StatusText = "Updating results…";
        try
        {
            var result = await Task.Run(
                () => ResultsQueryEngine.Evaluate(snapshot, Query, cancellation.Token, tagsSnapshot),
                cancellation.Token);
            if (cancellation.IsCancellationRequested || version != Volatile.Read(ref _queryVersion))
            {
                return;
            }

            Query = result.Query;
            Page = result.Page;
            _pageRows.Clear();
            foreach (var item in result.Page.Items)
            {
                _pageRows.Add(ResultsFileRow.FromResultFile(
                    item,
                    GetTags(item.Id),
                    result.Page.Matches.TryGetValue(item.Id, out var match) ? match : null));
            }

            if (SelectedRow is not null && !_pageRows.Any(row => row.FileId == SelectedRow.FileId))
            {
                SelectedRow = null;
            }

            StatusText = result.Page.TotalItemCount == 0
                ? HasResults
                    ? "No files match the active filters."
                    : "This completed scan found no files."
                : PageRangeText;
            AiSuggestions.SetContext(
                SelectedRow is null ? null : snapshot.Files.FirstOrDefault(file => file.Id == SelectedRow.FileId),
                snapshot,
                Page.Items);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (version == Volatile.Read(ref _queryVersion))
            {
                StatusText = "Results could not be updated. The last valid page remains available.";
            }
        }
        finally
        {
            if (version == Volatile.Read(ref _queryVersion))
            {
                IsLoading = false;
            }

            if (ReferenceEquals(_queryCancellation, cancellation))
            {
                _queryCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    /// <summary>
    /// Releases pending local query work.
    /// </summary>
    public void Dispose()
    {
        DuplicateReview.ShowGroupFilesRequested -= OnShowGroupFilesRequested;
        DuplicateReview.BackToExplorerRequested -= OnBackToExplorerRequested;
        AiSuggestions.TagsAccepted -= OnTagsAccepted;
        AiSuggestions.Dispose();
        _queryCancellation?.Cancel();
        _queryCancellation?.Dispose();
    }

    private void ChangeQuery(ResultsQuery query)
    {
        Query = ResultsQueryEngine.Normalize(query);
        SelectedRow = null;
        _ = RefreshAsync();
    }

    private void ReplaceSnapshotCollections(ResultsSnapshot snapshot)
    {
        _tagsByFile.Clear();
        foreach (var file in snapshot.Files)
        {
            var extensionTag = file.NormalizedExtension.TrimStart('.');
            if (extensionTag.Length > 0 && AiSuggestionValidator.TryNormalizeTags([extensionTag], out var tags, out _))
            {
                _tagsByFile[file.Id] = Array.AsReadOnly(tags.Select(tag => new TagAssociation(
                    $"tag:{file.Id}:extension:{tag.NormalizedValue}",
                    file.Id,
                    tag.DisplayName,
                    tag.NormalizedValue,
                    "File type",
                    TagSource.Deterministic,
                    TagAcceptanceState.Accepted,
                    "Derived from the scanned file extension.",
                    snapshot.ProjectedAtUtc)).ToArray());
            }
        }
        _directories.Clear();
        foreach (var directory in snapshot.Directories)
        {
            _directories.Add(directory);
        }

        _plannedOperations.Clear();
        foreach (var operation in snapshot.PlannedOperations)
        {
            _plannedOperations.Add(operation);
        }

        _warnings.Clear();
        foreach (var issue in snapshot.Issues)
        {
            _warnings.Add(issue.Message);
        }

        _extensionOptions.Clear();
        _extensionOptions.Add(new ResultsExtensionFilterOption(null, "All extensions"));
        foreach (var extension in snapshot.Files.Select(file => file.NormalizedExtension).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            _extensionOptions.Add(new ResultsExtensionFilterOption(extension, string.IsNullOrEmpty(extension) ? "No extension" : extension));
        }

        _categoryOptions.Clear();
        _categoryOptions.Add(new ResultsCategoryFilterOption(null, "All categories"));
        _categoryOptions.Add(new ResultsCategoryFilterOption(FileCategory.Unknown, "Unknown or unclassified"));
        foreach (var category in snapshot.Files
                     .Select(file => file.Category)
                     .OfType<FileCategory>()
                     .Where(category => category != FileCategory.Unknown)
                     .Distinct()
                     .Order())
        {
            _categoryOptions.Add(new ResultsCategoryFilterOption(category, category.ToString()));
        }
    }

    private void OnSnapshotStateChanged()
    {
        OnPropertyChanged(nameof(Snapshot));
        OnPropertyChanged(nameof(HasSnapshot));
        OnPropertyChanged(nameof(HasNoSnapshot));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasNoFilterResults));
        OnPropertyChanged(nameof(IsEmptyCompletedScan));
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(HasNoWarnings));
        OnPropertyChanged(nameof(HasPlannedOperations));
        OnPropertyChanged(nameof(CanOpenDuplicateReview));
        OpenDuplicateReviewCommand.NotifyCanExecuteChanged();
    }

    private void ResetPageForNoSnapshot()
    {
        Page = ResultsPage.Empty;
        _pageRows.Clear();
        SelectedRow = null;
        StatusText = "No completed scan results are available.";
        IsLoading = false;
    }

    private CancellationTokenSource ReplaceQueryCancellation()
    {
        var previous = Interlocked.Exchange(ref _queryCancellation, new CancellationTokenSource());
        previous?.Cancel();
        return _queryCancellation!;
    }

    private void UpdateSelectedDetails()
    {
        var selected = SelectedRow is null || Snapshot is null
            ? null
            : Snapshot.Files.FirstOrDefault(file => file.Id == SelectedRow.FileId);
        SelectedDetails = selected is null
            ? null
            : SelectedResultDetails.From(
                selected,
                Array.AsReadOnly(Snapshot!.PlannedOperations.Where(operation => operation.SourceFileId == selected.Id).ToArray()),
                GetTags(selected.Id));
    }

    private bool HasActiveFilters() => Query != ResultsQuery.Default;

    private bool CanClearFilters() => HasSnapshot && HasActiveFilters();

    private void ClearFilters()
    {
        var pageSize = Query.PageSize;
        ChangeQuery(ResultsQuery.Default with { PageSize = pageSize });
    }

    private void GoToPreviousPage()
    {
        if (CanGoPreviousPage)
        {
            ChangeQuery(Query with { PageIndex = Query.PageIndex - 1 });
        }
    }

    private void GoToNextPage()
    {
        if (CanGoNextPage)
        {
            ChangeQuery(Query with { PageIndex = Query.PageIndex + 1 });
        }
    }

    private void OpenDuplicateReview() => DisplayMode = ResultsDisplayMode.ExactDuplicates;

    private void BackToExplorer() => DisplayMode = ResultsDisplayMode.Explorer;

    private void OnShowGroupFilesRequested(object? sender, string groupId)
    {
        if (Snapshot?.DuplicateGroups.Any(group => string.Equals(group.GroupId, groupId, StringComparison.Ordinal)) != true)
        {
            return;
        }

        var query = ResultsQuery.Default with
        {
            PageSize = Query.PageSize,
            DuplicateGroupId = groupId,
        };
        DisplayMode = ResultsDisplayMode.Explorer;
        ChangeQuery(query);
    }

    private void OnBackToExplorerRequested(object? sender, EventArgs eventArgs) => BackToExplorer();

    private IReadOnlyList<TagAssociation> GetTags(string fileId) => _tagsByFile.TryGetValue(fileId, out var tags)
        ? tags
        : Array.Empty<TagAssociation>();

    private void OnTagsAccepted(object? sender, IReadOnlyList<TagAssociation> tags)
    {
        if (tags.Count == 0)
        {
            return;
        }

        var fileId = tags[0].FileId;
        var existing = GetTags(fileId);
        _tagsByFile[fileId] = Array.AsReadOnly(existing
            .Concat(tags)
            .GroupBy(tag => tag.NormalizedValue, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(tag => tag.NormalizedValue, StringComparer.Ordinal)
            .ToArray());
        _ = RefreshAsync();
        UpdateSelectedDetails();
    }

    private sealed class PreviewConfigurationService : IConfigurationService
    {
        public ApplicationSettings Current { get; private set; } = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken)
        {
            Current = settings;
            return Task.CompletedTask;
        }
    }
}
