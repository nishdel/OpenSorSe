namespace TidyMind.Rules.Models;

/// <summary>
/// Defines the deterministic policy used to resolve intra-plan conflicts.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Accepts the earliest valid operation and rejects later conflicting operations.</summary>
    KeepFirst,
}
