namespace OpenSorSe.Rules.Models;

/// <summary>
/// Configures deterministic conflict-resolution behavior.
/// </summary>
/// <param name="Strategy">The strategy used to resolve conflicts.</param>
public sealed record ConflictResolutionOptions(ConflictResolutionStrategy Strategy)
{
    /// <summary>
    /// Gets the default v0.1 keep-first strategy.
    /// </summary>
    public static ConflictResolutionOptions Default { get; } = new(ConflictResolutionStrategy.KeepFirst);
}
