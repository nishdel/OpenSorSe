namespace TidyMind.Core.Configuration;

/// <summary>
/// Loads, validates, exposes, and persists application configuration.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current validated settings.
    /// </summary>
    ApplicationSettings Current { get; }

    /// <summary>
    /// Loads settings from their configured sources and validates the result.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels loading before completion.</param>
    /// <returns>A task that completes when settings are ready for use.</returns>
    Task InitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists the current settings to the user configuration file.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels saving before completion.</param>
    /// <returns>A task that completes when settings have been persisted.</returns>
    Task SaveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Validates and persists replacement application settings to the configured user settings file.
    /// </summary>
    /// <param name="settings">The replacement settings to persist.</param>
    /// <param name="cancellationToken">A token that cancels saving before completion.</param>
    /// <returns>A task that completes when the replacement settings have been persisted.</returns>
    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken);
}
