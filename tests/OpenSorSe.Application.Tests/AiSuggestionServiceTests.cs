using Microsoft.Extensions.Logging;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Tests;

/// <summary>Verifies application-owned gates, validation coordination, and local review behavior.</summary>
public sealed class AiSuggestionServiceTests
{
    /// <summary>Verifies a valid rename response becomes one immutable review-only proposal.</summary>
    [Fact]
    public async Task GenerateFileRenameAsync_ValidResponse_PublishesReviewOnlySuggestion()
    {
        var provider = new FakeProvider(RenameJson("item-001", "April Invoice.pdf"));
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());

        var result = await service.GenerateFileRenameAsync(CreateRenameRequest(), EnabledSettings(), CancellationToken.None);

        var suggestion = Assert.IsType<AiFileRenameSuggestion>(result.Suggestion);
        Assert.Equal(AiAvailabilityState.ModelSelected, result.State);
        Assert.Equal("file:1", suggestion.SourceFileId);
        Assert.Equal("April Invoice.pdf", suggestion.SuggestedFileName);
        Assert.Equal(0.74, suggestion.Confidence);
        Assert.DoesNotContain("C:\\Reports", provider.LastPrompt, StringComparison.Ordinal);
        Assert.Contains(AiPromptBuilder.FileRenameTaskId, provider.LastPrompt, StringComparison.Ordinal);
    }

    /// <summary>Verifies the master switch blocks provider and history access even when called internally.</summary>
    [Fact]
    public async Task GenerateFileRenameAsync_GlobalDisabled_DoesNotInvokeProviderOrHistory()
    {
        var provider = new FakeProvider("{}");
        var history = new InMemoryDecisionHistoryStore();
        var service = CreateService(provider, history);
        var settings = WithEnabled(EnabledSettings(), false);

        var result = await service.GenerateFileRenameAsync(CreateRenameRequest(), settings, CancellationToken.None);

        Assert.Equal(AiAvailabilityState.Disabled, result.State);
        Assert.Equal(0, provider.GenerateCallCount);
        Assert.Equal(0, history.LoadCallCount);
    }

    /// <summary>Verifies an independent capability switch blocks only that provider request.</summary>
    [Fact]
    public async Task GenerateFileRenameAsync_CapabilityDisabled_DoesNotInvokeProvider()
    {
        var provider = new FakeProvider("{}");
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());
        var settings = new AiSettings
        {
            Enabled = true,
            FileRenameSuggestionsEnabled = false,
            FolderStructureSuggestionsEnabled = true,
            SelectedModel = "local-model",
        };

        var result = await service.GenerateFileRenameAsync(CreateRenameRequest(), settings, CancellationToken.None);

        Assert.Equal(AiAvailabilityState.CapabilityDisabled, result.State);
        Assert.Equal(0, provider.GenerateCallCount);
    }

    /// <summary>Verifies exact selected-model availability is checked before any generation request.</summary>
    [Fact]
    public async Task GenerateFileRenameAsync_SelectedModelMissing_DoesNotGenerate()
    {
        var provider = new FakeProvider("{}")
        {
            ConnectionModels = [new AiModel("LOCAL-MODEL", "LOCAL-MODEL")],
        };
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());

        var result = await service.GenerateFileRenameAsync(
            CreateRenameRequest(),
            EnabledSettings(),
            CancellationToken.None);

        Assert.Equal(AiAvailabilityState.ModelUnavailable, result.State);
        Assert.Equal(1, provider.ConnectionCallCount);
        Assert.Equal(0, provider.GenerateCallCount);
    }

    /// <summary>Verifies invalid known context is rejected before provider or preference access.</summary>
    [Fact]
    public async Task GenerateFolderStructureAsync_DuplicateSourceIdentity_IsRejectedBeforeProvider()
    {
        var file = CreateFile("file:1", "invoice.pdf");
        var provider = new FakeProvider("{}");
        var history = new InMemoryDecisionHistoryStore();
        var service = CreateService(provider, history);

        var result = await service.GenerateFolderStructureAsync(
            new AiFolderStructureRequest([file, file], []),
            EnabledSettings(),
            CancellationToken.None);

        Assert.Equal(AiAvailabilityState.InvalidContext, result.State);
        Assert.Equal(0, provider.GenerateCallCount);
        Assert.Equal(0, history.LoadCallCount);
    }

    /// <summary>Verifies a valid hierarchy references only known sources and remains a logical preview.</summary>
    [Fact]
    public async Task GenerateFolderStructureAsync_ValidResponse_PublishesLogicalHierarchy()
    {
        var response = """
            {
              "taskId":"folder-structure-v1",
              "status":"suggestion",
              "folders":[
                {"folderId":"f1","name":"Finance","parentFolderId":null,"reason":"Category","confidence":0.8},
                {"folderId":"f2","name":"Invoices","parentFolderId":"f1","reason":"Filename","confidence":0.7}
              ],
              "assignments":[{"sourceFileId":"item-001","folderId":"f2"}],
              "reason":"A bounded logical grouping."
            }
            """;
        var provider = new FakeProvider(response);
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());

        var result = await service.GenerateFolderStructureAsync(
            new AiFolderStructureRequest([CreateFile("file:1", "invoice.pdf")], ["Existing"]),
            EnabledSettings(),
            CancellationToken.None);

        var plan = Assert.IsType<AiFolderStructurePlan>(result.Plan);
        Assert.Equal(AiAvailabilityState.ModelSelected, result.State);
        Assert.Equal(["Finance", "Finance/Invoices"], plan.Folders.Select(folder => folder.LogicalPath));
        Assert.Equal("Finance/Invoices", Assert.Single(plan.Items).DestinationFolder);
    }

    /// <summary>Verifies the separate document-text switch blocks provider communication while global AI remains enabled.</summary>
    [Fact]
    public async Task GenerateDocumentInterpretation_CapabilityDisabled_DoesNotInvokeProvider()
    {
        var provider = new FakeProvider("{}");
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());

        var result = await service.GenerateDocumentInterpretationAsync(
            DocumentRequest(),
            EnabledSettings(),
            CancellationToken.None);

        Assert.Equal(AiAvailabilityState.CapabilityDisabled, result.State);
        Assert.Equal(0, provider.ConnectionCallCount);
        Assert.Equal(0, provider.GenerateCallCount);
    }

    /// <summary>Verifies an enabled explicit document request produces only a validated review proposal.</summary>
    [Fact]
    public async Task GenerateDocumentInterpretation_ValidResponse_PublishesReviewOnlySuggestion()
    {
        const string json = """
            {"taskId":"document-text-interpretation-v1","status":"suggestion","sourceFileId":"item-001",
             "documentType":"Invoice","title":"Consulting invoice","tags":["Finance"],
             "dates":["2026-07-24"],"issuer":"Local Studio","suggestedFolder":"Invoices",
             "reason":"Explicit invoice fields are present.","confidence":0.71}
            """;
        var provider = new FakeProvider(json);
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());
        var settings = EnabledSettings(documentText: true);

        var result = await service.GenerateDocumentInterpretationAsync(
            DocumentRequest(),
            settings,
            CancellationToken.None);

        var suggestion = Assert.IsType<AiDocumentInterpretationSuggestion>(result.Suggestion);
        Assert.Equal("known:1", suggestion.SourceFileId);
        Assert.Equal("Invoices", suggestion.SuggestedFolder);
        Assert.Contains(AiPromptBuilder.DocumentInterpretationTaskId, provider.LastPrompt, StringComparison.Ordinal);
        Assert.Contains("Invoice date", provider.LastPrompt, StringComparison.Ordinal);
    }

    /// <summary>Verifies typed provider failures are translated without parsing or throwing.</summary>
    [Theory]
    [InlineData(AiProviderFailureKind.Timeout, AiAvailabilityState.Unavailable)]
    [InlineData(AiProviderFailureKind.Cancelled, AiAvailabilityState.RequestCancelled)]
    [InlineData(AiProviderFailureKind.ModelUnavailable, AiAvailabilityState.ModelUnavailable)]
    [InlineData(AiProviderFailureKind.UnsupportedResponse, AiAvailabilityState.ResponseInvalid)]
    public async Task GenerateFileRenameAsync_ProviderFailure_ReturnsControlledState(
        AiProviderFailureKind failure,
        AiAvailabilityState expected)
    {
        var provider = new FakeProvider(null, failure);
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());

        var result = await service.GenerateFileRenameAsync(CreateRenameRequest(), EnabledSettings(), CancellationToken.None);

        Assert.Equal(expected, result.State);
        Assert.Null(result.Suggestion);
    }

    /// <summary>Verifies pre-cancellation does not load history or invoke a provider.</summary>
    [Fact]
    public async Task GenerateFileRenameAsync_PreCancelled_ReturnsCancelledBeforeProvider()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new FakeProvider(RenameJson("file:1", "name.pdf"));
        var history = new InMemoryDecisionHistoryStore();
        var service = CreateService(provider, history);

        var result = await service.GenerateFileRenameAsync(CreateRenameRequest(), EnabledSettings(), cancellation.Token);

        Assert.Equal(AiAvailabilityState.RequestCancelled, result.State);
        Assert.Equal(0, provider.GenerateCallCount);
        Assert.Equal(0, history.LoadCallCount);
    }

    /// <summary>Verifies typed stages and opt-in raw diagnostics describe one complete metadata-only request.</summary>
    [Fact]
    public async Task GenerateFileRenameAsync_DiagnosticsEnabled_CapturesBoundedStagesAndLocalIdentity()
    {
        var provider = new FakeProvider(RenameJson("item-001", "April Invoice.pdf"));
        var diagnostics = new AiRequestDiagnosticsStore();
        diagnostics.SetEnabled(true);
        var service = new AiSuggestionService(
            provider,
            new InMemoryDecisionHistoryStore(),
            new LoggingService(),
            new FixedTimeProvider(DateTimeOffset.UnixEpoch),
            diagnostics);
        var progress = new InlineProgress<AiRequestProgress>();

        var result = await service.GenerateFileRenameAsync(
            CreateRenameRequest(),
            WithDiagnostics(),
            progress,
            CancellationToken.None);

        Assert.NotNull(result.Suggestion);
        Assert.Equal(AiRequestStage.CheckingSettings, progress.Values[0].Stage);
        Assert.Equal(AiRequestStage.SuggestionReady, progress.Values[^1].Stage);
        var record = Assert.Single(diagnostics.GetRecent());
        Assert.Equal("Accepted", record.ValidationOutcome);
        Assert.Contains("item-001", record.Prompt, StringComparison.Ordinal);
        Assert.Contains("invoice.pdf", record.Prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\Reports", record.Prompt, StringComparison.Ordinal);
        Assert.Contains(record.Stages, stage => stage.Stage == AiRequestStage.ValidatingModel);
        Assert.Contains(record.Stages, stage => stage.Stage == AiRequestStage.ValidatingSuggestion);

        static AiSettings WithDiagnostics()
        {
            var settings = EnabledSettings();
            return new AiSettings
            {
                Enabled = settings.Enabled,
                FileRenameSuggestionsEnabled = settings.FileRenameSuggestionsEnabled,
                FolderStructureSuggestionsEnabled = settings.FolderStructureSuggestionsEnabled,
                Endpoint = settings.Endpoint,
                SelectedModel = settings.SelectedModel,
                RequestTimeoutSeconds = settings.RequestTimeoutSeconds,
                PreferenceAdaptationEnabled = settings.PreferenceAdaptationEnabled,
                RequestDiagnosticsEnabled = true,
            };
        }
    }

    /// <summary>Verifies an enabled store cannot capture when the request's explicit opt-in is inactive.</summary>
    [Fact]
    public async Task GenerateFileRenameAsync_DiagnosticSettingDisabled_DoesNotCapture()
    {
        var diagnostics = new AiRequestDiagnosticsStore();
        diagnostics.SetEnabled(true);
        var service = new AiSuggestionService(
            new FakeProvider(RenameJson("item-001", "April Invoice.pdf")),
            new InMemoryDecisionHistoryStore(),
            new LoggingService(),
            new FixedTimeProvider(DateTimeOffset.UnixEpoch),
            diagnostics);

        var result = await service.GenerateFileRenameAsync(
            CreateRenameRequest(),
            EnabledSettings(),
            CancellationToken.None);

        Assert.NotNull(result.Suggestion);
        Assert.Empty(diagnostics.GetRecent());
    }

    /// <summary>Verifies the AI master switch alone gates ordinary provider setup communication.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task TestConnectionAsync_MissingRequiredMasterSwitch_DoesNotInvokeProvider(bool ai, bool advanced)
    {
        var provider = new FakeProvider("{}");
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());
        var settings = new ApplicationSettings
        {
            Features = new FeatureSettings { ShowAdvancedFeatures = advanced },
            Ai = new AiSettings { Enabled = ai },
        };

        var result = await service.TestConnectionAsync(settings, CancellationToken.None);

        if (ai)
        {
            Assert.Equal(AiAvailabilityState.Connected, result.State);
            Assert.Equal(1, provider.ConnectionCallCount);
        }
        else
        {
            Assert.Equal(AiAvailabilityState.Disabled, result.State);
            Assert.Equal(0, provider.ConnectionCallCount);
        }
    }

    /// <summary>Verifies an unexpected transport exception becomes a safe provider result.</summary>
    [Fact]
    public async Task TestConnectionAsync_ProviderTransportException_ReturnsUnavailable()
    {
        var provider = new FakeProvider("{}") { ConnectionException = new HttpRequestException("connection refused") };
        var service = CreateService(provider, new InMemoryDecisionHistoryStore());
        var settings = new ApplicationSettings
        {
            Features = new FeatureSettings { ShowAdvancedFeatures = true },
            Ai = EnabledSettings(),
        };

        var result = await service.TestConnectionAsync(settings, CancellationToken.None);

        Assert.Equal(AiAvailabilityState.Unavailable, result.State);
        Assert.Empty(result.Models);
        Assert.Equal(1, provider.ConnectionCallCount);
    }

    /// <summary>Verifies disabled review recording cannot write local AI history.</summary>
    [Fact]
    public async Task RecordDecisionAsync_DisabledCapability_DoesNotWriteHistory()
    {
        var history = new InMemoryDecisionHistoryStore();
        var service = CreateService(new FakeProvider("{}"), history);

        var result = await service.RecordDecisionAsync(
            CreateDecision(AiSuggestionDecisionKind.Rename, AiSuggestionDecisionOutcome.Accepted, "new.pdf", "new.pdf"),
            new AiSettings { Enabled = true },
            CancellationToken.None);

        Assert.Equal(AiAvailabilityState.CapabilityDisabled, result.State);
        Assert.Equal(0, history.AppendCallCount);
    }

    /// <summary>Verifies compatible history aggregation includes accepted v0.9.1 folder names.</summary>
    [Fact]
    public void PreferenceAggregator_AcceptedAndRejectedDecisions_ProducesStableSignals()
    {
        var decisions = new AiSuggestionDecision[]
        {
            CreateDecision(AiSuggestionDecisionKind.Tags, AiSuggestionDecisionOutcome.Accepted, "finance", "finance"),
            CreateDecision(AiSuggestionDecisionKind.Tags, AiSuggestionDecisionOutcome.Accepted, "finance", "finance"),
            CreateDecision(AiSuggestionDecisionKind.FolderStructure, AiSuggestionDecisionOutcome.Accepted, "Finance;Invoices", "Finance;Invoices"),
            CreateDecision(AiSuggestionDecisionKind.Category, AiSuggestionDecisionOutcome.Accepted, "Document", "Document"),
            CreateDecision(AiSuggestionDecisionKind.Rename, AiSuggestionDecisionOutcome.Rejected, "draft.pdf", null),
        };

        var preferences = AiPreferenceAggregator.Build(decisions);

        Assert.Equal(["finance"], preferences.PreferredTags);
        Assert.Equal(["Finance", "Invoices"], preferences.PreferredFolders);
        Assert.Equal(["Document"], preferences.PreferredCategories);
        Assert.Equal(["draft.pdf"], preferences.RejectedValues);
    }

    private static AiSuggestionService CreateService(FakeProvider provider, InMemoryDecisionHistoryStore store) =>
        new(provider, store, new LoggingService(), new FixedTimeProvider(DateTimeOffset.UnixEpoch));

    private static AiFileRenameRequest CreateRenameRequest(IReadOnlyList<string>? siblings = null) =>
        new(CreateFile("file:1", "invoice.pdf"), siblings ?? Array.Empty<string>());

    private static ResultFile CreateFile(string id, string name) => new(
        id,
        $"C:\\Reports\\{name}",
        name,
        Path.GetExtension(name),
        10,
        DateTimeOffset.UnixEpoch,
        FileCategory.Document,
        "Document",
        DuplicateStatus.Unique,
        null,
        false);

    private static AiSettings EnabledSettings(bool documentText = false) => new()
    {
        Enabled = true,
        FileRenameSuggestionsEnabled = true,
        FolderStructureSuggestionsEnabled = true,
        DocumentTextInterpretationEnabled = documentText,
        SelectedModel = "local-model",
    };

    private static AiSettings WithEnabled(AiSettings settings, bool enabled) => new()
    {
        Enabled = enabled,
        FileRenameSuggestionsEnabled = settings.FileRenameSuggestionsEnabled,
        FolderStructureSuggestionsEnabled = settings.FolderStructureSuggestionsEnabled,
        DocumentTextInterpretationEnabled = settings.DocumentTextInterpretationEnabled,
        Endpoint = settings.Endpoint,
        SelectedModel = settings.SelectedModel,
        RequestTimeoutSeconds = settings.RequestTimeoutSeconds,
        PreferenceAdaptationEnabled = settings.PreferenceAdaptationEnabled,
    };

    private static string RenameJson(string sourceId, string name) =>
        $$"""{"taskId":"file-rename-v1","status":"suggestion","sourceFileId":"{{sourceId}}","suggestedFileName":"{{name}}","reason":"Clearer supplied metadata name.","confidence":0.74}""";

    private static AiDocumentTextRequest DocumentRequest() =>
        new("known:1", "invoice.pdf", "Invoice date 2026-07-24 and issuer Local Studio.", null, []);

    private static AiSuggestionDecision CreateDecision(AiSuggestionDecisionKind kind, AiSuggestionDecisionOutcome outcome, string suggested, string? final) =>
        new(kind, outcome, ".pdf", suggested, final, "Ollama", "local-model", DateTimeOffset.UnixEpoch);

    private sealed class FakeProvider(string? response, AiProviderFailureKind failure = AiProviderFailureKind.None) : IAiSuggestionProvider
    {
        public int GenerateCallCount { get; private set; }

        public int ConnectionCallCount { get; private set; }

        public string LastPrompt { get; private set; } = string.Empty;

        public Exception? ConnectionException { get; init; }

        public IReadOnlyList<AiModel> ConnectionModels { get; init; } = [new AiModel("local-model", "local-model")];

        public Task<AiConnectionResult> GetConnectionAsync(AiSettings settings, CancellationToken cancellationToken)
        {
            ConnectionCallCount++;
            if (ConnectionException is not null)
            {
                throw ConnectionException;
            }

            return Task.FromResult(new AiConnectionResult(AiAvailabilityState.Connected, "Connected", ConnectionModels));
        }

        public Task<AiProviderGenerationResult> GenerateAsync(AiProviderGenerationRequest request, CancellationToken cancellationToken)
        {
            GenerateCallCount++;
            LastPrompt = request.Prompt;
            return Task.FromResult(new AiProviderGenerationResult(response, failure, failure == AiProviderFailureKind.None ? "OK" : "Controlled provider failure."));
        }
    }

    private sealed class InMemoryDecisionHistoryStore : IDecisionHistoryStore
    {
        private readonly List<AiSuggestionDecision> _decisions = [];

        public int LoadCallCount { get; private set; }

        public int AppendCallCount { get; private set; }

        public Task<IReadOnlyList<AiSuggestionDecision>> LoadAsync(CancellationToken cancellationToken)
        {
            LoadCallCount++;
            return Task.FromResult<IReadOnlyList<AiSuggestionDecision>>(Array.AsReadOnly(_decisions.ToArray()));
        }

        public Task AppendAsync(AiSuggestionDecision decision, CancellationToken cancellationToken)
        {
            AppendCallCount++;
            _decisions.Add(decision);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            _decisions.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InlineProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];

        public void Report(T value) => Values.Add(value);
    }
}
