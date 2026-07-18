namespace OpenSorSe.Application.Tags;

/// <summary>
/// Defines fixed bounds for application-owned user tags.
/// </summary>
public static class UserTagLimits
{
    /// <summary>Gets the maximum accepted non-deterministic tags associated with one result file.</summary>
    public const int MaximumAcceptedTagsPerFile = 12;

    /// <summary>Gets the maximum display and normalized length of one tag.</summary>
    public const int MaximumTagLength = 64;
}
