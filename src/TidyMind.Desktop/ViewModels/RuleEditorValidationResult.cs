namespace TidyMind.Desktop.ViewModels;

/// <summary>
/// Contains user-safe validation feedback for an in-memory rule edit.
/// </summary>
/// <param name="IsValid">Whether the supplied rule can be retained in the in-memory editor.</param>
/// <param name="Errors">Validation messages in deterministic check order.</param>
public sealed record RuleEditorValidationResult(bool IsValid, IReadOnlyList<string> Errors);
