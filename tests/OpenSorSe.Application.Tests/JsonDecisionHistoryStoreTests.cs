using OpenSorSe.AI;
using OpenSorSe.Application.AI;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.Application.Tests;

/// <summary>
/// Verifies local decision-history persistence without scan-result persistence or user-folder access.
/// </summary>
public sealed class JsonDecisionHistoryStoreTests
{
    /// <summary>
    /// Verifies a decision round-trips locally and reset removes the isolated history file.
    /// </summary>
    [Fact]
    public async Task AppendLoadAndClearAsync_RoundTripsAndResetsLocalHistory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"opensorse-ai-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "decision-history.json");
        var store = new JsonDecisionHistoryStore(path, new LoggingService());
        var decision = CreateDecision();

        try
        {
            await store.AppendAsync(decision, CancellationToken.None);
            var loaded = await store.LoadAsync(CancellationToken.None);
            await store.ClearAsync(CancellationToken.None);

            Assert.Equal([decision], loaded);
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies malformed local persistence fails safely instead of becoming preference context.
    /// </summary>
    [Fact]
    public async Task LoadAsync_MalformedHistory_ThrowsInvalidDataException()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"opensorse-ai-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "decision-history.json");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, "{invalid");
        var store = new JsonDecisionHistoryStore(path, new LoggingService());

        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync(CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AiSuggestionDecision CreateDecision() => new(
        AiSuggestionDecisionKind.Tags,
        AiSuggestionDecisionOutcome.Accepted,
        ".pdf",
        "finance",
        "finance",
        "Ollama",
        "model",
        DateTimeOffset.UnixEpoch);
}
