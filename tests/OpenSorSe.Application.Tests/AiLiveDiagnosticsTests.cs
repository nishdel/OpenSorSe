using OpenSorSe.Application.AI;

namespace OpenSorSe.Application.Tests;

/// <summary>Verifies opt-in live, bounded, privacy-aware diagnostics.</summary>
public sealed class AiLiveDiagnosticsTests
{
    /// <summary>Disabled diagnostics neither create nor expose a session.</summary>
    [Fact]
    public void Begin_WhenDisabled_DoesNotCreateSession()
    {
        var collector = new AiDiagnosticsCollector();
        var events = 0;
        collector.SessionChanged += (_, _) => events++;

        var id = collector.Begin(AiSuggestionKind.FileRename, "model", "http://127.0.0.1:11434");

        Assert.Null(id);
        Assert.Empty(collector.GetRecent());
        Assert.Equal(0, events);
    }

    /// <summary>Enabled sessions update live, preserve order, and observer failures are isolated.</summary>
    [Fact]
    public void EnabledSession_PublishesOrderedStagesAndSurvivesObserverFailure()
    {
        var collector = new AiDiagnosticsCollector();
        collector.Configure(true, true);
        collector.SessionChanged += (_, _) => throw new InvalidOperationException("Observer failure");
        var id = collector.Begin(AiSuggestionKind.FileRename, "model", "http://127.0.0.1:11434");

        collector.ReportStage(id, "Building system prompt", AiDiagnosticState.Succeeded, TimeSpan.FromMilliseconds(1));
        collector.ReportStage(id, "Request sent", AiDiagnosticState.Succeeded, TimeSpan.FromMilliseconds(2));
        collector.Complete(id, AiDiagnosticState.Succeeded, false, TimeSpan.FromMilliseconds(3));

        var session = Assert.Single(collector.GetRecent());
        Assert.Equal(
            ["Building system prompt", "Request sent"],
            session.Stages.Where(stage => stage.State != AiDiagnosticState.Pending).Select(stage => stage.Name));
        Assert.Equal(AiDiagnosticState.Succeeded, session.Status);
    }

    /// <summary>Default mode redacts content while explicit unredacted mode retains exact values.</summary>
    [Fact]
    public void Capture_RespectsRedactedAndUnredactedModes()
    {
        const string sensitive = """{"fileName":"tax-return.pdf","relativePath":"Finance/2025"}""";
        var collector = new AiDiagnosticsCollector();
        collector.Configure(true, false);
        var redactedId = collector.Begin(AiSuggestionKind.FileRename, "model", "http://127.0.0.1:11434")!;
        collector.Capture(redactedId, AiDiagnosticContentKind.UserPrompt, sensitive);
        Assert.DoesNotContain("tax-return.pdf", collector.GetRecent()[0].UserPrompt, StringComparison.Ordinal);

        collector.Configure(true, true);
        var exactId = collector.Begin(AiSuggestionKind.FileRename, "model", "http://127.0.0.1:11434")!;
        collector.Capture(exactId, AiDiagnosticContentKind.UserPrompt, sensitive);
        Assert.Equal(sensitive, collector.GetRecent()[0].UserPrompt);
    }

    /// <summary>History is bounded to 20 and can be cleared individually or entirely.</summary>
    [Fact]
    public void History_IsBoundedAndClearable()
    {
        var collector = new AiDiagnosticsCollector();
        collector.Configure(true, false);
        for (var index = 0; index < 23; index++)
            collector.Begin(AiSuggestionKind.FolderStructure, $"model-{index}", "http://127.0.0.1:11434");

        Assert.Equal(AiRequestDiagnosticLimits.MaximumRetainedRequests, collector.GetRecent().Count);
        var first = collector.GetRecent()[0].RequestId;
        collector.Clear(first);
        Assert.Equal(19, collector.GetRecent().Count);
        collector.Configure(false, false);
        Assert.Empty(collector.GetRecent());
    }

    /// <summary>Invalid reason types produce the precise diagnostic required for model troubleshooting.</summary>
    [Fact]
    public void Inspect_InvalidReasonObject_ReportsActualType()
    {
        var inspected = AiDiagnosticValidationInspector.Inspect(
            """{"taskId":"file-rename-v1","status":"suggestion","reason":{"text":"clearer"}}""",
            AiPromptBuilder.FileRenameTaskId);

        var reason = Assert.Single(inspected.Checks, check => check.PropertyName == "reason");
        Assert.False(reason.Passed);
        Assert.Contains("received object", reason.Message, StringComparison.Ordinal);
    }
}
