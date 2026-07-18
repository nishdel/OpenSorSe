using OpenSorSe.Application.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents one accepted application-owned tag for the selected result file.
/// </summary>
public sealed record ResultTagRow(
    string TagId,
    string DisplayName,
    string Category,
    string Source,
    bool IsRemovable)
{
    /// <summary>Creates a display row from a validated tag association.</summary>
    public static ResultTagRow FromAssociation(TagAssociation tag) => new(
        tag.TagId,
        tag.DisplayName,
        tag.Category,
        tag.Source switch
        {
            TagSource.Deterministic => "Derived",
            TagSource.AiSuggestion => "AI suggestion",
            TagSource.UserApproved => "User approved",
            TagSource.Preference => "Local preference",
            _ => "Application",
        },
        tag.Source != TagSource.Deterministic);
}
