namespace OpenSorSe.Core.Logging;

/// <summary>
/// Defines fixed capacity bounds for application-owned diagnostic output.
/// </summary>
public static class LoggingLimits
{
    /// <summary>Gets the maximum encoded size of one UTC daily diagnostic log.</summary>
    public const long MaximumDailyFileBytes = 10L * 1024 * 1024;
}
