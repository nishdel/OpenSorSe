namespace OpenSorSe.Application.Models;

/// <summary>
/// Identifies the source of an application-owned tag association.
/// </summary>
public enum TagSource
{
    /// <summary>The application derived the tag deterministically.</summary>
    Deterministic,
    /// <summary>An optional AI provider proposed the tag.</summary>
    AiSuggestion,
    /// <summary>The user explicitly accepted or created the tag.</summary>
    UserApproved,
    /// <summary>A local approved-pattern preference influenced the tag.</summary>
    Preference,
}

/// <summary>
/// Identifies whether a tag is only suggested or is available to the current result-search session.
/// </summary>
public enum TagAcceptanceState
{
    /// <summary>The tag has not been accepted into the result session.</summary>
    Suggested,
    /// <summary>The tag was accepted for the current in-memory result session.</summary>
    Accepted,
    /// <summary>The tag was explicitly rejected.</summary>
    Rejected,
}

/// <summary>
/// Represents one normalized tag associated with a file in the current in-memory result session.
/// </summary>
public sealed record TagAssociation(
    string TagId,
    string FileId,
    string DisplayName,
    string NormalizedValue,
    string Category,
    TagSource Source,
    TagAcceptanceState AcceptanceState,
    string? Explanation,
    DateTimeOffset CreatedAtUtc);
