namespace OpenSorSe.Application.AI;

/// <summary>
/// Defines fixed privacy and capacity bounds for application-owned AI decision history.
/// </summary>
public static class AiDecisionHistoryLimits
{
    /// <summary>Gets the maximum retained user-review decisions.</summary>
    public const int MaximumDecisionCount = 1_000;

    /// <summary>Gets the maximum stored suggestion or final-value length.</summary>
    public const int MaximumValueLength = 512;

    /// <summary>Gets the maximum provider or model identifier length.</summary>
    public const int MaximumProviderIdentifierLength = 128;

    /// <summary>Gets the maximum optional extension length.</summary>
    public const int MaximumExtensionLength = 32;

    /// <summary>Gets the maximum encoded size of the complete decision-history file.</summary>
    public const long MaximumHistoryFileBytes = 4L * 1024 * 1024;
}
