using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Owns the read-only AI suggestion review workflow for the selected completed-scan result.
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
    private AiFileOrganizationSuggestion? _suggestion;
    private AiFolderStructurePlan? _folderStructurePlan;
    private string? _proposedFileName;
    private string _proposedTagsText = string.Empty;
    private string? _proposedCategory;
    private string? _proposedDestinationFolder;
    private string _statusText = "Select a completed-scan result to request optional local AI suggestions.";
    private bool _isBusy;
    private bool _hasContext;

    /// <summary>
    /// Initializes the suggestion-review model over the optional application service.
    /// </summary>
    /// <param name="configurationService">The centralized persisted-settings service.</param>
    /// <param name="aiSuggestionService">The optional application-owned suggestion service.</param>
    public AiSuggestionsViewModel(IConfigurationService configurationService, IAiSuggestionService? aiSuggestionService = null)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _aiSuggestionService = aiSuggestionService;
        StructureItems = new ReadOnlyObservableCollection<AiFolderStructurePlanItem>(_structureItems);
        GenerateSuggestionCommand = new AsyncRelayCommand(GenerateSuggestionAsync, CanGenerateSuggestion);
        AcceptRenameCommand = new AsyncRelayCommand(AcceptRenameAsync, CanAcceptRename);
        RejectRenameCommand = new AsyncRelayCommand(RejectRenameAsync, CanRejectRename);
        AcceptTagsCommand = new AsyncRelayCommand(AcceptTagsAsync, CanAcceptTags);
        RejectTagsCommand = new AsyncRelayCommand(RejectTagsAsync, CanRejectTags);
        AcceptCategoryCommand = new AsyncRelayCommand(AcceptCategoryAsync, CanAcceptCategory);
        RejectCategoryCommand = new AsyncRelayCommand(RejectCategoryAsync, CanRejectCategory);
        AcceptDestinationCommand = new AsyncRelayCommand(AcceptDestinationAsync, CanAcceptDestination);
        RejectDestinationCommand = new AsyncRelayCommand(RejectDestinationAsync, CanRejectDestination);
        GenerateFolderStructureCommand = new AsyncRelayCommand(GenerateFolderStructureAsync, CanGenerateFolderStructure);
        AcceptFolderStructureCommand = new AsyncRelayCommand(AcceptFolderStructureAsync, CanAcceptFolderStructure);
        RejectFolderStructureCommand = new AsyncRelayCommand(RejectFolderStructureAsync, CanRejectFolderStructure);
    }

    /// <summary>Occurs when the user accepts tags for the current in-memory result session.</summary>
    public event EventHandler<IReadOnlyList<TagAssociation>>? TagsAccepted;

    /// <summary>Gets the selected completed-scan file currently available for review.</summary>
    public ResultFile? SelectedFile => _selectedFile;

    /// <summary>Gets the current validated application-owned suggestion.</summary>
    public AiFileOrganizationSuggestion? Suggestion
    {
        get => _suggestion;
        private set
        {
            if (SetProperty(ref _suggestion, value))
            {
                OnPropertyChanged(nameof(HasSuggestion));
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets whether a validated suggestion is available for review.</summary>
    public bool HasSuggestion => Suggestion is not null;

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

    /// <summary>Gets or sets the comma-separated editable tag proposal.</summary>
    public string ProposedTagsText
    {
        get => _proposedTagsText;
        set
        {
            if (SetProperty(ref _proposedTagsText, value))
            {
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets or sets the editable category proposal.</summary>
    public string? ProposedCategory
    {
        get => _proposedCategory;
        set
        {
            if (SetProperty(ref _proposedCategory, value))
            {
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets or sets the editable relative destination proposal.</summary>
    public string? ProposedDestinationFolder
    {
        get => _proposedDestinationFolder;
        set
        {
            if (SetProperty(ref _proposedDestinationFolder, value))
            {
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets the optional provider explanation for the current suggestion.</summary>
    public string? SuggestionExplanation => Suggestion?.Explanation;

    /// <summary>Gets the current preview-only folder-structure plan.</summary>
    public AiFolderStructurePlan? FolderStructurePlan
    {
        get => _folderStructurePlan;
        private set
        {
            if (SetProperty(ref _folderStructurePlan, value))
            {
                OnPropertyChanged(nameof(HasFolderStructurePlan));
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets whether a preview-only folder-structure plan is available.</summary>
    public bool HasFolderStructurePlan => FolderStructurePlan is not null;

    /// <summary>Gets structure-plan items that cannot create folders or move files.</summary>
    public ReadOnlyObservableCollection<AiFolderStructurePlanItem> StructureItems { get; }

    /// <summary>Gets the optional provider explanation for the current structure plan.</summary>
    public string? FolderStructureExplanation => FolderStructurePlan?.Explanation;

    /// <summary>Gets the user-safe workflow state.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Gets whether one optional provider request or decision write is active.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandStates();
            }
        }
    }

    /// <summary>Gets whether an optional provider is available to this composed desktop application.</summary>
    public bool IsAvailable => _aiSuggestionService is not null;

    /// <summary>Gets whether a completed results snapshot is available for optional AI review.</summary>
    public bool HasContext
    {
        get => _hasContext;
        private set => SetProperty(ref _hasContext, value);
    }

    /// <summary>Gets the command that requests one validated organization suggestion.</summary>
    public IAsyncRelayCommand GenerateSuggestionCommand { get; }

    /// <summary>Gets the command that records acceptance or an edit of the rename proposal.</summary>
    public IAsyncRelayCommand AcceptRenameCommand { get; }

    /// <summary>Gets the command that records rejection of the rename proposal.</summary>
    public IAsyncRelayCommand RejectRenameCommand { get; }

    /// <summary>Gets the command that accepts tags into the current result session and records the decision.</summary>
    public IAsyncRelayCommand AcceptTagsCommand { get; }

    /// <summary>Gets the command that records rejection of the tag proposal.</summary>
    public IAsyncRelayCommand RejectTagsCommand { get; }

    /// <summary>Gets the command that records acceptance or an edit of the category proposal.</summary>
    public IAsyncRelayCommand AcceptCategoryCommand { get; }

    /// <summary>Gets the command that records rejection of the category proposal.</summary>
    public IAsyncRelayCommand RejectCategoryCommand { get; }

    /// <summary>Gets the command that records acceptance or an edit of the destination proposal.</summary>
    public IAsyncRelayCommand AcceptDestinationCommand { get; }

    /// <summary>Gets the command that records rejection of the destination proposal.</summary>
    public IAsyncRelayCommand RejectDestinationCommand { get; }

    /// <summary>Gets the command that requests a bounded preview-only folder-structure plan for the current result page.</summary>
    public IAsyncRelayCommand GenerateFolderStructureCommand { get; }

    /// <summary>Gets the command that records acceptance of the preview-only folder-structure plan.</summary>
    public IAsyncRelayCommand AcceptFolderStructureCommand { get; }

    /// <summary>Gets the command that records rejection of the preview-only folder-structure plan.</summary>
    public IAsyncRelayCommand RejectFolderStructureCommand { get; }

    /// <summary>
    /// Replaces the in-memory review context without reading the filesystem or retaining source paths for provider requests.
    /// </summary>
    /// <param name="selectedFile">The selected immutable result file.</param>
    /// <param name="snapshot">The owning completed scan snapshot.</param>
    /// <param name="pageFiles">The bounded result page available for a structure preview.</param>
    public void SetContext(ResultFile? selectedFile, ResultsSnapshot? snapshot, IReadOnlyList<ResultFile>? pageFiles)
    {
        HasContext = snapshot is not null;
        var fileChanged = !string.Equals(_selectedFile?.Id, selectedFile?.Id, StringComparison.Ordinal);
        _selectedFile = selectedFile;
        _pageFiles = pageFiles is null ? Array.Empty<ResultFile>() : Array.AsReadOnly(pageFiles.Take(25).ToArray());
        _existingFolderNames = snapshot is null
            ? Array.Empty<string>()
            : Array.AsReadOnly(snapshot.Directories
                .Select(directory => directory.DisplayName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Take(30)
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
            Suggestion = null;
            ProposedFileName = null;
            ProposedTagsText = string.Empty;
            ProposedCategory = null;
            ProposedDestinationFolder = null;
            StatusText = selectedFile is null
                ? "Select a completed-scan result to request optional local AI suggestions."
                : "Ready to request an optional, review-only AI suggestion.";
        }

        NotifyCommandStates();
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private async Task GenerateSuggestionAsync()
    {
        if (_aiSuggestionService is null || _selectedFile is null)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Requesting an optional local AI suggestion…";
        try
        {
            var result = await _aiSuggestionService.GenerateFileSuggestionAsync(
                new AiFileSuggestionRequest(_selectedFile, _existingFolderNames, _siblingFileNames),
                _configurationService.Current.Ai,
                CancellationToken.None);
            Suggestion = result.Suggestion;
            if (result.Suggestion is not null)
            {
                ProposedFileName = result.Suggestion.SuggestedFileName;
                ProposedTagsText = string.Join(", ", result.Suggestion.SuggestedTags.Select(tag => tag.DisplayName));
                ProposedCategory = result.Suggestion.SuggestedCategory?.ToString();
                ProposedDestinationFolder = result.Suggestion.SuggestedDestinationFolder;
                OnPropertyChanged(nameof(SuggestionExplanation));
            }

            StatusText = result.Message;
        }
        catch (OperationCanceledException)
        {
            StatusText = "The AI suggestion request was cancelled.";
        }
        catch (Exception)
        {
            StatusText = "The AI suggestion could not be recorded or validated.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GenerateFolderStructureAsync()
    {
        if (_aiSuggestionService is null || _pageFiles.Count == 0)
        {
            return;
        }

        IsBusy = true;
        StatusText = "Requesting an optional folder-structure preview…";
        try
        {
            var result = await _aiSuggestionService.GenerateFolderStructureAsync(
                new AiFolderStructureRequest(_pageFiles, _existingFolderNames),
                _configurationService.Current.Ai,
                CancellationToken.None);
            FolderStructurePlan = result.Plan;
            _structureItems.Clear();
            if (result.Plan is not null)
            {
                foreach (var item in result.Plan.Items)
                {
                    _structureItems.Add(item);
                }

                OnPropertyChanged(nameof(FolderStructureExplanation));
            }

            StatusText = result.Message;
        }
        catch (OperationCanceledException)
        {
            StatusText = "The folder-structure preview was cancelled.";
        }
        catch (Exception)
        {
            StatusText = "The folder-structure preview could not be recorded or validated.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AcceptRenameAsync()
    {
        var suggestion = Suggestion;
        if (suggestion?.SuggestedFileName is null || _selectedFile is null)
        {
            StatusText = "No rename suggestion is available.";
            return;
        }

        if (!AiSuggestionValidator.TryNormalizeFileName(ProposedFileName, _selectedFile.NormalizedExtension, _siblingFileNames, out var finalValue, out var error))
        {
            StatusText = error;
            return;
        }

        await RecordAsync(AiSuggestionDecisionKind.Rename, suggestion.SuggestedFileName, finalValue, "Rename preference recorded. No file was renamed.");
    }

    private Task RejectRenameAsync() => RecordRejectionAsync(AiSuggestionDecisionKind.Rename, Suggestion?.SuggestedFileName, "Rename suggestion rejected. No file was changed.");

    private async Task AcceptTagsAsync()
    {
        var suggestion = Suggestion;
        if (suggestion is null)
        {
            StatusText = "No tag suggestion is available.";
            return;
        }

        if (!AiSuggestionValidator.TryNormalizeTags(SplitTags(ProposedTagsText), out var tags, out var error))
        {
            StatusText = error;
            return;
        }

        var finalValue = string.Join(",", tags.Select(tag => tag.NormalizedValue));
        var suggestedValue = string.Join(",", suggestion.SuggestedTags.Select(tag => tag.NormalizedValue));
        await RecordAsync(AiSuggestionDecisionKind.Tags, suggestedValue, finalValue, "Tags accepted for this in-memory result session. They do not change the file.");
        TagsAccepted?.Invoke(this, AiSuggestionValidator.CreateAcceptedTagAssociations(suggestion.FileId, tags, suggestion.Explanation, DateTimeOffset.UtcNow));
    }

    private Task RejectTagsAsync() => RecordRejectionAsync(
        AiSuggestionDecisionKind.Tags,
        Suggestion is null ? null : string.Join(",", Suggestion.SuggestedTags.Select(tag => tag.NormalizedValue)),
        "Tag suggestion rejected.");

    private async Task AcceptCategoryAsync()
    {
        var suggestion = Suggestion;
        var suggestedCategory = suggestion?.SuggestedCategory;
        if (suggestedCategory is null)
        {
            StatusText = "No category suggestion is available.";
            return;
        }

        if (!AiSuggestionValidator.TryParseCategory(ProposedCategory, out var category, out var error) || category is null)
        {
            StatusText = error;
            return;
        }

        await RecordAsync(AiSuggestionDecisionKind.Category, suggestedCategory.Value.ToString(), category.Value.ToString(), "Category preference recorded. The deterministic scan classification was not changed.");
    }

    private Task RejectCategoryAsync() => RecordRejectionAsync(AiSuggestionDecisionKind.Category, Suggestion?.SuggestedCategory?.ToString(), "Category suggestion rejected.");

    private async Task AcceptDestinationAsync()
    {
        var suggestion = Suggestion;
        if (suggestion?.SuggestedDestinationFolder is null)
        {
            StatusText = "No destination suggestion is available.";
            return;
        }

        if (!AiSuggestionValidator.TryNormalizeDestinationFolder(ProposedDestinationFolder, out var destination, out var error) || destination is null)
        {
            StatusText = error;
            return;
        }

        await RecordAsync(AiSuggestionDecisionKind.DestinationFolder, suggestion.SuggestedDestinationFolder, destination, "Destination preference recorded. No folder was created and no file was moved.");
    }

    private Task RejectDestinationAsync() => RecordRejectionAsync(AiSuggestionDecisionKind.DestinationFolder, Suggestion?.SuggestedDestinationFolder, "Destination suggestion rejected.");

    private Task AcceptFolderStructureAsync() => RecordFolderStructureAsync(AiSuggestionDecisionOutcome.Accepted, "Folder-structure preference recorded. No folders were created and no files were moved.");

    private Task RejectFolderStructureAsync() => RecordFolderStructureAsync(AiSuggestionDecisionOutcome.Rejected, "Folder-structure preview rejected.");

    private async Task RecordFolderStructureAsync(AiSuggestionDecisionOutcome outcome, string message)
    {
        if (FolderStructurePlan is null || _aiSuggestionService is null)
        {
            return;
        }

        var value = string.Join(";", FolderStructurePlan.Items.Select(item => $"{item.FileId}:{item.DestinationFolder}"));
        await RecordDecisionAsync(new AiSuggestionDecision(
            AiSuggestionDecisionKind.FolderStructure,
            outcome,
            null,
            value,
            outcome == AiSuggestionDecisionOutcome.Rejected ? null : value,
            FolderStructurePlan.Provider,
            FolderStructurePlan.Model,
            DateTimeOffset.UtcNow), message);
    }

    private Task RecordRejectionAsync(AiSuggestionDecisionKind kind, string? suggestedValue, string message)
    {
        if (Suggestion is null || string.IsNullOrWhiteSpace(suggestedValue))
        {
            StatusText = "No suggestion is available to reject.";
            return Task.CompletedTask;
        }

        return RecordDecisionAsync(new AiSuggestionDecision(
            kind,
            AiSuggestionDecisionOutcome.Rejected,
            _selectedFile?.NormalizedExtension,
            suggestedValue,
            null,
            Suggestion.Provider,
            Suggestion.Model,
            DateTimeOffset.UtcNow), message);
    }

    private Task RecordAsync(AiSuggestionDecisionKind kind, string suggestedValue, string finalValue, string message)
    {
        if (Suggestion is null)
        {
            return Task.CompletedTask;
        }

        var outcome = string.Equals(suggestedValue, finalValue, StringComparison.Ordinal)
            ? AiSuggestionDecisionOutcome.Accepted
            : AiSuggestionDecisionOutcome.Edited;
        return RecordDecisionAsync(new AiSuggestionDecision(
            kind,
            outcome,
            _selectedFile?.NormalizedExtension,
            suggestedValue,
            finalValue,
            Suggestion.Provider,
            Suggestion.Model,
            DateTimeOffset.UtcNow), message);
    }

    private async Task RecordDecisionAsync(AiSuggestionDecision decision, string message)
    {
        if (_aiSuggestionService is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _aiSuggestionService.RecordDecisionAsync(decision, CancellationToken.None);
            StatusText = message;
        }
        catch (Exception)
        {
            StatusText = "The review decision could not be saved locally. No file was changed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanGenerateSuggestion() => !IsBusy && _aiSuggestionService is not null && _selectedFile is not null;

    private bool CanAcceptRename() => !IsBusy && Suggestion?.SuggestedFileName is not null;

    private bool CanRejectRename() => !IsBusy && Suggestion?.SuggestedFileName is not null;

    private bool CanAcceptTags() => !IsBusy && Suggestion?.SuggestedTags.Count > 0;

    private bool CanRejectTags() => !IsBusy && Suggestion?.SuggestedTags.Count > 0;

    private bool CanAcceptCategory() => !IsBusy && Suggestion?.SuggestedCategory is not null;

    private bool CanRejectCategory() => !IsBusy && Suggestion?.SuggestedCategory is not null;

    private bool CanAcceptDestination() => !IsBusy && Suggestion?.SuggestedDestinationFolder is not null;

    private bool CanRejectDestination() => !IsBusy && Suggestion?.SuggestedDestinationFolder is not null;

    private bool CanGenerateFolderStructure() => !IsBusy && _aiSuggestionService is not null && _pageFiles.Count > 0;

    private bool CanAcceptFolderStructure() => !IsBusy && FolderStructurePlan is not null;

    private bool CanRejectFolderStructure() => !IsBusy && FolderStructurePlan is not null;

    private void NotifyCommandStates()
    {
        GenerateSuggestionCommand.NotifyCanExecuteChanged();
        AcceptRenameCommand.NotifyCanExecuteChanged();
        RejectRenameCommand.NotifyCanExecuteChanged();
        AcceptTagsCommand.NotifyCanExecuteChanged();
        RejectTagsCommand.NotifyCanExecuteChanged();
        AcceptCategoryCommand.NotifyCanExecuteChanged();
        RejectCategoryCommand.NotifyCanExecuteChanged();
        AcceptDestinationCommand.NotifyCanExecuteChanged();
        RejectDestinationCommand.NotifyCanExecuteChanged();
        GenerateFolderStructureCommand.NotifyCanExecuteChanged();
        AcceptFolderStructureCommand.NotifyCanExecuteChanged();
        RejectFolderStructureCommand.NotifyCanExecuteChanged();
    }

    private static IReadOnlyList<string> SplitTags(string value) => Array.AsReadOnly(value.Split([',', ';', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
}
