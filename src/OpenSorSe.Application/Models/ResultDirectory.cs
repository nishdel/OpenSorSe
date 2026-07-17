namespace OpenSorSe.Application.Models;

/// <summary>
/// Represents one immutable, display-safe directory result in a completed processing snapshot.
/// </summary>
public sealed record ResultDirectory(string FullPath, string DisplayName);
