namespace OpenSorSe.Core.Logging;

/// <summary>
/// Contains process-lifetime counts of entries accepted by the centralized logging service.
/// </summary>
/// <param name="TraceEntries">The number of emitted Trace entries.</param>
/// <param name="DebugEntries">The number of emitted Debug entries.</param>
/// <param name="InformationEntries">The number of emitted Information entries.</param>
/// <param name="WarningEntries">The number of emitted Warning entries.</param>
/// <param name="ErrorEntries">The number of emitted Error entries.</param>
/// <param name="CriticalEntries">The number of emitted Critical entries.</param>
/// <param name="FileWriteFailures">The number of isolated local file-sink failures.</param>
public sealed record LoggingStatistics(
    long TraceEntries,
    long DebugEntries,
    long InformationEntries,
    long WarningEntries,
    long ErrorEntries,
    long CriticalEntries,
    long FileWriteFailures)
{
    /// <summary>
    /// Gets an empty process-lifetime statistics snapshot.
    /// </summary>
    public static LoggingStatistics Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);
}
