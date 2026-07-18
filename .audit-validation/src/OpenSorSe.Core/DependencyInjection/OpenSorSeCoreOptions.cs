namespace OpenSorSe.Core.DependencyInjection;

/// <summary>
/// Defines composition settings required to register the shared Core foundation.
/// </summary>
public sealed class OpenSorSeCoreOptions
{
    /// <summary>
    /// Gets or initializes the absolute path of the user settings file.
    /// </summary>
    public string ConfigurationFilePath { get; init; } = string.Empty;
}
