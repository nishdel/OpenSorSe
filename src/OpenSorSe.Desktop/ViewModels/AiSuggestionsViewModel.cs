using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Owns the non-mutating AI suggestion review workflow for known completed-scan results.
/// </summary>
public sealed class AiSuggestionsViewModel : ViewModelBase, IDisposable
{
    private readonly IAiSuggestionService? _aiSuggestionService;
    private readonly IConfigurationService _configurationService;
    private readonly ObservableCollection<AiFolderStructurePlanItem> _structureItems = [];
    private IReadOnlyList<string> _existingFolderNames = Array.Empty<string>();
    private IReadOnlyList<string> _siblingFileNames = Array.Empty<string>();
    private IReadOnlyList<ResultFile> _pageFiles = Array.Empty<ResultFile>();
    private ResultFile? _selectedFile;
    private AiFileRenameSuggestion? _renameSuggestion;
    private AiFolderStructurePlan? _folderStructurePlan;
    private string? _proposedFileName;
    private string _statusText = "Enable an AI capability in Settings to request review-only suggestions.";
    private bool _isBusy;
    private bool _hasContext;
    private CancellationTokenSource? _operationCancellation;
    private long _operationVersion;
    private bool _isDisposed;

    /// <summary>Initializes the suggestion-review model over the optional application service.</summary>
    public AiSuggestionsViewModel(IConfigurationService configurationService, IAiSuggestionService? aiSuggestionService = null)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _aiSuggestionService = aiSuggestionService;
        StructureItems = new ReadOnlyObservableCollection<AiFolderStructurePlanItem>(_structureItems);
        GenerateSuggestionCommand = new AsyncRelayCommand(GenerateRenameAsync, CanGenerateRename);
        AcceptRenameCommand = new AsyncRelayCommand(() => RecordRenameAsync(AiSuggestionDecisionOutcome.Accepted), CanReviewRename);
        RejectRenameCommand = new AsyncRelayCommand(() => RecordRenameAsync(AiSuggestionDecisionOutcome.Rejected), CanReviewRename);
        GenerateFolderStructureCommand = new AsyncRelayCommand(GenerateFolderStructureAsync, CanGenerateFolderStructure);
        AcceptFolderStructureCommand = new AsyncRelayCommand(() => RecordFolderStructureAsync(AiSuggestionDecisionOutcome.Accepted), CanReviewFolderStructure);
        RejectFolderStructureCommand = new AsyncRelayCommand(() => RecordFolderStructureAsync(AiSuggestionDecisionOutcome.Rejected), CanReviewFolderStructure);
        CancelAiOperationCommand = new RelayCommand(CancelOperation, () => IsBusy);
        RefreshFeatureAvailability();
    }

    /// <summary>Gets the selected completed-scan file currently available for review.</summary>
    public ResultFile? SelectedFile => _selectedFile;

    /// <summary>Gets whether any enabled AI capability should be presented.</summary>
    public bool IsVisible => HasContext && (IsFileRenameVisible || IsFolderStructureVisible);

    /// <summary>Gets whether the rename capability is enabled and available.</summary>
    public bool IsFileRenameVisible =>
        _aiSuggestionService is not null && _configurationService.Current.Ai.IsCapabilityEnabled(AiCapability.FileRenameSuggestions);

    /// <summary>Gets whether the folder-structure capability is enabled and available.</summary>
    public bool IsFolderStructureVisible =>
        _aiSuggestionService is not null && _configurationService.Current.Ai.IsCapabilityEnabled(AiCapability.FolderStructureSuggestions);

    /// <summary>Gets the current validated rename proposal.</summary>
    public AiFileRenameSuggestion? RenameSuggestion
    {
        get => _renameSuggestion;
        private set
        {
            if (SetProperty(ref _renameSuggestion, value))
            {
                OnPropertyChanged(nameof(HasRenameSuggestion));
                OnPropertyChanged(nameof(RenameReason));
                OnPropertyChanged(nameof(RenameConfidenceText));
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets whether one completely validated rename proposal is available.</summary>
    public bool HasRenameSuggestion => RenameSuggestion is not null;

    /// <summary>Gets or sets the editable rename proposal. It is never applied to a file by this view model.</summary>
    public string? ProposedFileName
    {
        get => _proposedFileName;
        set
        {
            if (SetProperty(ref _proposedFileName, value))
            {
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets the bounded model reason for the rename proposal.</summary>
    public string? RenameReason => RenameSuggestion?.Reason;

    /// <summary>Gets a non-certain description of the optional model confidence.</summary>
    public string RenameConfidenceText => RenameSuggestion?.Confidence is { } confidence
        ? $"Model confidence estimate: {confidence:P0}. This is not certainty."
        : "The model did not provide a confidence estimate.";

    /// <summary>Gets the current preview-only folder-structure plan.</summary>
    public AiFolderStructurePlan? FolderStructurePlan
    {
        get => _folderStructurePlan;
        private set
        {
            if (SetProperty(ref _folderStructurePlan, value))
            {
                OnPropertyChanged(nameof(HasFolderStructurePlan));
                OnPropertyChanged(nameof(FolderStructureReason));
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets whether a completely validated preview-only folder plan is available.</summary>
    public bool HasFolderStructurePlan => FolderStructurePlan is not null;

    /// <summary>Gets validated known-file assignments for the current logical hierarchy.</summary>
    public ReadOnlyObservableCollection<AiFolderStructurePlanItem> StructureItems { get; }

    /// <summary>Gets the bounded model reason for the logical hierarchy.</summary>
    public string? FolderStructureReason => FolderStructurePlan?.Reason;

    /// <summary>Gets the user-safe workflow status.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Gets whether one explicit provider or local-review operation is active.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                CancelAiOperationCommand.NotifyCanExecuteChanged();
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets whether a completed Results snapshot is available.</summary>
    public bool HasContext
    {
        get => _hasContext;
        private set
        {
            if (SetProperty(ref _hasContext, value))
            {
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    /// <summary>Gets the command that requests one validated filename proposal.</summary>
    public IAsyncRelayCommand GenerateSuggestionCommand { get; }

    /// <summary>Gets the command that records acceptance or an edit without renaming a file.</summary>
    public IAsyncRelayCommand AcceptRenameCommand { get; }

    /// <summary>Gets the command that records rejection without changing a file.</summary>
    public IAsyncRelayCommand RejectRenameCommand { get; }

    /// <summary>Gets the command that requests a bounded preview-only hierarchy.</summary>
    public IAsyncRelayCommand GenerateFolderStructureCommand { get; }

    /// <summary>Gets the command that records acceptance without creating folders or moving files.</summary>
    public IAsyncRelayCommand AcceptFolderStructureCommand { get; }

    /// <summary>Gets the command that records rejection of a logical hierarchy.</summary>
    public IAsyncRelayCommand RejectFolderStructureCommand { get; }

    /// <summary>Gets the command that cancels the active explicit AI operation.</summary>
    public IRelayCommand CancelAiOperationCommand { get; }

    /// <summary>Replaces in-memory review context without reading file content or retaining paths for provider requests.</summary>
    public void SetContext(ResultFile? selectedFile, ResultsSnapshot? snapshot, IReadOnlyList<ResultFile>? pageFiles)
    {
        if (IsBusy)
        {
            CancelOperation();
        }

        HasContext = snapshot is not null;
        var fileChanged = !string.Equals(_selectedFile?.Id, selectedFile?.Id, StringComparison.Ordinal);
        _selectedFile = selectedFile;
        _pageFiles = pageFiles is null ? Array.Empty<ResultFile>() : Array.AsReadOnly(pageFiles.ToArray());
        _existingFolderNames = snapshot is null
            ? Array.Empty<string>()
            : Array.AsReadOnly(snapshot.Directories
                .Select(directory => directory.DisplayName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray());
        _siblingFileNames = selectedFile is null || snapshot is null
            ? Array.Empty<string>()
            : Array.AsReadOnly(snapshot.Files
                .Where(file => !string.Equals(file.Id, selectedFile.Id, StringComparison.Ordinal) &&
                               string.Equals(Path.GetDirectoryName(file.FullPath), Path.GetDirectoryName(selectedFile.FullPath), StringComparison.OrdinalIgnoreCase))
                .Select(file => file.DisplayFileName)
                .ToArray());

        if (fileChanged)
        {
            RenameSuggestion = null;
            ProposedFileName = null;
        }

        FolderStructurePlan = null;
        _structureItems.Clear();

        StatusText = IsVisible
            ? "AI capabilities are ready for an explicit review-only request."
            : "Enable an AI capability in Settings to request review-only suggestions.";
        NotifyCommandStates();
    }

    /// <summary>Refreshes visibility and command gates after active settings change.</summary>
    public void RefreshFeatureAvailability()
    {
        if (!IsFileRenameVisible)
        {
            RenameSuggestion = null;
            ProposedFileName = null;
        }

        if (!IsFolderStructureVisible)
        {
            FolderStructurePlan = null;
            _structureItems.Clear();
        }

        if (!IsFileRenameVisible && !IsFolderStructureVisible)
        {
            CancelOperation();
        }

        OnPropertyChanged(nameof(IsFileRenameVisible));
        OnPropertyChanged(nameof(IsFolderStructureVisible));
        OnPropertyChanged(nameof(IsVisible));
        NotifyCommandStates();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        CancelOperation();
        _operationCancellation?.Dispose();
        _isDisposed = true;
    }

    private async Task GenerateRenameAsync()
    {
        if (_aiSuggestionService is null || _selectedFile is null || !IsFileRenameVisible)
        {
            return;
        }

        var (cancellation, version) = BeginOperation();
        StatusText = "Requesting an AI-generated rename suggestion...";
        try
        {
            var result = await _aiSuggestionService.GenerateFileRenameAsync(
                new AiFileRenameRequest(_selectedFile, _siblingFileNames),
                _configurationService.Current.Ai,
                cancellation.Token);
            if (!IsCurrentOperation(cancellation, version))
            {
                return;
            }

            RenameSuggestion = result.Suggestion;
            ProposedFileName = result.Suggestion?.SuggestedFileName;
            StatusText = result.Message;
        }
        catch (OperationCanceledException)
        {
            if (version == Volatile.Read(ref _operationVersion))
            {
                StatusText = "The AI rename request was cancelled.";
            }
        }
        catch (Exception)
        {
            if (version == Volatile.Read(ref _operationVersion))
            {
                StatusText = "The AI rename request failed safely. No file was changed.";
            }
        }
        finally
        {
            EndOperation(cancellation, version);
        }
    }

    private async Task GenerateFolderStructureAsync()
    {
        if (_aiSuggestionService is null || _pageFiles.Count == 0 || !IsFolderStructureVisible)
        {
            return;
        }

        var (cancellation, version) = BeginOperation();
        StatusText = "Requesting an AI-generated folder-structure suggestion...";
        try
        {
            var result = await _aiSuggestionService.GenerateFolderStructureAsync(
                new AiFolderStructureRequest(_pageFiles, _existingFolderNames),
                _configurationService.Current.Ai,
                cancellation.Token);
            if (!IsCurrentOperation(cancellation, version))
            {
                return;
            }

            FolderStructurePlan = result.Plan;
            _structureItems.Clear();
            if (result.Plan is not null)
            {
                foreach (var item in result.Plan.Items)
                {
                    _structureItems.Add(item);
                }
            }

            StatusText = result.Message;
        }
        catch (OperationCanceledException)
        {
            if (version == Volatile.Read(ref _operationVersion))
            {
                StatusText = "The AI folder-structure request was cancelled.";
            }
        }
        catch (Exception)
        {
            if (version == Volatile.Read(ref _operationVersion))
            {
                StatusText = "The AI folder-structure request failed safely. No folder or file was changed.";
            }
        }
        finally
        {
            EndOperation(cancellation, version);
        }
    }

    private async Task RecordRenameAsync(AiSuggestionDecisionOutcome requestedOutcome)
    {
        var suggestion = RenameSuggestion;
        if (_aiSuggestionService is null || suggestion is null || _selectedFile is null || !IsFileRenameVisible)
        {
            return;
        }

        var outcome = requestedOutcome;
        string? finalValue = null;
        if (requestedOutcome != AiSuggestionDecisionOutcome.Rejected)
        {
            if (!AiSuggestionValidator.TryNormalizeFileName(
                    ProposedFileName,
                    _selectedFile.NormalizedExtension,
                    _siblingFileNames,
                    out var normalized,
                    out var error))
            {
                StatusText = error;
                return;
            }

            if (string.Equals(normalized, _selectedFile.DisplayFileName, StringComparison.OrdinalIgnoreCase))
            {
                StatusText = "The reviewed filename does not propose a change. No decision was saved and no file was changed.";
                return;
            }

            finalValue = normalized;
            outcome = string.Equals(normalized, suggestion.SuggestedFileName, StringComparison.Ordinal)
                ? AiSuggestionDecisionOutcome.Accepted
                : AiSuggestionDecisionOutcome.Edited;
        }

        var result = await _aiSuggestionService.RecordDecisionAsync(
            new AiSuggestionDecision(
                AiSuggestionDecisionKind.Rename,
                outcome,
                _selectedFile.NormalizedExtension,
                suggestion.SuggestedFileName,
                finalValue,
                suggestion.Provider,
                suggestion.Model,
                DateTimeOffset.UtcNow),
            _configurationService.Current.Ai,
            CancellationToken.None);
        StatusText = outcome == AiSuggestionDecisionOutcome.Rejected && result.State == AiAvailabilityState.ModelSelected
            ? "The AI-generated rename suggestion was rejected. No file was changed."
            : result.Message;
    }

    private async Task RecordFolderStructureAsync(AiSuggestionDecisionOutcome outcome)
    {
        var plan = FolderStructurePlan;
        if (_aiSuggestionService is null || plan is null || !IsFolderStructureVisible)
        {
            return;
        }

        var value = string.Join(';', plan.Folders.Select(folder => folder.Name).Distinct(StringComparer.OrdinalIgnoreCase));
        var result = await _aiSuggestionService.RecordDecisionAsync(
            new AiSuggestionDecision(
                AiSuggestionDecisionKind.FolderStructure,
                outcome,
                null,
                value,
                outcome == AiSuggestionDecisionOutcome.Rejected ? null : value,
                plan.Provider,
                plan.Model,
                DateTimeOffset.UtcNow),
            _configurationService.Current.Ai,
            CancellationToken.None);
        StatusText = outcome == AiSuggestionDecisionOutcome.Rejected && result.State == AiAvailabilityState.ModelSelected
            ? "The AI-generated folder-structure suggestion was rejected. No folder or file was changed."
            : result.Message;
    }

    private bool CanGenerateRename() =>
        !IsBusy && IsFileRenameVisible && _selectedFile is not null;

    private bool CanReviewRename() =>
        !IsBusy && IsFileRenameVisible && RenameSuggestion is not null;

    private bool CanGenerateFolderStructure() =>
        !IsBusy && IsFolderStructureVisible && _pageFiles.Count > 0;

    private bool CanReviewFolderStructure() =>
        !IsBusy && IsFolderStructureVisible && FolderStructurePlan is not null;

    private void NotifyCommandStates()
    {
        GenerateSuggestionCommand.NotifyCanExecuteChanged();
        AcceptRenameCommand.NotifyCanExecuteChanged();
        RejectRenameCommand.NotifyCanExecuteChanged();
        GenerateFolderStructureCommand.NotifyCanExecuteChanged();
        AcceptFolderStructureCommand.NotifyCanExecuteChanged();
        RejectFolderStructureCommand.NotifyCanExecuteChanged();
    }

    private (CancellationTokenSource Cancellation, long Version) BeginOperation()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, cancellation);
        previous?.Cancel();
        previous?.Dispose();
        var version = Interlocked.Increment(ref _operationVersion);
        IsBusy = true;
        return (cancellation, version);
    }

    private bool IsCurrentOperation(CancellationTokenSource cancellation, long version) =>
        !cancellation.IsCancellationRequested &&
        ReferenceEquals(_operationCancellation, cancellation) &&
        version == Volatile.Read(ref _operationVersion);

    private void EndOperation(CancellationTokenSource cancellation, long version)
    {
        if (ReferenceEquals(_operationCancellation, cancellation))
        {
            _operationCancellation = null;
        }

        if (version == Volatile.Read(ref _operationVersion))
        {
            IsBusy = false;
        }

        cancellation.Dispose();
    }

    private void CancelOperation()
    {
        var cancellation = Interlocked.Exchange(ref _operationCancellation, null);
        if (cancellation is null)
        {
            return;
        }

        Interlocked.Increment(ref _operationVersion);
        cancellation.Cancel();
        cancellation.Dispose();
        IsBusy = false;
        StatusText = "The active AI operation was cancelled. No file or folder was changed.";
    }
}
