using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Maintains validated scan-root selection and emits non-executing scan requests.
/// </summary>
public sealed class FolderSelectionViewModel : ViewModelBase
{
    private const int RecentFolderLimit = 5;
    private readonly StringComparer _pathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private readonly ObservableCollection<string> _recentFolders = [];
    private readonly ObservableCollection<string> _selectedFolders = [];
    private string? _folderPathInput;
    private string? _selectedFolder;
    private string _statusText = "Ready";

    /// <summary>
    /// Initializes folder-selection commands.
    /// </summary>
    public FolderSelectionViewModel()
    {
        SelectedFolders = new ReadOnlyObservableCollection<string>(_selectedFolders);
        RecentFolders = new ReadOnlyObservableCollection<string>(_recentFolders);
        AddFolderCommand = new RelayCommand(AddFolderFromInput);
        RemoveSelectedFolderCommand = new RelayCommand(() => _ = RemoveSelectedFolder());
        StartScanCommand = new RelayCommand(RequestScan);
    }

    /// <summary>
    /// Occurs when the user requests a scan of the current validated folders.
    /// </summary>
    public event EventHandler<ScanRequest>? ScanRequested;

    /// <summary>
    /// Gets the selected folder roots in user selection order.
    /// </summary>
    public ReadOnlyObservableCollection<string> SelectedFolders { get; }

    /// <summary>
    /// Gets recently added folder roots for the current application process.
    /// </summary>
    public ReadOnlyObservableCollection<string> RecentFolders { get; }

    /// <summary>
    /// Gets or sets the manually entered folder path awaiting validation.
    /// </summary>
    public string? FolderPathInput
    {
        get => _folderPathInput;
        set => SetProperty(ref _folderPathInput, value);
    }

    /// <summary>
    /// Gets or sets the folder currently selected for removal.
    /// </summary>
    public string? SelectedFolder
    {
        get => _selectedFolder;
        set => SetProperty(ref _selectedFolder, value);
    }

    /// <summary>
    /// Gets the current user-safe validation or request status.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the command that validates and adds the currently entered folder path.
    /// </summary>
    public IRelayCommand AddFolderCommand { get; }

    /// <summary>
    /// Gets the command that removes the currently selected folder root.
    /// </summary>
    public IRelayCommand RemoveSelectedFolderCommand { get; }

    /// <summary>
    /// Gets the command that emits a non-executing scan request.
    /// </summary>
    public IRelayCommand StartScanCommand { get; }

    /// <summary>
    /// Validates and adds a folder root when it exists and has not already been selected.
    /// </summary>
    /// <param name="folderPath">The user-selected folder path.</param>
    /// <returns><see langword="true"/> when the folder was added; otherwise, <see langword="false"/>.</returns>
    public bool AddFolder(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Path.IsPathRooted(folderPath))
        {
            StatusText = "Select an absolute folder path.";
            return false;
        }

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(folderPath);
        }
        catch (ArgumentException)
        {
            StatusText = "The folder path is invalid.";
            return false;
        }

        if (!Directory.Exists(normalizedPath))
        {
            StatusText = "The selected folder is unavailable.";
            return false;
        }

        if (_selectedFolders.Any(existingPath => _pathComparer.Equals(existingPath, normalizedPath)))
        {
            StatusText = "The folder is already selected.";
            return false;
        }

        _selectedFolders.Add(normalizedPath);
        AddRecentFolder(normalizedPath);
        FolderPathInput = string.Empty;
        StatusText = "Folder added.";
        return true;
    }

    /// <summary>
    /// Removes the currently selected folder root when it belongs to the selection.
    /// </summary>
    /// <returns><see langword="true"/> when a folder was removed; otherwise, <see langword="false"/>.</returns>
    public bool RemoveSelectedFolder()
    {
        if (SelectedFolder is null)
        {
            StatusText = "Select a folder to remove.";
            return false;
        }

        var selectedPath = _selectedFolders.FirstOrDefault(path => _pathComparer.Equals(path, SelectedFolder));
        if (selectedPath is null)
        {
            StatusText = "The selected folder is unavailable.";
            return false;
        }

        _selectedFolders.Remove(selectedPath);
        SelectedFolder = null;
        StatusText = "Folder removed.";
        return true;
    }

    /// <summary>
    /// Emits the current validated folder selection without performing a scan.
    /// </summary>
    public void RequestScan()
    {
        if (_selectedFolders.Count == 0)
        {
            StatusText = "Select at least one folder before starting a scan.";
            return;
        }

        ScanRequested?.Invoke(this, new ScanRequest(_selectedFolders.ToArray()));
        StatusText = "Scan request created.";
    }

    private void AddFolderFromInput() => AddFolder(FolderPathInput);

    private void AddRecentFolder(string normalizedPath)
    {
        var existingPath = _recentFolders.FirstOrDefault(path => _pathComparer.Equals(path, normalizedPath));
        if (existingPath is not null)
        {
            _recentFolders.Remove(existingPath);
        }

        _recentFolders.Insert(0, normalizedPath);
        while (_recentFolders.Count > RecentFolderLimit)
        {
            _recentFolders.RemoveAt(_recentFolders.Count - 1);
        }
    }
}
