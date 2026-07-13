using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TidyMind.Core.Logging;

/// <summary>
/// Provides a centrally configured debug logging factory for application modules.
/// </summary>
public sealed class LoggingService : ILoggingService
{
    private readonly object _syncRoot = new();
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private bool _isDisposed;

    /// <inheritdoc />
    public void Initialize(LogLevel minimumLevel)
    {
        lock (_syncRoot)
        {
            ThrowIfDisposed();
            var previousFactory = _loggerFactory;
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(minimumLevel);
                builder.AddDebug();
            });

            if (previousFactory is not NullLoggerFactory)
            {
                previousFactory.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        lock (_syncRoot)
        {
            ThrowIfDisposed();
            return _loggerFactory.CreateLogger(categoryName);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_isDisposed)
            {
                return;
            }

            _loggerFactory.Dispose();
            _isDisposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
