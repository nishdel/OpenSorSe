namespace TidyMind.Core.Configuration;

/// <summary>
/// Represents an invalid application configuration value.
/// </summary>
public sealed class ConfigurationValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
    /// </summary>
    /// <param name="message">A description of the invalid configuration.</param>
    public ConfigurationValidationException(string message)
        : base(message)
    {
    }
}
