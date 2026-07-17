namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Represents an immutable user request to scan normalized folder roots.
/// </summary>
/// <param name="FolderPaths">The validated folder roots in user selection order.</param>
public sealed record ScanRequest(IReadOnlyList<string> FolderPaths);
