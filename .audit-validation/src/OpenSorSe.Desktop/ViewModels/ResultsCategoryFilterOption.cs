using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Represents one display-safe deterministic-category filter option.
/// </summary>
public sealed record ResultsCategoryFilterOption(FileCategory? Value, string DisplayName);
