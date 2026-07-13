using TidyMind.Core.Logging;
using TidyMind.Core.Tasks;

namespace TidyMind.Core.Tests.Tasks;

/// <summary>
/// Tests shared background task coordination.
/// </summary>
public sealed class TaskManagerTests
{
    /// <summary>
    /// Verifies that a successful operation reports its final completed status.
    /// </summary>
    [Fact]
    public async Task RunAsync_ReturnsCompletedSnapshot()
    {
        using var loggingService = new LoggingService();
        var taskManager = new TaskManager(loggingService);

        var result = await taskManager.RunAsync(
            "Foundation test",
            (_, progress) =>
            {
                progress.Report(1d);
                return Task.CompletedTask;
            });

        Assert.Equal(BackgroundTaskStatus.Completed, result.Status);
        Assert.Equal(1d, result.Progress);
        Assert.Empty(taskManager.ActiveTasks);
    }
}
