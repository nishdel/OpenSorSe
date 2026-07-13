using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TidyMind.Core.Logging;

namespace TidyMind.Core.Tasks;

/// <summary>
/// Tracks independent background operations without owning their business logic.
/// </summary>
public sealed class TaskManager : ITaskManager
{
    private readonly ConcurrentDictionary<Guid, TaskExecution> _activeTasks = new();
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initializes a task manager that reports task failures through centralized logging.
    /// </summary>
    /// <param name="loggingService">The logging service used for task diagnostics.</param>
    public TaskManager(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<BackgroundTaskSnapshot> ActiveTasks =>
        _activeTasks.Values.Select(execution => execution.CreateSnapshot()).ToArray();

    /// <inheritdoc />
    public event Action<BackgroundTaskSnapshot>? TaskChanged;

    /// <inheritdoc />
    public async Task<BackgroundTaskSnapshot> RunAsync(
        string name,
        Func<CancellationToken, IProgress<double>, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(operation);

        var execution = new TaskExecution(name, cancellationToken);
        if (!_activeTasks.TryAdd(execution.Id, execution))
        {
            throw new InvalidOperationException("Unable to register the background task.");
        }

        try
        {
            Publish(execution.CreateSnapshot());
            execution.Status = BackgroundTaskStatus.Running;
            Publish(execution.CreateSnapshot());
            var progress = new InlineProgress(value =>
            {
                execution.Progress = Math.Clamp(value, 0d, 1d);
                Publish(execution.CreateSnapshot());
            });

            await Task.Run(
                () => operation(execution.CancellationSource.Token, progress),
                CancellationToken.None).ConfigureAwait(false);
            execution.Status = BackgroundTaskStatus.Completed;
        }
        catch (OperationCanceledException) when (execution.CancellationSource.IsCancellationRequested)
        {
            execution.Status = BackgroundTaskStatus.Cancelled;
        }
        catch (Exception exception)
        {
            execution.Failure = exception;
            execution.Status = BackgroundTaskStatus.Failed;
            _loggingService.CreateLogger(nameof(TaskManager)).LogError(
                exception,
                "Background task {TaskName} failed.",
                name);
        }
        finally
        {
            var finalSnapshot = execution.CreateSnapshot();
            Publish(finalSnapshot);
            _activeTasks.TryRemove(execution.Id, out _);
            execution.CancellationSource.Dispose();
        }

        return execution.CreateSnapshot();
    }

    /// <inheritdoc />
    public bool TryCancel(Guid taskId)
    {
        if (!_activeTasks.TryGetValue(taskId, out var execution))
        {
            return false;
        }

        execution.CancellationSource.Cancel();
        return true;
    }

    private void Publish(BackgroundTaskSnapshot snapshot)
    {
        var handlers = TaskChanged?.GetInvocationList().Cast<Action<BackgroundTaskSnapshot>>().ToArray()
            ?? Array.Empty<Action<BackgroundTaskSnapshot>>();
        foreach (var handler in handlers)
        {
            try
            {
                handler(snapshot);
            }
            catch (Exception exception)
            {
                _loggingService.CreateLogger(nameof(TaskManager)).LogError(
                    exception,
                    "A background task observer failed.");
            }
        }
    }

    private sealed class TaskExecution
    {
        public TaskExecution(string name, CancellationToken cancellationToken)
        {
            Id = Guid.NewGuid();
            Name = name;
            CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public Guid Id { get; }

        public string Name { get; }

        public CancellationTokenSource CancellationSource { get; }

        public BackgroundTaskStatus Status { get; set; } = BackgroundTaskStatus.Pending;

        public double? Progress { get; set; }

        public Exception? Failure { get; set; }

        public BackgroundTaskSnapshot CreateSnapshot() => new(Id, Name, Status, Progress, Failure);
    }

    private sealed class InlineProgress : IProgress<double>
    {
        private readonly Action<double> _report;

        public InlineProgress(Action<double> report)
        {
            _report = report;
        }

        public void Report(double value)
        {
            _report(value);
        }
    }
}
