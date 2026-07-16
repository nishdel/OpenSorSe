namespace TidyMind.Scanner.Models;

/// <summary>
/// Defines metadata used to match a classification rule.
/// </summary>
public enum FileClassificationMatchKind
{
    /// <summary>Matches the complete metadata filename.</summary>
    ExactFileName,
    /// <summary>Matches the metadata extension.</summary>
    Extension,
}
