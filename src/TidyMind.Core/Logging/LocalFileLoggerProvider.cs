using System.Text;
using Microsoft.Extensions.Logging;

namespace TidyMind.Core.Logging;

internal sealed class LocalFileLoggerProvider : ILoggerProvider
{
    private const string FilePrefix = "tidymind-";
    private const string FileExtension = ".log";
    private readonly LoggingOptions _options;
    private readonly LoggingStatisticsCounter _statistics;
    private readonly object _syncRoot = new();
    private bool _fileSinkUnavailable;

    public LocalFileLoggerProvider(LoggingOptions options, LoggingStatisticsCounter statistics)
    {
        _options = options;
        _statistics = statistics;
        InitializeFileSink();
    }

    public ILogger CreateLogger(string categoryName) => new LocalFileLogger(categoryName, this);

    public void Dispose()
    {
    }

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel is not LogLevel.None && logLevel >= _options.MinimumLevel;

    public void Write(LogLevel logLevel, string categoryName, string message)
    {
        _statistics.IncrementEntry(logLevel);
        if (!_options.FileLoggingEnabled || _fileSinkUnavailable)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_fileSinkUnavailable)
            {
                return;
            }

            try
            {
                var timestamp = DateTimeOffset.UtcNow;
                var filePath = Path.Combine(
                    _options.ResolvedLogDirectoryPath,
                    $"{FilePrefix}{timestamp:yyyy-MM-dd}{FileExtension}");
                var entry = $"{timestamp:O} [{logLevel}] [{categoryName}] {message}{Environment.NewLine}";
                using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(entry);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                _fileSinkUnavailable = true;
                _statistics.IncrementFileWriteFailures();
            }
        }
    }

    private void InitializeFileSink()
    {
        if (!_options.FileLoggingEnabled)
        {
            return;
        }

        try
        {
            var directoryPath = _options.ResolvedLogDirectoryPath;
            Directory.CreateDirectory(directoryPath);
            var activeFileName = GetDailyFileName(DateTimeOffset.UtcNow);
            var dailyFiles = Directory.EnumerateFiles(directoryPath, $"{FilePrefix}*{FileExtension}", SearchOption.TopDirectoryOnly)
                .Where(IsDailyLogFile)
                .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
                .ToArray();
            var activeFileExists = dailyFiles.Any(filePath => string.Equals(
                Path.GetFileName(filePath),
                activeFileName,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
            var retainedFiles = dailyFiles
                .Where(filePath => !string.Equals(
                    Path.GetFileName(filePath),
                    activeFileName,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .Skip(Math.Max(0, _options.RetainedFileCount - (activeFileExists ? 1 : 0)))
                .ToArray();
            foreach (var retainedFile in retainedFiles)
            {
                File.Delete(retainedFile);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _fileSinkUnavailable = true;
            _statistics.IncrementFileWriteFailures();
        }
    }

    private static string GetDailyFileName(DateTimeOffset timestamp) =>
        $"{FilePrefix}{timestamp:yyyy-MM-dd}{FileExtension}";

    private static bool IsDailyLogFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (!fileName.StartsWith(FilePrefix, StringComparison.Ordinal) ||
            !fileName.EndsWith(FileExtension, StringComparison.Ordinal) ||
            fileName.Length != FilePrefix.Length + "yyyy-MM-dd".Length + FileExtension.Length)
        {
            return false;
        }

        var dateText = fileName.Substring(FilePrefix.Length, "yyyy-MM-dd".Length);
        return DateOnly.TryParseExact(dateText, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _);
    }

    private sealed class LocalFileLogger(string categoryName, LocalFileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (IsEnabled(logLevel))
            {
                provider.Write(logLevel, categoryName, formatter(state, exception));
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

internal sealed class LoggingStatisticsCounter
{
    private long _criticalEntries;
    private long _debugEntries;
    private long _errorEntries;
    private long _fileWriteFailures;
    private long _informationEntries;
    private long _traceEntries;
    private long _warningEntries;

    public void IncrementEntry(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                Interlocked.Increment(ref _traceEntries);
                break;
            case LogLevel.Debug:
                Interlocked.Increment(ref _debugEntries);
                break;
            case LogLevel.Information:
                Interlocked.Increment(ref _informationEntries);
                break;
            case LogLevel.Warning:
                Interlocked.Increment(ref _warningEntries);
                break;
            case LogLevel.Error:
                Interlocked.Increment(ref _errorEntries);
                break;
            case LogLevel.Critical:
                Interlocked.Increment(ref _criticalEntries);
                break;
        }
    }

    public void IncrementFileWriteFailures() => Interlocked.Increment(ref _fileWriteFailures);

    public LoggingStatistics Snapshot() => new(
        Interlocked.Read(ref _traceEntries),
        Interlocked.Read(ref _debugEntries),
        Interlocked.Read(ref _informationEntries),
        Interlocked.Read(ref _warningEntries),
        Interlocked.Read(ref _errorEntries),
        Interlocked.Read(ref _criticalEntries),
        Interlocked.Read(ref _fileWriteFailures));
}
