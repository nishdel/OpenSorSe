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

    /// <summary>
    /// Initializes a configuration validation exception with a non-user-facing underlying failure.
    /// </summary>
    /// <param name="message">The user-safe validation message.</param>
    /// <param name="innerException">The underlying configuration failure.</param>
    public ConfigurationValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
